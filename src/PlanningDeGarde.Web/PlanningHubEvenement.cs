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

    /// <summary>Évènement de diffusion PORTEUSE DE PAYLOAD de la cloche : porte un
    /// <c>EvenementChangementSnapshot</c> (journal) que le client reprojette SANS GET sur push (miroir de
    /// <c>PlanningHub.EvenementChangement</c> côté API).</summary>
    public const string Changement = "Changement";

    /// <summary>Évènement de diffusion PORTEUSE DE PAYLOAD d'une proposition d'échange : porte un
    /// <c>PropositionEchangeSnapshot</c> reprojeté dans la cloche (miroir de <c>PlanningHub.EvenementProposition</c>).</summary>
    public const string Proposition = "Proposition";
}
