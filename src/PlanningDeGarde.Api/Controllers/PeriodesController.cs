namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Périodes</b> (BC Periodes) — controller MVC REST. Écritures via handlers inchangés
/// (canal requête/réponse) + diffusion temps réel (lecture seule) sur succès ; lecture des périodes
/// couvrant une date (CQRS, jamais de diffusion).
/// </summary>
[ApiController]
public sealed class PeriodesController(
    AffecterPeriodeHandler affecter,
    EditerPeriodeHandler editer,
    SupprimerPeriodeHandler supprimer,
    IPeriodeRepository periodes,
    PeriodesDuJourQuery periodesDuJour,
    IReferentielResponsables referentiel,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Affectation d'une période (POST). Succès acquitté, refus métier avec motif.</summary>
    [HttpPost("/api/periodes")]
    public IActionResult Affecter([FromBody] AffecterPeriodeRequete requete)
    {
        var resultat = affecter.Handle(new AffecterPeriodeCommand(requete.ResponsableId, requete.Debut, requete.Fin, requete.EnfantId));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Édition d'une période par id (PUT). L'état observé (jeton de concurrence optimiste) est
    /// résolu côté serveur sur l'id : load-then-act. Id absent → refus clair, jamais d'écriture aveugle.</summary>
    [HttpPut("/api/periodes/{id}")]
    public IActionResult Editer(string id, [FromBody] EditerPeriodeCorps corps)
    {
        var etatObserve = periodes.AllSnapshots().FirstOrDefault(p => p.Id == id);
        if (etatObserve is null)
            return BadRequest("Période introuvable : elle a peut-être été supprimée.");

        var resultat = editer.Handle(new EditerPeriodeCommand(
            etatObserve, corps.NouveauResponsableId, corps.NouveauDebut, corps.NouvelleFin));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Suppression d'une période par son identifiant stable (idempotente côté handler).</summary>
    [HttpDelete("/api/periodes/{id}")]
    public IActionResult Supprimer(string id)
    {
        var resultat = supprimer.Handle(new SupprimerPeriodeCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Périodes couvrant une date (lecture seule, CQRS) — nom du responsable résolu sur l'id.</summary>
    [HttpGet("/api/periodes/{annee:int}/{mois:int}/{jour:int}")]
    public IActionResult DuJour(int annee, int mois, int jour)
    {
        var vues = periodesDuJour.Lister(new DateOnly(annee, mois, jour))
            .Select(p => new PeriodeDuJourVue(p.Id, p.ResponsableId, referentiel.NomDe(p.ResponsableId), p.Debut, p.Fin))
            .ToList();
        return Ok(vues);
    }
}
