using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 36 — Sc.1 — RoleFoyer porte un flag « est rôle parent », persisté durablement (@back, option B1)
//   Étant donné le référentiel de rôles du foyer (id stable + libellé, s21)
//   Quand le modèle du rôle est enrichi d'un attribut booléen « est un rôle parent »
//   Alors le port de lecture (IEnumerationRoles.EnumererRoles) surface ce flag pour chaque rôle
//   Et l'adaptateur InMemory le porte et le restitue à l'identique (round-trip)
//   Et un rôle créé sans bascule démarre « est rôle parent » = false (défaut neutre, pas de crash)
//
// Frontière Application : le flag est la SOURCE DE VÉRITÉ de l'éligibilité (jamais le libellé, anti-piège
// s35). Le port d'écriture bas niveau MarquerParent pose/retire le flag (le handler idempotent est Sc.2).
public class Scenario36_S1_RoleFoyerFlagParent
{
    [Fact]
    public void Un_role_cree_sans_bascule_demarre_non_parent_defaut_neutre()
    {
        var roles = new ReferentielRolesEnMemoire();
        var roleId = new CreerRoleHandler(roles, roles).Handle(new CreerRoleCommand("Papa")).Valeur!.RoleId;

        var role = roles.EnumererRoles().Single(r => r.Id == roleId);
        Assert.False(role.EstRoleParent); // défaut neutre : aucun rôle marqué tant qu'aucune bascule
    }

    [Fact]
    public void MarquerParent_surface_le_flag_true_puis_false_round_trip_InMemory()
    {
        var roles = new ReferentielRolesEnMemoire();
        var roleId = new CreerRoleHandler(roles, roles).Handle(new CreerRoleCommand("Papa")).Valeur!.RoleId;

        roles.MarquerParent(roleId, true);
        Assert.True(roles.EnumererRoles().Single(r => r.Id == roleId).EstRoleParent);

        roles.MarquerParent(roleId, false);
        Assert.False(roles.EnumererRoles().Single(r => r.Id == roleId).EstRoleParent);
    }

    [Fact]
    public void Le_renommage_preserve_le_flag_parent_surfaces_distinctes()
    {
        var roles = new ReferentielRolesEnMemoire();
        var roleId = new CreerRoleHandler(roles, roles).Handle(new CreerRoleCommand("Papa")).Valeur!.RoleId;
        roles.MarquerParent(roleId, true);

        roles.Renommer(roleId, "Papounet"); // le libellé mute, le flag est une surface DISTINCTE

        var role = roles.EnumererRoles().Single(r => r.Id == roleId);
        Assert.Equal("Papounet", role.Libelle);
        Assert.True(role.EstRoleParent); // le renommage ne réinitialise pas le flag
    }
}
