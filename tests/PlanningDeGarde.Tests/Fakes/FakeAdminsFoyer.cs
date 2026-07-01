using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store des admins du foyer : réalise le port de lecture
/// <see cref="IEnumerationAdminsFoyer"/> (énumérer) et le port d'écriture <see cref="IEditeurAdminsFoyer"/>
/// (désigner) sur un ensemble d'identifiants stables d'acteurs, pour asserter en unitaire que le
/// handler a bien persisté (ou non) la désignation d'un admin.
/// </summary>
public sealed class FakeAdminsFoyer : IEnumerationAdminsFoyer, IEditeurAdminsFoyer
{
    private readonly HashSet<string> _admins = new();

    public void DesignerAdmin(string acteurId) => _admins.Add(acteurId);

    public IReadOnlyCollection<string> EnumererAdmins() => _admins.ToList();
}
