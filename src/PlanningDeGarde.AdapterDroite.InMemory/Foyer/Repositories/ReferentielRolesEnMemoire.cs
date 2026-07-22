using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.InMemory.Foyer.Repositories;

// Le segment de namespace « .Foyer » masque la classe seed Foyer (Application.Foyer.Seed) : alias
// scopé au namespace (gagne sur le membre de namespace externe) pour relire le référentiel d'amorçage.
using Foyer = PlanningDeGarde.Application.Foyer.Seed.Foyer;

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
    // Flag « est un rôle parent » (s36, B1) porté sur une surface DISTINCTE du libellé : id → bool.
    // Absent = false par défaut (défaut neutre, une donnée sans flag se relit non-parent).
    private readonly Dictionary<string, bool> _parents = new();

    public void Creer(string roleId, string libelle)
        => _libelles[roleId] = libelle; // le rôle neuf existe désormais sur son id opaque

    public void Renommer(string roleId, string nouveauLibelle)
        => _libelles[roleId] = nouveauLibelle; // même id (clé) → aucun doublon, dernière écriture gagne

    public void Supprimer(string roleId)
    {
        _libelles.Remove(roleId); // cesse d'être énuméré ; tolérant à l'absence (idempotence)
        _parents.Remove(roleId);  // ... et son flag parent (surface distincte) — aucun flag fantôme
    }

    public void MarquerParent(string roleId, bool estParent)
        => _parents[roleId] = estParent; // pose/retire le flag ; surface distincte du libellé

    public IReadOnlyCollection<RoleFoyer> EnumererRoles()
        => _libelles.Select(kv => new RoleFoyer(
            kv.Key, kv.Value, _parents.TryGetValue(kv.Key, out var p) && p)).ToList();
}
