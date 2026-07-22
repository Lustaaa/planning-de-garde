namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Slots</b> (BC Slots) — controller MVC REST porté sur l'hôte d'API détaché. Les
/// écritures invoquent les handlers <b>inchangés</b> (canal requête/réponse, CQRS) et renvoient un
/// accusé succès/échec ; la lecture projette les slots d'un jour (lecture seule, jamais de diffusion).
/// </summary>
[ApiController]
public sealed class SlotsController(
    PoserSlotHandler poserSlot,
    PoserSlotRecurrentHandler poserSlotRecurrent,
    SupprimerSlotHandler supprimerSlot,
    SupprimerSlotRecurrentHandler supprimerSlotRecurrent,
    JourneeEnfantQuery journee,
    SlotsDuJourQuery slots,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Pose ponctuelle d'un slot (: chevauchement accepté + averti, porté par l'outcome).</summary>
    [HttpPost("/api/slots")]
    public IActionResult Poser([FromBody] PoserSlotRequete requete)
    {
        var resultat = poserSlot.Handle(new PoserSlotCommand(requete.EnfantId, requete.LieuId, requete.Debut, requete.Fin));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        var chevauchement = journee.Chevauchements(requete.EnfantId, requete.Debut).Count > 0;
        return Ok(new PoserSlotReponse(chevauchement));
    }

    /// <summary>Pose d'un slot RÉCURRENT hebdo. Refus miroir de la pose ponctuelle avec son motif.</summary>
    [HttpPost("/api/slots/recurrents")]
    public IActionResult PoserRecurrent([FromBody] PoserSlotRecurrentRequete requete)
    {
        var resultat = poserSlotRecurrent.Handle(new PoserSlotRecurrentCommand(
            requete.EnfantId, requete.LieuId, requete.JourDeSemaine, requete.HeureDebut, requete.HeureFin,
            requete.ConditionneGarde, requete.PoseurId));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Suppression d'un slot par son identifiant stable (idempotente côté handler).</summary>
    [HttpDelete("/api/slots/{id}")]
    public IActionResult Supprimer(string id)
    {
        var resultat = supprimerSlot.Handle(new SupprimerSlotCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        // Diffusion temps réel (lecture seule) sur succès : les autres écrans re-projettent la grille
        // sans rechargement (parité avec l'ancien endpoint POST /api/canal/supprimer-slot).
        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Suppression d'un slot récurrent par son identifiant stable (idempotente).</summary>
    [HttpDelete("/api/slots/recurrents/{id}")]
    public IActionResult SupprimerRecurrent(string id)
    {
        var resultat = supprimerSlotRecurrent.Handle(new SupprimerSlotRecurrentCommand(id));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Slots couvrant une date (lecture seule, CQRS) — alimente la dialog de suppression.</summary>
    [HttpGet("/api/slots/{annee:int}/{mois:int}/{jour:int}")]
    public IActionResult DuJour(int annee, int mois, int jour)
    {
        var vues = slots.Lister(new DateOnly(annee, mois, jour))
            .Select(s => new SlotDuJourVue(s.Id, s.EnfantId, s.LieuId, s.Debut, s.Fin))
            .ToList();
        return Ok(vues);
    }
}
