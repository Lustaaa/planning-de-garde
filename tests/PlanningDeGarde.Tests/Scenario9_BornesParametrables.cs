using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 9 — Bornes de période paramétrables (@limite)
//   Given un Parent connecté + le responsable « Parent B » existe
//   When le Parent rend « Parent B » responsable de Léa du mercredi 16/07 au mercredi 23/07
//   Then le planning partagé indique « Parent B responsable du 16/07 au 23/07 »
//
// Vérifie l'absence d'hypothèse de calendrier fixe : les bornes sont des données libres,
// pas une convention codée en dur (pas de semaine figée lundi→lundi).
public class Scenario9_BornesParametrables
{
    private static readonly System.DateTime Mercredi16 = new(2025, 7, 16, 0, 0, 0);
    private static readonly System.DateTime Mercredi23 = new(2025, 7, 23, 0, 0, 0);

    private static AffecterPeriodeHandler Handler(out FakePeriodeRepository periodes)
    {
        periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-b");
        return new AffecterPeriodeHandler(periodes, responsables);
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_indiquer_la_periode_aux_bornes_demandees_dans_le_planning_partage_When_un_Parent_affecte_un_responsable_sur_un_intervalle_decale()
    {
        // Given
        var handler = Handler(out var periodes);
        var commande = new PeriodeBuilder().PourResponsable("parent-b").Du(Mercredi16).Au(Mercredi23).Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — le planning indique « Parent B responsable du 16/07 au 23/07 » (bornes conservées)
        Assert.True(resultat.EstSucces);
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-b", periode.ResponsableId);
        Assert.Equal(Mercredi16, periode.Debut);
        Assert.Equal(Mercredi23, periode.Fin);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — un intervalle qui ne commence pas un lundi est conservé tel quel : les bornes
    // sont des données libres, aucune normalisation vers une semaine figée lundi→lundi
    [Fact]
    public void Should_exposer_la_periode_aux_bornes_demandees_When_l_intervalle_affecte_ne_commence_pas_un_lundi()
    {
        var handler = Handler(out _);
        var commande = new PeriodeBuilder().PourResponsable("parent-b").Du(Mercredi16).Au(Mercredi23).Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
        var periode = resultat.Valeur!;
        Assert.Equal(System.DayOfWeek.Wednesday, periode.Debut.DayOfWeek);
        Assert.Equal(Mercredi16, periode.Debut);
        Assert.Equal(Mercredi23, periode.Fin);
    }
}
