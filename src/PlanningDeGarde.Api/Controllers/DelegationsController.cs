using System.Globalization;

namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Délégations</b> (BC Delegation) — controller MVC REST. Déléguer COMPOSE l'écriture
/// surcharge multi-jours ; reprendre (DELETE) COMPOSE la suppression de surcharge. Diffusion
/// temps réel (lecture seule) sur succès ; jamais d'écriture par le canal de diffusion.
/// </summary>
[ApiController]
public sealed class DelegationsController(
    DeleguerRecuperationHandler deleguer,
    AnnulerDelegationHandler annuler,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Délégation de la récupération d'une PLAGE (POST). Refus métier avec motif.</summary>
    [HttpPost("/api/delegations")]
    public IActionResult Deleguer([FromBody] DeleguerRecuperationRequete requete)
    {
        var resultat = deleguer.Handle(new DeleguerRecuperationCommand(
            requete.Jour, requete.EnfantId, requete.VersActeurId, requete.JourFin));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>« Reprendre ce jour » (DELETE) : jour + enfant en query (l'enfant peut être vide en
    /// mono-enfant). No-op idempotent (jour sans délégation active) = succès.</summary>
    [HttpDelete("/api/delegations")]
    public IActionResult Annuler([FromQuery] string jour, [FromQuery] string? enfant)
    {
        var jourRepris = DateOnly.ParseExact(jour, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var resultat = annuler.Handle(new AnnulerDelegationCommand(jourRepris, enfant ?? ""));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }
}
