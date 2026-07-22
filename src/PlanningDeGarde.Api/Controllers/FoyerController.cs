namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Foyer</b> (agrégats transverses de lecture) — controller MVC REST. Expose le graphe
/// foyer « enfant-racine » (s38) : lecture AGRÉGÉE restituant, PAR enfant en racine, ses parents liés
/// (nom résolu + rôle-du-lien s37), reflet fidèle des liens réels. Lecture PURE — jamais de diffusion.
/// </summary>
[ApiController]
public sealed class FoyerController(GrapheFoyerQuery graphe) : ControllerBase
{
    /// <summary>Graphe foyer enfant-racine (lecture seule) consommé par la Config foyer.</summary>
    [HttpGet("/api/foyer/graphe")]
    public IActionResult Graphe() => Ok(graphe.Lire());
}
