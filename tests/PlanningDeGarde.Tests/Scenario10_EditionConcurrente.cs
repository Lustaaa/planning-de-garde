using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 10 — Édition concurrente d'une période (@erreur, état périmé)
//   Given une période « Parent A responsable du 14/07 au 21/07 » existe
//   And le Parent X et le Parent Y affichent tous deux cette même période
//   And le Parent X a enregistré le remplacement du responsable par « Parent B »
//   When le Parent Y enregistre, depuis son affichage périmé, le décalage de la fin au 22/07
//   Then la modification du Parent Y est rejetée car elle se fonde sur un état périmé
//   And le Parent Y est invité à recharger l'état à jour de la période
//
// Le jeton optimiste fait partie du contrat de sauvegarde (IPeriodeRepository), pas un
// getter de l'agrégat. La version observée = le snapshot affiché par l'auteur.
public class Scenario10_EditionConcurrente
{
    private static readonly System.DateTime Debut = new(2025, 7, 14, 0, 0, 0);
    private static readonly System.DateTime Fin = new(2025, 7, 21, 0, 0, 0);

    private static FakePeriodeRepository PlanningAvecPeriodeParentA()
    {
        var periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a").AvecResponsable("parent-b");
        var affecter = new AffecterPeriodeHandler(periodes, responsables);
        affecter.Handle(new PeriodeBuilder().PourResponsable("parent-a").Du(Debut).Au(Fin).Build());
        return periodes;
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_rejeter_la_modification_fondee_sur_un_etat_perime_et_inviter_a_recharger_la_periode_When_un_Parent_enregistre_depuis_un_affichage_devance_par_une_modification_anterieure()
    {
        // Given — la période existe, X et Y affichent la même version initiale
        var periodes = PlanningAvecPeriodeParentA();
        var handler = new ModifierPeriodeHandler(periodes);
        var versionAffichee = Assert.Single(periodes.AllSnapshots());

        // And — le Parent X a remplacé le responsable par « Parent B » (devance la version)
        var modifX = handler.Handle(new ModifierPeriodeCommand(versionAffichee, new PeriodeSnapshot("parent-b", Debut, Fin)));
        Assert.True(modifX.EstSucces);

        // When — le Parent Y enregistre, depuis son affichage périmé, le décalage de la fin
        var modifY = handler.Handle(new ModifierPeriodeCommand(versionAffichee, new PeriodeSnapshot("parent-a", Debut, new System.DateTime(2025, 7, 22, 0, 0, 0))));

        // Then — la modification de Y est rejetée car fondée sur un état périmé,
        // And — Y est invité à recharger l'état à jour (motif métier observable)
        Assert.False(modifY.EstSucces);
        Assert.Contains("recharger", modifY.Motif, System.StringComparison.OrdinalIgnoreCase);

        // And — l'état conservé est celui de la modification antérieure (Parent B), pas Y
        var courante = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-b", courante.ResponsableId);
        Assert.Equal(Fin, courante.Fin);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — baseline : une modification fondée sur la version courante de la période réussit
    [Fact]
    public void Should_enregistrer_la_modification_When_elle_se_fonde_sur_la_version_courante_de_la_periode()
    {
        var periodes = PlanningAvecPeriodeParentA();
        var handler = new ModifierPeriodeHandler(periodes);
        var courante = Assert.Single(periodes.AllSnapshots());

        var resultat = handler.Handle(new ModifierPeriodeCommand(courante, new PeriodeSnapshot("parent-b", Debut, Fin)));

        Assert.True(resultat.EstSucces);
    }

    // Test #2 — une modification fondée sur une version dépassée est rejetée (écriture périmée)
    [Fact]
    public void Should_rejeter_la_modification_au_motif_d_etat_perime_When_la_periode_a_ete_modifiee_depuis_l_affichage_de_l_auteur()
    {
        var periodes = PlanningAvecPeriodeParentA();
        var handler = new ModifierPeriodeHandler(periodes);
        var versionAffichee = Assert.Single(periodes.AllSnapshots());

        // Une 1re modification devance la version observée.
        handler.Handle(new ModifierPeriodeCommand(versionAffichee, new PeriodeSnapshot("parent-b", Debut, Fin)));

        // Modification fondée sur la version désormais périmée.
        var resultat = handler.Handle(new ModifierPeriodeCommand(versionAffichee, new PeriodeSnapshot("parent-a", Debut, new System.DateTime(2025, 7, 22, 0, 0, 0))));

        Assert.False(resultat.EstSucces);
    }

    // Test #3 — le rejet d'une modification périmée ne produit aucun effet de bord :
    // l'état reste celui de la modification antérieure (snapshot inchangé)
    [Fact]
    public void Should_conserver_la_modification_anterieure_de_la_periode_When_une_modification_perimee_est_rejetee()
    {
        var periodes = PlanningAvecPeriodeParentA();
        var handler = new ModifierPeriodeHandler(periodes);
        var versionAffichee = Assert.Single(periodes.AllSnapshots());

        // Modification antérieure (Parent B) qui devance la version observée.
        handler.Handle(new ModifierPeriodeCommand(versionAffichee, new PeriodeSnapshot("parent-b", Debut, Fin)));

        // Modification périmée de Y, rejetée.
        handler.Handle(new ModifierPeriodeCommand(versionAffichee, new PeriodeSnapshot("parent-a", Debut, new System.DateTime(2025, 7, 22, 0, 0, 0))));

        var courante = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-b", courante.ResponsableId);
        Assert.Equal(Debut, courante.Debut);
        Assert.Equal(Fin, courante.Fin);
    }
}
