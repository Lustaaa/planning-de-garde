using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de <b>lecture</b> des admins du foyer (petit agrégat de config foyer) : énumère les ids
/// stables des acteurs désignés admins. Réalisé par le store en Infrastructure (InMemory tests /
/// Mongo runtime, bornés à la config foyer) ; l'Application n'en dépend pas. Alimente la
/// reconstitution de l'agrégat <c>AdministrationFoyer</c> (invariant admin=parent).
/// </summary>
public interface IEnumerationAdminsFoyer
{
    /// <summary>Les ids stables des acteurs admins du foyer, tels que persistés.</summary>
    IReadOnlyCollection<string> EnumererAdmins();
}
