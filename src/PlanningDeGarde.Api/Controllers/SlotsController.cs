namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Activités d'un enfant</b> (BC Slots) — controller MVC REST porté sur l'hôte d'API
/// détaché. Depuis s54, l'activité (ponctuelle ou récurrente) est une <b>sous-ressource de l'enfant</b>
/// (<c>/api/enfants/{enfantId}/activites…</c>) : l'<c>EnfantId</c> voyage par l'URL, plus dans le corps.
/// Les écritures invoquent les handlers <b>inchangés</b> (canal requête/réponse, CQRS) et renvoient un
/// accusé succès/échec ; la lecture projette les activités d'un jour <b>pour cet enfant</b> (lecture
/// seule, jamais de diffusion). Un id d'item ciblé par DELETE doit <b>appartenir</b> à l'enfant de l'URL
/// (scope défensif), sinon 404.
/// </summary>
[ApiController]
public sealed class SlotsController(
    PoserSlotHandler poserSlot,
    PoserSlotRecurrentHandler poserSlotRecurrent,
    ModifierSlotRecurrentHandler modifierSlotRecurrent,
    SupprimerSlotHandler supprimerSlot,
    SupprimerSlotRecurrentHandler supprimerSlotRecurrent,
    JourneeEnfantQuery journee,
    SlotsDuJourQuery slots,
    SlotsRecurrentsParEnfantQuery recurrentsParEnfant,
    ISlotRepository slotRepository,
    ISlotRecurrentRepository slotRecurrentRepository,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Pose ponctuelle d'une activité (chevauchement accepté + averti, porté par l'outcome).</summary>
    [HttpPost("/api/enfants/{enfantId}/activites")]
    public IActionResult Poser(string enfantId, [FromBody] PoserSlotRequete requete)
    {
        var resultat = poserSlot.Handle(new PoserSlotCommand(enfantId, requete.LieuId, requete.Debut, requete.Fin));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        var chevauchement = journee.Chevauchements(enfantId, requete.Debut).Count > 0;
        return Ok(new PoserSlotReponse(chevauchement));
    }

    /// <summary>Pose d'une activité RÉCURRENTE hebdo. Refus miroir de la pose ponctuelle avec son motif.</summary>
    [HttpPost("/api/enfants/{enfantId}/activites/recurrentes")]
    public IActionResult PoserRecurrent(string enfantId, [FromBody] PoserSlotRecurrentRequete requete)
    {
        var resultat = poserSlotRecurrent.Handle(new PoserSlotRecurrentCommand(
            enfantId, requete.LieuId, requete.JourDeSemaine, requete.HeureDebut, requete.HeureFin,
            requete.ConditionneGarde, requete.PoseurId, requete.JoursDeSemaine));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Suppression d'une activité par son identifiant stable (idempotente côté handler). Scope
    /// défensif : un id existant sous un AUTRE enfant → 404 (l'id doit appartenir à l'enfant de l'URL).</summary>
    [HttpDelete("/api/enfants/{enfantId}/activites/{id}")]
    public IActionResult Supprimer(string enfantId, string id)
    {
        var possede = slotRepository.AllSnapshots().FirstOrDefault(s => s.Id == id);
        if (possede is not null && possede.EnfantId != enfantId)
            return NotFound();

        var resultat = supprimerSlot.Handle(new SupprimerSlotCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        // Diffusion temps réel (lecture seule) sur succès : les autres écrans re-projettent la grille
        // sans rechargement.
        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Suppression d'une activité récurrente par son identifiant stable (idempotente). Scope
    /// défensif : un id existant sous un AUTRE enfant → 404.</summary>
    [HttpDelete("/api/enfants/{enfantId}/activites/recurrentes/{id}")]
    public IActionResult SupprimerRecurrent(string enfantId, string id)
    {
        var possede = slotRecurrentRepository.AllSnapshots().FirstOrDefault(s => s.Id == id);
        if (possede is not null && possede.EnfantId != enfantId)
            return NotFound();

        var resultat = supprimerSlotRecurrent.Handle(new SupprimerSlotRecurrentCommand(id));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Activités récurrentes de l'enfant (lecture seule, CQRS) — alimente la config foyer par
    /// enfant. Filtrées sur l'enfant de l'URL (isolation par enfant, s53).</summary>
    [HttpGet("/api/enfants/{enfantId}/activites/recurrentes")]
    public IActionResult RecurrentesDeLEnfant(string enfantId)
        => Ok(recurrentsParEnfant.PourEnfant(enfantId));

    /// <summary>Édition d'une activité récurrente (PUT) — TOUTE la série (jours + plage + lieu). Scope
    /// défensif : un id existant sous un AUTRE enfant → 404. L'EnfantId n'est jamais réaffecté (relu du slot).</summary>
    [HttpPut("/api/enfants/{enfantId}/activites/recurrentes/{id}")]
    public IActionResult ModifierRecurrent(string enfantId, string id, [FromBody] ModifierSlotRecurrentCorps corps)
    {
        var possede = slotRecurrentRepository.AllSnapshots().FirstOrDefault(s => s.Id == id);
        if (possede is not null && possede.EnfantId != enfantId)
            return NotFound();

        // Le handler d'édition diffuse déjà la mise à jour temps réel (lecture seule) sur succès.
        var resultat = modifierSlotRecurrent.Handle(new ModifierSlotRecurrentCommand(
            id, corps.LieuId, corps.JoursDeSemaine, corps.HeureDebut, corps.HeureFin,
            corps.ConditionneGarde, corps.PoseurId));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Activités de l'enfant couvrant une date (lecture seule, CQRS) — alimente la dialog de
    /// suppression. Filtrées sur l'enfant de l'URL (isolation par enfant, s53).</summary>
    [HttpGet("/api/enfants/{enfantId}/activites/{annee:int}/{mois:int}/{jour:int}")]
    public IActionResult DuJour(string enfantId, int annee, int mois, int jour)
    {
        var vues = slots.Lister(new DateOnly(annee, mois, jour), enfantId)
            .Select(s => new SlotDuJourVue(s.Id, s.EnfantId, s.LieuId, s.Debut, s.Fin))
            .ToList();
        return Ok(vues);
    }
}
