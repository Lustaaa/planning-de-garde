using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 41 — Sc.1 — Dé-désigner un admin (commande/handler Domain pur) (@back)
//   Sens OFF du toggle admin (débloque le verrou ON s33). L'agrégat AdministrationFoyer (s22) retire
//   la désignation d'admin d'un acteur : mutation Domain pure, no-op idempotent si déjà non-admin,
//   acteur inconnu refusé sans mutation (garde handler). Le sens ON (DesignerAdmin, invariant
//   admin=parent) reste strictement inchangé. La borne « dernier admin » est Sc.2 (non traitée ici).
public class Sprint41Sc1_DeDesignerAdmin
{
    private const string ParentA = "acteur-parent-a";
    private const string ParentB = "acteur-parent-b";
    private const string Inconnu = "acteur-fantome";

    private static IEnumerationActeursFoyer Acteurs() => new FakeActeursTypes(new Dictionary<string, TypeActeur>
    {
        [ParentA] = TypeActeur.Parent,
        [ParentB] = TypeActeur.Parent,
    });

    // ================= Invariant PUR Domain (agrégat AdministrationFoyer) =================

    // ---------- Test #1 — Domain : dé-désigner un admin le retire de l'ensemble ----------
    // Contradiction : l'agrégat n'a pas de chemin de retrait. Force DeDesignerAdmin à retirer l'acteur
    // ciblé de l'ensemble des admins (mutation Domain pure), les autres admins conservés.
    [Fact]
    public void Domain_Should_Retirer_l_admin_cible_When_on_le_de_designe()
    {
        var administration = AdministrationFoyer.FromSnapshot(new[] { ParentA, ParentB });

        var resultat = administration.DeDesignerAdmin(ParentA);

        Assert.True(resultat.EstSucces);
        Assert.DoesNotContain(ParentA, administration.Admins); // retiré
        Assert.Contains(ParentB, administration.Admins);       // les autres admins conservés
    }

    // ---------- Test #2 — Domain : dé-désigner un acteur DÉJÀ non-admin est un no-op qui réussit ----------
    // Contradiction : une impl naïve pourrait échouer / muter. Force l'idempotence : dé-désigner un
    // acteur qui n'est pas admin réussit sans toucher l'ensemble.
    [Fact]
    public void Domain_Should_Reussir_en_no_op_sans_mutation_When_l_acteur_est_deja_non_admin()
    {
        var administration = AdministrationFoyer.FromSnapshot(new[] { ParentB });

        var resultat = administration.DeDesignerAdmin(ParentA); // ParentA n'est pas admin

        Assert.True(resultat.EstSucces);                  // no-op qui RÉUSSIT
        Assert.Single(administration.Admins);             // ensemble inchangé
        Assert.Contains(ParentB, administration.Admins);
    }

    // ================= Frontière Application (handler + ports + store) =================

    // ---------- Acceptation — dé-désigner un admin (parmi plusieurs) le retire et persiste ----------
    [Fact]
    public void Acceptation_Should_Retirer_l_admin_et_persister_When_on_de_designe_un_acteur_parmi_plusieurs_admins()
    {
        var admins = new AdminsFoyerEnMemoire();
        admins.DesignerAdmin(ParentA);
        admins.DesignerAdmin(ParentB);
        var handler = new DeDesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DeDesignerAdminCommand(ParentA));

        Assert.True(resultat.EstSucces);
        Assert.DoesNotContain(ParentA, admins.EnumererAdmins()); // dé-désignation persistée
        Assert.Contains(ParentB, admins.EnumererAdmins());       // l'autre admin conservé
    }

    // ---------- Acceptation — idempotence : dé-désigner un acteur connu mais non-admin réussit ----------
    [Fact]
    public void Acceptation_Should_Reussir_en_no_op_When_on_de_designe_un_acteur_connu_non_admin()
    {
        var admins = new AdminsFoyerEnMemoire();
        admins.DesignerAdmin(ParentB); // seul ParentB est admin
        var handler = new DeDesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DeDesignerAdminCommand(ParentA)); // connu, non-admin

        Assert.True(resultat.EstSucces);
        Assert.Single(admins.EnumererAdmins());
        Assert.Contains(ParentB, admins.EnumererAdmins());
    }

    // ---------- Driver — acteur INCONNU du référentiel : refus sans mutation ----------
    // Contradiction : sans garde, le handler retirerait / acquitterait un id fantôme. Force le refus
    // AVANT écriture : motif clair, ensemble des admins intact.
    [Fact]
    public void Should_Refuser_sans_mutation_When_l_acteur_est_inconnu_du_referentiel()
    {
        var admins = new AdminsFoyerEnMemoire();
        admins.DesignerAdmin(ParentA);
        admins.DesignerAdmin(ParentB);
        var handler = new DeDesignerAdminHandler(admins, admins, Acteurs());

        var resultat = handler.Handle(new DeDesignerAdminCommand(Inconnu));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif restitué
        Assert.Equal(2, admins.EnumererAdmins().Count);          // store intact
    }
}
