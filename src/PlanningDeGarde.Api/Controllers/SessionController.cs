namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Session</b> (BC Comptes, connexion s23/s25) — controller MVC REST. La connexion est une
/// commande applicative (canal requête/réponse) : réussit ssi un compte de cet email existe ET est Actif.
/// Sur succès, l'identité réelle (acteur lié) + son nom + son type sont résolus côté serveur (gating réel).
/// La session est un état d'hôte/requête, PAS un agrégat durable (aucune persistance neuve, règle 30).
/// </summary>
[ApiController]
public sealed class SessionController(
    SeConnecterHandler handler,
    IReferentielResponsables referentiel,
    IEnumerationActeursFoyer acteurs) : ControllerBase
{
    /// <summary>Connexion locale par email (POST). Sur refus (email inconnu / compte non activé), motif clair.</summary>
    [HttpPost("/api/session")]
    public IActionResult SeConnecter([FromBody] SeConnecterRequete requete)
    {
        var resultat = handler.Handle(new SeConnecterCommand(requete.Email, requete.MotDePasse));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        var acteurId = resultat.Valeur!.IdentiteReelle;
        return Ok(new SeConnecterReponse(acteurId, referentiel.NomDe(acteurId), acteurs.TypeDe(acteurId)));
    }
}
