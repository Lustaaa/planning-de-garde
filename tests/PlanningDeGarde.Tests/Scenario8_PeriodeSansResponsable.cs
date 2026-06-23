using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 8 — Période sans responsable refusée (@erreur)
//   Given un Parent connecté au planning du foyer
//   When le Parent crée une période de garde du 14/07 au 21/07 sans désigner de responsable
//   Then la création est refusée car un responsable est requis
//   And aucune période « du 14/07 au 21/07 » n'apparaît dans le planning partagé
//
// Invariant « exactement un responsable » porté par l'agrégat PeriodeDeGarde.
public class Scenario8_PeriodeSansResponsable
{
    private static readonly System.DateTime Debut = new(2025, 7, 14, 0, 0, 0);
    private static readonly System.DateTime Fin = new(2025, 7, 21, 0, 0, 0);

    private static AffecterPeriodeHandler Handler(out FakePeriodeRepository periodes)
    {
        periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a");
        return new AffecterPeriodeHandler(periodes, responsables);
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_refuser_la_creation_car_un_responsable_est_requis_et_n_inscrire_aucune_periode_When_un_Parent_cree_une_periode_sans_designer_de_responsable()
    {
        // Given — aucun responsable désigné (Id vide)
        var handler = Handler(out var periodes);
        var commande = new PeriodeBuilder().PourResponsable("").Du(Debut).Au(Fin).Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — la création est refusée car un responsable est requis
        Assert.False(resultat.EstSucces);

        // And — aucune période n'apparaît dans le planning partagé
        Assert.Empty(periodes.AllSnapshots());
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — la création est refusée quand aucun responsable n'est désigné
    // (garde « exactement un responsable » conditionnelle dès le départ : Sc.7 nominal est vert)
    [Fact]
    public void Should_refuser_la_creation_au_motif_de_responsable_requis_When_aucun_responsable_n_est_designe_sur_la_periode()
    {
        var handler = Handler(out _);
        var commande = new PeriodeBuilder().PourResponsable("").Build();

        var resultat = handler.Handle(commande);

        Assert.False(resultat.EstSucces);
    }

    // Test #2 — un refus faute de responsable ne produit aucun effet de bord
    // (aucune période inscrite dans le planning partagé)
    [Fact]
    public void Should_n_inscrire_aucune_periode_dans_le_planning_partage_When_la_creation_est_refusee_faute_de_responsable()
    {
        var handler = Handler(out var periodes);
        var commande = new PeriodeBuilder().PourResponsable("").Build();

        handler.Handle(commande);

        Assert.Empty(periodes.AllSnapshots());
    }
}
