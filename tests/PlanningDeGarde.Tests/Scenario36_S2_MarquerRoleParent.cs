using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 36 — Sc.2 — Commande MarquerRoleParent : bascule le flag (@back, option B1)
//   Étant donné un rôle existant dont « est rôle parent » = false
//   Quand la commande (roleId, estParent=true) est émise → le flag passe à true et est persisté
//   Et ré-émettre (estParent=true) est NEUTRE (idempotent, aucun doublon)
//   Quand ré-émise avec estParent=false → le flag repasse à false (décoche pilotée par l'utilisateur)
//   Étant donné un roleId INEXISTANT → le domaine REFUSE, motif restitué, AUCUNE écriture
//
// Frontière Application : le handler MarquerRoleParent vérifie l'existence AVANT toute écriture ;
// la source de vérité reste le flag posé explicitement, jamais le libellé (anti-piège s35).
public class Scenario36_S2_MarquerRoleParent
{
    private static (ReferentielRolesEnMemoire roles, string roleId) FoyerAvecUnRole(string libelle)
    {
        var roles = new ReferentielRolesEnMemoire();
        var roleId = new CreerRoleHandler(roles, roles).Handle(new CreerRoleCommand(libelle)).Valeur!.RoleId;
        return (roles, roleId);
    }

    [Fact]
    public void Acceptation_Should_Basculer_le_flag_true_puis_false_When_la_commande_designe_un_role_existant()
    {
        var (roles, papaId) = FoyerAvecUnRole("Papa");
        var handler = new MarquerRoleParentHandler(roles, roles);

        var r1 = handler.Handle(new MarquerRoleParentCommand(papaId, true));
        Assert.True(r1.EstSucces);
        Assert.True(roles.EnumererRoles().Single(r => r.Id == papaId).EstRoleParent);

        // Ré-émettre estParent=true est NEUTRE (idempotent) — pas d'échec, toujours un unique rôle.
        Assert.True(handler.Handle(new MarquerRoleParentCommand(papaId, true)).EstSucces);
        Assert.Single(roles.EnumererRoles(), r => r.Id == papaId);
        Assert.True(roles.EnumererRoles().Single(r => r.Id == papaId).EstRoleParent);

        // Décoche pilotée par l'utilisateur : estParent=false → le flag repasse à false.
        Assert.True(handler.Handle(new MarquerRoleParentCommand(papaId, false)).EstSucces);
        Assert.False(roles.EnumererRoles().Single(r => r.Id == papaId).EstRoleParent);
    }

    [Fact]
    public void Should_Refuser_un_role_inexistant_sans_ecriture_When_le_roleId_est_absent_du_referentiel()
    {
        var (roles, _) = FoyerAvecUnRole("Papa");
        var handler = new MarquerRoleParentHandler(roles, roles);

        var resultat = handler.Handle(new MarquerRoleParentCommand("role-fantome", true));

        Assert.False(resultat.EstSucces);
        Assert.Equal("rôle inexistant", resultat.Motif);
        // AUCUNE écriture : aucun rôle fantôme n'apparaît, aucun flag posé sur un id absent.
        Assert.DoesNotContain(roles.EnumererRoles(), r => r.Id == "role-fantome");
    }
}
