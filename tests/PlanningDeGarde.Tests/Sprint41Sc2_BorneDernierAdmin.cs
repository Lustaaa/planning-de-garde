using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 41 — Sc.2 — Borne « dernier admin » : refus AVANT écriture (limite) (@back)
//   Borne défensive neuve du sens OFF : dé-désigner le DERNIER admin du foyer est REFUSÉ avant toute
//   écriture (le foyer ne se retrouve JAMAIS sans admin, cohérent avec l'invariant admin=Parent s22).
//   Dé-désigner un admin quand il en reste d'autres réussit. La borne est portée par l'agrégat
//   AdministrationFoyer (Domain pur) ; le no-op idempotent (acteur déjà non-admin) NE déclenche PAS la
//   borne même si un seul autre admin subsiste.
public class Sprint41Sc2_BorneDernierAdmin
{
    private const string ParentA = "acteur-parent-a";
    private const string ParentB = "acteur-parent-b";

    private static IEnumerationActeursFoyer Acteurs() => new FakeActeursTypes(new Dictionary<string, TypeActeur>
    {
        [ParentA] = TypeActeur.Parent,
        [ParentB] = TypeActeur.Parent,
    });

    // ================= Invariant PUR Domain (agrégat AdministrationFoyer) =================

    // ---------- Test #1 — Domain : dé-désigner le DERNIER admin est refusé, store intact ----------
    // Contradiction : l'impl Sc.1 retire toujours. Force la borne : sur un unique admin, la
    // dé-désignation de CE dernier admin échoue avec un motif clair, l'ensemble reste inchangé.
    [Fact]
    public void Domain_Should_Refuser_et_conserver_l_admin_When_c_est_le_dernier_admin()
    {
        var administration = AdministrationFoyer.FromSnapshot(new[] { ParentA });

        var resultat = administration.DeDesignerAdmin(ParentA);

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair « au moins un admin »
        Assert.Contains(ParentA, administration.Admins);         // demeure admin, aucune mutation
    }

    // ---------- Test #2 — Domain : dé-désigner l'un de DEUX admins réussit (il en reste un) ----------
    [Fact]
    public void Domain_Should_Reussir_When_il_reste_au_moins_un_admin_apres_le_retrait()
    {
        var administration = AdministrationFoyer.FromSnapshot(new[] { ParentA, ParentB });

        var resultat = administration.DeDesignerAdmin(ParentA);

        Assert.True(resultat.EstSucces);
        Assert.DoesNotContain(ParentA, administration.Admins);
        Assert.Contains(ParentB, administration.Admins); // il reste un admin
    }

    // ---------- Test #3 — Domain : le no-op idempotent ne déclenche PAS la borne ----------
    // Un acteur DÉJÀ non-admin, même s'il ne reste qu'un seul autre admin, reste un no-op qui réussit
    // (la borne ne s'applique qu'à un RETRAIT effectif du dernier admin, pas à une absence).
    [Fact]
    public void Domain_Should_Rester_no_op_When_l_acteur_deja_non_admin_et_un_seul_autre_admin_subsiste()
    {
        var administration = AdministrationFoyer.FromSnapshot(new[] { ParentB });

        var resultat = administration.DeDesignerAdmin(ParentA); // non-admin

        Assert.True(resultat.EstSucces);
        Assert.Single(administration.Admins);
        Assert.Contains(ParentB, administration.Admins);
    }

    // ================= Frontière Application (handler + ports + store) =================

    // ---------- Acceptation — refus du dernier admin : store intact ----------
    [Fact]
    public void Acceptation_Should_Refuser_et_laisser_l_admin_When_on_de_designe_le_dernier_admin()
    {
        var admins = new AdminsFoyerEnMemoire();
        admins.DesignerAdmin(ParentA); // unique admin
        var handler = new DeDesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DeDesignerAdminCommand(ParentA));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Contains(ParentA, admins.EnumererAdmins()); // aucune mutation partielle
        Assert.Single(admins.EnumererAdmins());
    }

    // ---------- Acceptation — dé-désigner l'un de deux admins réussit et persiste ----------
    [Fact]
    public void Acceptation_Should_Reussir_When_on_de_designe_l_un_de_deux_admins()
    {
        var admins = new AdminsFoyerEnMemoire();
        admins.DesignerAdmin(ParentA);
        admins.DesignerAdmin(ParentB);
        var handler = new DeDesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DeDesignerAdminCommand(ParentA));

        Assert.True(resultat.EstSucces);
        Assert.DoesNotContain(ParentA, admins.EnumererAdmins());
        Assert.Contains(ParentB, admins.EnumererAdmins());
    }
}
