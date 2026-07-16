using Microsoft.AspNetCore.SignalR;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Hub temps réel du planning partagé. Le serveur pousse l'évènement « MiseAJour » aux
/// membres connectés du foyer ; les clients rafraîchissent leur vue à sa réception.
/// </summary>
public sealed class PlanningHub : Hub
{
    /// <summary>Nom de l'évènement poussé aux clients lors d'une mise à jour du planning.</summary>
    public const string EvenementMiseAJour = "MiseAJour";

    /// <summary>Évènement de diffusion PORTEUSE DE PAYLOAD de la cloche (s47) : porte un
    /// <c>EvenementChangementSnapshot</c> (journal — délégation / reprise / transfert) que le client reprojette
    /// dans sa cloche SANS GET sur push (garde-fou anti-flake).</summary>
    public const string EvenementChangement = "Changement";

    /// <summary>Évènement de diffusion PORTEUSE DE PAYLOAD d'une proposition d'échange (s47) : porte un
    /// <c>PropositionEchangeSnapshot</c> (créée / acceptée / refusée) reprojeté dans la cloche du destinataire /
    /// émetteur, 0 GET.</summary>
    public const string EvenementProposition = "Proposition";
}
