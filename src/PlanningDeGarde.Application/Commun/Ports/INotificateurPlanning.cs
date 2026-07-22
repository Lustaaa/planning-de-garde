namespace PlanningDeGarde.Application.Commun.Ports;

/// <summary>Port de notification temps réel des membres du foyer (SignalR en Infrastructure).</summary>
public interface INotificateurPlanning
{
    void NotifierMiseAJour();
}
