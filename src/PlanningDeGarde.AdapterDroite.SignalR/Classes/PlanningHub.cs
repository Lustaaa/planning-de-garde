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
}
