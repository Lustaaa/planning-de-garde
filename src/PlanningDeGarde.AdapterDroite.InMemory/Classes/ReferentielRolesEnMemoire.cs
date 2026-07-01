using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire du référentiel de rôles du foyer (nouveau petit agrégat de config foyer,
/// miroir du CRUD acteurs). Réalise le port de lecture <see cref="IEnumerationRoles"/> et le port
/// d'écriture <see cref="IEditeurReferentielRoles"/> sur un dictionnaire id→libellé. Volatile
/// (re-parti vide au redémarrage) — le remplaçant durable est <c>ReferentielRolesMongo</c> ; la
/// résolution reste sur l'identifiant stable opaque, jamais sur le libellé.
/// </summary>
public sealed class ReferentielRolesEnMemoire : IEnumerationRoles, IEditeurReferentielRoles
{
    private readonly Dictionary<string, string> _libelles = new();

    public void Creer(string roleId, string libelle)
        => _libelles[roleId] = libelle; // le rôle neuf existe désormais sur son id opaque

    public void Renommer(string roleId, string nouveauLibelle)
        => _libelles[roleId] = nouveauLibelle; // même id (clé) → aucun doublon, dernière écriture gagne

    public IReadOnlyCollection<RoleFoyer> EnumererRoles()
        => _libelles.Select(kv => new RoleFoyer(kv.Key, kv.Value)).ToList();
}
