using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.Tests;

/// <summary>Doublure du seul port temps réel — évite un hub SignalR vivant dans les tests UI.</summary>
public sealed class FakeNotificateurPlanning : INotificateurPlanning
{
    public int Notifications { get; private set; }

    public void NotifierMiseAJour() => Notifications++;
}
