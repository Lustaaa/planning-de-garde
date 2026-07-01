using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 22 — Sc.4 — Invariant admin = parent (rejet sans écriture sinon) (@back)
//   Invariant métier PUR (Domain, aggrégat AdministrationFoyer) : un acteur ne peut être admin du
//   foyer QUE s'il est de type Parent. Le handler DesignerAdmin lit le type de l'acteur (read-only,
//   s14), demande à l'aggrégat de désigner l'admin, et ne persiste QUE si l'invariant est tenu. Un
//   acteur non-Parent est rejeté avec un motif clair, l'ensemble des admins reste inchangé.
public class Scenario4_DesignerAdminInvariantParent
{
    private const string Parent = "acteur-parent";
    private const string Nounou = "acteur-nounou";

    private static IEnumerationActeursFoyer Acteurs() => new FakeActeursTypes(new Dictionary<string, TypeActeur>
    {
        [Parent] = TypeActeur.Parent,
        [Nounou] = TypeActeur.Autre,
    });

    // ================= Invariant PUR Domain (aggrégat AdministrationFoyer) =================

    // ---------- Test #1 — Domain : un Parent PEUT devenir admin ----------
    // Contradiction : aucun aggrégat AdministrationFoyer n'existe. Force un aggrégat qui, sur un acteur
    // Parent, autorise la désignation et l'intègre à l'ensemble des admins.
    [Fact]
    public void Domain_Should_Accepter_et_integrer_l_admin_When_l_acteur_est_un_parent()
    {
        var administration = AdministrationFoyer.FromSnapshot(new string[0]);

        var resultat = administration.DesignerAdmin(Parent, acteurEstParent: true);

        Assert.True(resultat.EstSucces);
        Assert.Contains(Parent, administration.Admins);
    }

    // ---------- Test #2 — Domain : un non-Parent NE PEUT PAS devenir admin (invariant) ----------
    // Contradiction : l'impl du #1 pourrait intégrer tout acteur. Force l'invariant admin=parent :
    // un acteur non-Parent est refusé et n'intègre PAS l'ensemble des admins.
    [Fact]
    public void Domain_Should_Refuser_et_ne_pas_integrer_l_admin_When_l_acteur_n_est_pas_un_parent()
    {
        var administration = AdministrationFoyer.FromSnapshot(new string[0]);

        var resultat = administration.DesignerAdmin(Nounou, acteurEstParent: false);

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.DoesNotContain(Nounou, administration.Admins);
    }

    // ================= Frontière Application (handler + ports + store) =================

    // ---------- Acceptation — désigner un Parent admin réussit et persiste ----------
    [Fact]
    public void Acceptation_Should_Persister_l_admin_When_on_designe_un_acteur_parent()
    {
        var admins = new AdminsFoyerEnMemoire();
        var handler = new DesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DesignerAdminCommand(Parent));

        Assert.True(resultat.EstSucces);
        Assert.Contains(Parent, admins.EnumererAdmins()); // l'admin du foyer est ce Parent, persisté
    }

    // ---------- Acceptation — désigner un non-Parent est rejeté, admins inchangés ----------
    [Fact]
    public void Acceptation_Should_Rejeter_et_laisser_les_admins_inchanges_When_on_designe_un_acteur_non_parent()
    {
        var admins = new AdminsFoyerEnMemoire();
        var handler = new DesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DesignerAdminCommand(Nounou));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair (l'admin doit être un parent)
        Assert.Empty(admins.EnumererAdmins());                   // aucune écriture d'un admin non-Parent
    }

    // ---------- Driver — le handler traduit le type de l'acteur en invariant (rejet du non-Parent) ----------
    // Contradiction : sans lecture du type, le handler persisterait tout acteur. Force le câblage
    // TypeDe(acteur) -> invariant Domain -> aucune écriture pour un non-Parent.
    [Fact]
    public void Should_Ne_rien_ecrire_When_le_handler_recoit_un_acteur_non_parent()
    {
        var admins = new FakeAdminsFoyer();
        var handler = new DesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DesignerAdminCommand(Nounou));

        Assert.False(resultat.EstSucces);
        Assert.Empty(admins.EnumererAdmins());
    }
}
