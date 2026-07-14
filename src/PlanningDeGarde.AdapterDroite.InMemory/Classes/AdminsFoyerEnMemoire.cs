using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire des admins du foyer (petit agrégat de config foyer). Réalise le port de
/// lecture <see cref="IEnumerationAdminsFoyer"/> et le port d'écriture <see cref="IEditeurAdminsFoyer"/>
/// sur un ensemble d'ids stables d'acteurs. Volatile (re-parti vide au redémarrage) — le remplaçant
/// durable est <c>AdminsFoyerMongo</c>. L'invariant admin=parent est porté par l'agrégat Domain, pas
/// par le store (le store ne fait que persister une désignation déjà validée).
/// </summary>
public sealed class AdminsFoyerEnMemoire : IEnumerationAdminsFoyer, IEditeurAdminsFoyer
{
    private readonly HashSet<string> _admins = new();

    public void DesignerAdmin(string acteurId) => _admins.Add(acteurId);

    public void DeDesignerAdmin(string acteurId) => _admins.Remove(acteurId);

    public IReadOnlyCollection<string> EnumererAdmins() => _admins.ToList();
}
