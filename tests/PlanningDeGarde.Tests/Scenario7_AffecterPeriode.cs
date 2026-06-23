using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 7 — Un Parent affecte la responsabilité d'une période de garde (@nominal)
//   Given un Parent connecté + les responsables « Parent A » et « Parent B » existent
//   When le Parent rend « Parent A » responsable de Léa du lundi 14/07 au lundi 21/07
//   Then le planning partagé indique « Parent A responsable du 14/07 au 21/07 »
//   And « Parent A » reste responsable quel que soit le lieu où se trouve Léa pendant la période
//
// Axe responsabilité orthogonal à la localisation : la période ne lit jamais les slots.
public class Scenario7_AffecterPeriode
{
    private static readonly System.DateTime Debut = new(2025, 7, 14, 0, 0, 0);
    private static readonly System.DateTime Fin = new(2025, 7, 21, 0, 0, 0);

    private static AffecterPeriodeHandler Handler(out FakePeriodeRepository periodes)
    {
        periodes = new FakePeriodeRepository();
        var responsables = new FakeResponsableRepository().AvecResponsable("parent-a").AvecResponsable("parent-b");
        return new AffecterPeriodeHandler(periodes, responsables);
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_indiquer_le_responsable_de_la_periode_dans_le_planning_partage_independamment_du_lieu_de_l_enfant_When_un_Parent_affecte_un_responsable_sur_un_intervalle()
    {
        // Given
        var handler = Handler(out var periodes);
        var commande = new PeriodeBuilder().PourResponsable("parent-a").Du(Debut).Au(Fin).Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — le planning indique « Parent A responsable du 14/07 au 21/07 »
        Assert.True(resultat.EstSucces);
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-a", periode.ResponsableId);
        Assert.Equal(Debut, periode.Debut);
        Assert.Equal(Fin, periode.Fin);

        // And — la responsabilité reste stable quel que soit le lieu de l'enfant (orthogonalité) :
        // poser un slot pour Léa ne touche pas la période.
        var slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("ecole");
        var poser = new PoserSlotHandler(slots, lieux, new FakeNotificateurPlanning());
        poser.Handle(new SlotBuilder().PourEnfant("lea").DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 16, 30, 0))
            .Build());

        var periodeApres = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-a", periodeApres.ResponsableId);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — baseline du nouvel agrégat : l'affectation d'une période sur un intervalle
    // valide est confirmée (nil → constant)
    [Fact]
    public void Should_confirmer_l_affectation_de_la_periode_When_un_Parent_rend_un_responsable_responsable_sur_un_intervalle_valide()
    {
        var handler = Handler(out _);
        var commande = new PeriodeBuilder().Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
    }

    // Test #2 — la période affectée expose le responsable et l'intervalle fournis (snapshot)
    [Fact]
    public void Should_exposer_le_responsable_et_l_intervalle_de_la_periode_affectee_When_un_Parent_a_affecte_la_responsabilite()
    {
        var handler = Handler(out _);
        var commande = new PeriodeBuilder().PourResponsable("parent-b").Du(Debut).Au(Fin).Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
        var periode = resultat.Valeur!;
        Assert.Equal("parent-b", periode.ResponsableId);
        Assert.Equal(Debut, periode.Debut);
        Assert.Equal(Fin, periode.Fin);
    }

    // Test #3 — la période affectée est inscrite dans le planning partagé (fake repository)
    [Fact]
    public void Should_indiquer_la_periode_dans_le_planning_partage_du_foyer_When_la_periode_a_ete_affectee()
    {
        var handler = Handler(out var periodes);
        var commande = new PeriodeBuilder().PourResponsable("parent-a").Du(Debut).Au(Fin).Build();

        handler.Handle(commande);

        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-a", periode.ResponsableId);
        Assert.Equal(Debut, periode.Debut);
        Assert.Equal(Fin, periode.Fin);
    }

    // Test #4 — orthogonalité période↔slot : le responsable reste stable quels que soient
    // les slots de localisation de l'enfant pendant l'intervalle (la responsabilité ne lit
    // jamais les slots)
    [Fact]
    public void Should_conserver_le_responsable_de_la_periode_When_l_enfant_change_de_lieu_pendant_l_intervalle()
    {
        var handler = Handler(out var periodes);
        handler.Handle(new PeriodeBuilder().PourResponsable("parent-a").Du(Debut).Au(Fin).Build());

        // L'enfant change de lieu plusieurs fois pendant l'intervalle.
        var slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("ecole").AvecLieu("nounou");
        var poser = new PoserSlotHandler(slots, lieux, new FakeNotificateurPlanning());
        poser.Handle(new SlotBuilder().PourEnfant("lea").DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0)).A(new System.DateTime(2025, 7, 15, 16, 30, 0)).Build());
        poser.Handle(new SlotBuilder().PourEnfant("lea").DansLieu("nounou")
            .De(new System.DateTime(2025, 7, 16, 8, 30, 0)).A(new System.DateTime(2025, 7, 16, 16, 30, 0)).Build());

        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("parent-a", periode.ResponsableId);
        Assert.Equal(Debut, periode.Debut);
        Assert.Equal(Fin, periode.Fin);
    }
}
