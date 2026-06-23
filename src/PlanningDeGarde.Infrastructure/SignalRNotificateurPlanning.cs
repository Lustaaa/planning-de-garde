using Microsoft.AspNetCore.SignalR;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Implémentation réelle du port de notification temps réel : pousse l'évènement de mise à
/// jour à tous les clients connectés via le hub SignalR. Remplace le fake des scénarios.
/// </summary>
public sealed class SignalRNotificateurPlanning : INotificateurPlanning
{
    private readonly IHubContext<PlanningHub> _hub;

    public SignalRNotificateurPlanning(IHubContext<PlanningHub> hub) => _hub = hub;

    public void NotifierMiseAJour()
        => _hub.Clients.All.SendAsync(PlanningHub.EvenementMiseAJour).GetAwaiter().GetResult();
}
