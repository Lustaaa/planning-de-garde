namespace PlanningDeGarde.Api.Controllers;

/// <summary>Ressource <b>Transferts</b> (BC Transferts) — controller MVC REST. Écriture via le handler inchangé.</summary>
[ApiController]
public sealed class TransfertsController(DefinirTransfertHandler definir) : ControllerBase
{
    /// <summary>Définition d'un transfert de bascule (POST). Succès acquitté, refus métier avec motif.</summary>
    [HttpPost("/api/transferts")]
    public IActionResult Definir([FromBody] DefinirTransfertRequete requete)
    {
        var resultat = definir.Handle(new DefinirTransfertCommand(
            requete.DeposeParId, requete.RecupereParId, requete.LieuId, requete.Heure, requete.Date, requete.EnfantId));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }
}
