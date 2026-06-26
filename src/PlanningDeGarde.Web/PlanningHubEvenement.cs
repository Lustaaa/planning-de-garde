namespace PlanningDeGarde.Web;

/// <summary>
/// Nom de l'évènement de diffusion du hub SignalR distant (« /hubs/planning »), tel que le
/// client navigateur (front WASM) doit s'y abonner. C'est le contrat de diffusion partagé avec
/// l'hôte d'API détaché (où <c>PlanningHub.EvenementMiseAJour</c> porte la même valeur) : le
/// front ne référençant pas l'Infrastructure, il en duplique la constante de transport.
/// </summary>
public static class PlanningHubEvenement
{
    /// <summary>Évènement poussé aux clients lors d'une mise à jour du planning.</summary>
    public const string MiseAJour = "MiseAJour";
}
