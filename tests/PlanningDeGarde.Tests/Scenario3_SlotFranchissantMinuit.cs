using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 3 — Slot de nuit franchissant minuit (@limite)
//   Given un Parent connecté + l'enfant « Léa » et le lieu « domicile A » existent
//   When le Parent place Léa au domicile A de 22h le 15/07 à 7h le 16/07
//   Then le slot « Léa au domicile A 22h–7h du 15/07 au 16/07 » apparaît dans le planning partagé
public class Scenario3_SlotFranchissantMinuit
{
    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_faire_apparaitre_le_slot_de_nuit_dans_le_planning_partage_When_un_Parent_place_un_enfant_de_22h_un_jour_a_7h_le_lendemain()
    {
        // Given
        var slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("domicile-a");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotHandler(slots, lieux, notificateur);
        var debut = new System.DateTime(2025, 7, 15, 22, 0, 0);
        var fin = new System.DateTime(2025, 7, 16, 7, 0, 0);
        var commande = new SlotBuilder()
            .PourEnfant("lea")
            .DansLieu("domicile-a")
            .De(debut)
            .A(fin)
            .Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — la pose réussit
        Assert.True(resultat.EstSucces);

        // And — le slot de nuit apparaît dans le planning partagé avec ses bornes calendaires
        var slot = Assert.Single(slots.AllSnapshots());
        Assert.Equal("lea", slot.EnfantId);
        Assert.Equal("domicile-a", slot.LieuId);
        Assert.Equal(debut, slot.Debut);
        Assert.Equal(fin, slot.Fin);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    private static PoserSlotHandler HandlerAvecDomicileA(out FakeSlotRepository slots)
    {
        slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("domicile-a");
        var notificateur = new FakeNotificateurPlanning();
        return new PoserSlotHandler(slots, lieux, notificateur);
    }

    // Test #1 — un slot franchissant minuit (fin calendaire > début) est de durée positive
    // et doit réussir, distinct de la durée nulle du Sc.2 (comparaison sur l'instant complet)
    [Fact]
    public void Should_confirmer_la_pose_du_slot_et_exposer_ses_bornes_de_22h_a_7h_le_lendemain_When_le_slot_franchit_minuit()
    {
        var handler = HandlerAvecDomicileA(out var slots);
        var debut = new System.DateTime(2025, 7, 15, 22, 0, 0);
        var fin = new System.DateTime(2025, 7, 16, 7, 0, 0);
        var commande = new SlotBuilder()
            .PourEnfant("lea").DansLieu("domicile-a").De(debut).A(fin).Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
        var slot = resultat.Valeur!;
        Assert.Equal(debut, slot.Debut);
        Assert.Equal(fin, slot.Fin);
    }
}
