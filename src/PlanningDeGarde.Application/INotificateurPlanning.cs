namespace PlanningDeGarde.Application;

/// <summary>Port de notification temps réel des membres du foyer (SignalR en Infrastructure).</summary>
public interface INotificateurPlanning
{
    void NotifierMiseAJour();
}
