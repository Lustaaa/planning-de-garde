using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de notification temps réel (SignalR en Infrastructure).
/// Enregistre les notifications émises pour assertion au niveau acceptation.
/// </summary>
public sealed class FakeNotificateurPlanning : INotificateurPlanning
{
    private readonly List<string> _notifications = new();

    public void NotifierMiseAJour() => _notifications.Add("planning-mis-a-jour");

    public int NombreDeNotifications => _notifications.Count;
}
