using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.State;

/// <summary>
/// Contexte de la session de consultation : le rôle de l'utilisateur (Parent / Invité) et
/// l'enfant affiché. Le rôle est gardé à l'entrée de l'Application (les handlers décident) ;
/// l'UI ne fait que le transmettre. Scoped = par circuit Blazor.
/// </summary>
public sealed class SessionPlanning
{
    public RoleAuteur Role { get; set; } = RoleAuteur.Parent;
    public string EnfantId { get; set; } = "Léa";

    public bool EstParent => Role == RoleAuteur.Parent;
}
