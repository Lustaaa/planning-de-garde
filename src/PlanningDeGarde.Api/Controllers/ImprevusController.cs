namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Imprévus</b> (BC Imprevus) — controller MVC REST. Signaler est purement INFORMATIF :
/// consigne une trace au journal (cloche) DIFFUSÉE (payload), N'ÉCRIT AUCUNE surcharge (résolution
/// jamais touchée, invariant). Refus métier (type inconnu) renvoyé avec son motif.
/// </summary>
[ApiController]
public sealed class ImprevusController(SignalerImprevuHandler signaler) : ControllerBase
{
    /// <summary>Signaler un imprévu (POST).</summary>
    [HttpPost("/api/imprevus")]
    public IActionResult Signaler([FromBody] SignalerImprevuRequete requete)
    {
        var resultat = signaler.Handle(new SignalerImprevuCommand(
            requete.Jour, requete.EnfantId, requete.Type, requete.SignalantId, requete.Motif));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }
}
