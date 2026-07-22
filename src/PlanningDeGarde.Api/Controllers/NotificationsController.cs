namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Notifications</b> (BC Notifications, cloche s47) — controller MVC REST. Lecture du flux
/// de l'utilisateur courant (événements du journal enrichis lu/non-lu PAR utilisateur + propositions le
/// concernant) ; « marquer lu » mute l'état de lecture PAR utilisateur (privé, aucune diffusion).
/// </summary>
[ApiController]
public sealed class NotificationsController(
    FluxNotificationsQuery flux,
    IPropositionEchangeRepository propositions,
    MarquerNotificationsLuesHandler marquer) : ControllerBase
{
    /// <summary>Flux de la cloche de l'utilisateur courant (lecture seule, CQRS) — trié par récence.</summary>
    [HttpGet("/api/notifications/{utilisateurId}")]
    public IActionResult Flux(string utilisateurId)
    {
        var evenements = flux.FluxAvecEtat(utilisateurId)
            .Select(n => new NotificationClocheVue(
                n.Evenement.Id, n.Evenement.Type.ToString().ToLowerInvariant(), n.Evenement.Jour,
                n.Evenement.EnfantId, n.Evenement.CedantId, n.Evenement.RecevantId, n.Evenement.Horodatage,
                n.Lu, false, null, n.Evenement.Imprevu?.ToString().ToLowerInvariant() ?? "changement"));

        var echanges = propositions.AllSnapshots()
            .Where(p => (p.VersActeurId == utilisateurId || p.DeActeurId == utilisateurId)
                        && p.Statut != StatutProposition.Refusee)
            .Select(p => new NotificationClocheVue(
                p.Id, "echange", p.Jour, p.EnfantId, p.DeActeurId, p.VersActeurId,
                DateTime.MinValue, p.Statut != StatutProposition.Proposee,
                p.Statut == StatutProposition.Proposee && p.VersActeurId == utilisateurId,
                p.Id, p.Statut.ToString().ToLowerInvariant()));

        var toutes = evenements.Concat(echanges)
            .OrderByDescending(n => n.Horodatage)
            .ToList();

        var nonLus = flux.NombreNonLus(utilisateurId)
            + propositions.AllSnapshots().Count(p => p.Statut == StatutProposition.Proposee && p.VersActeurId == utilisateurId);

        return Ok(new ClocheVue(nonLus, toutes));
    }

    /// <summary>Marquer lu (POST, s47) : un événement précis ou TOUTES ses notifications (EvenementId null).
    /// Idempotent côté handler ; aucune diffusion (état lu/non-lu privé à l'utilisateur).</summary>
    [HttpPost("/api/notifications/lues")]
    public IActionResult MarquerLues([FromBody] MarquerNotificationsLuesRequete requete)
    {
        var resultat = marquer.Handle(new MarquerNotificationsLuesCommand(requete.UtilisateurId, requete.EvenementId));
        return resultat.EstSucces ? Ok(resultat.Valeur) : BadRequest(resultat.Motif);
    }
}
