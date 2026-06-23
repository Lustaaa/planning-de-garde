using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 1 — Un Parent pose un slot de localisation (@nominal)
//   Given un Parent connecté + l'enfant « Léa » et le lieu « école » existent
//   When le Parent place Léa à l'école de 8h30 à 16h30 le mardi 15/07
//   Then le slot « Léa à l'école 8h30–16h30 le 15/07 » apparaît dans le planning partagé
//   And l'Invité reçoit une notification de mise à jour du planning
public class Scenario1_PoserSlot
{
    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_faire_apparaitre_le_slot_dans_le_planning_partage_et_notifier_l_Invite_When_un_Parent_place_un_enfant_dans_un_lieu_existant_sur_un_creneau_valide()
    {
        // Given
        var slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("ecole");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotHandler(slots, lieux, notificateur);
        var commande = new SlotBuilder()
            .PourEnfant("lea")
            .DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 16, 30, 0))
            .Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — la pose réussit
        Assert.True(resultat.EstSucces);

        // And — le slot apparaît dans le planning partagé
        var planning = slots.AllSnapshots();
        var slot = Assert.Single(planning);
        Assert.Equal("lea", slot.EnfantId);
        Assert.Equal("ecole", slot.LieuId);
        Assert.Equal(new System.DateTime(2025, 7, 15, 8, 30, 0), slot.Debut);
        Assert.Equal(new System.DateTime(2025, 7, 15, 16, 30, 0), slot.Fin);

        // And — l'Invité reçoit une notification de mise à jour
        Assert.Equal(1, notificateur.NombreDeNotifications);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    private static PoserSlotHandler HandlerAvecLieuEcole(
        out FakeSlotRepository slots,
        out FakeNotificateurPlanning notificateur)
    {
        slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu("ecole");
        notificateur = new FakeNotificateurPlanning();
        return new PoserSlotHandler(slots, lieux, notificateur);
    }

    // Test #1 — baseline : pose d'un slot valide confirmée (TPP nil → constant)
    [Fact]
    public void Should_confirmer_la_pose_du_slot_When_un_Parent_place_un_enfant_dans_un_lieu_existant_sur_un_creneau_valide()
    {
        var handler = HandlerAvecLieuEcole(out _, out _);
        var commande = new SlotBuilder().Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
    }

    // Test #2 — le slot posé reflète enfant / lieu / bornes fournis (TPP constant → scalar, snapshot)
    [Fact]
    public void Should_exposer_l_enfant_le_lieu_et_les_bornes_du_slot_pose_When_un_Parent_place_un_enfant_dans_un_lieu_existant_sur_un_creneau_valide()
    {
        var handler = HandlerAvecLieuEcole(out _, out _);
        var debut = new System.DateTime(2025, 7, 15, 8, 30, 0);
        var fin = new System.DateTime(2025, 7, 15, 16, 30, 0);
        var commande = new SlotBuilder()
            .PourEnfant("lea").DansLieu("ecole").De(debut).A(fin).Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
        var slot = resultat.Valeur!;
        Assert.Equal("lea", slot.EnfantId);
        Assert.Equal("ecole", slot.LieuId);
        Assert.Equal(debut, slot.Debut);
        Assert.Equal(fin, slot.Fin);
    }

    // Test #3 — le slot posé est inscrit dans le planning partagé (fake repository, AllSnapshots)
    [Fact]
    public void Should_inscrire_le_slot_dans_le_planning_partage_du_foyer_When_un_Parent_a_pose_le_slot()
    {
        var handler = HandlerAvecLieuEcole(out var slots, out _);
        var debut = new System.DateTime(2025, 7, 15, 8, 30, 0);
        var fin = new System.DateTime(2025, 7, 15, 16, 30, 0);
        var commande = new SlotBuilder()
            .PourEnfant("lea").DansLieu("ecole").De(debut).A(fin).Build();

        handler.Handle(commande);

        var slot = Assert.Single(slots.AllSnapshots());
        Assert.Equal("lea", slot.EnfantId);
        Assert.Equal("ecole", slot.LieuId);
        Assert.Equal(debut, slot.Debut);
        Assert.Equal(fin, slot.Fin);
    }
}
