using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 2 — Slot de durée nulle refusé (@erreur)
//   Given un Parent connecté + l'enfant « Léa » et le lieu « école » existent
//   When le Parent place Léa à l'école de 16h30 à 16h30 le mardi 15/07
//   Then la création est refusée car la durée est nulle
//   And aucun slot « Léa à l'école le 15/07 » n'apparaît dans le planning partagé
public class Scenario2_SlotDureeNulle
{
    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_refuser_la_pose_car_la_duree_est_nulle_et_n_inscrire_aucun_slot_When_un_Parent_place_un_enfant_avec_une_fin_egale_au_debut()
    {
        // Given
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielLieux().AvecLieu("ecole");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), notificateur);
        var instant = new System.DateTime(2025, 7, 15, 16, 30, 0);
        var commande = new SlotBuilder()
            .PourEnfant("lea")
            .DansLieu("ecole")
            .De(instant)
            .A(instant)
            .Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — la création est refusée car la durée est nulle
        Assert.False(resultat.EstSucces);

        // And — aucun slot n'apparaît dans le planning partagé
        Assert.Empty(slots.AllSnapshots());
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    private static PoserSlotHandler HandlerAvecLieuEcole(
        out FakeSlotRepository slots,
        out FakeNotificateurPlanning notificateur)
    {
        slots = new FakeSlotRepository();
        var lieux = new FakeReferentielLieux().AvecLieu("ecole");
        notificateur = new FakeNotificateurPlanning();
        return new PoserSlotHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), notificateur);
    }

    // Test #1 — la pose est refusée quand la fin du slot est égale au début
    // (TPP unconditional → conditional : la garde fin > début contredit le toujours-réussir du Sc.1)
    [Fact]
    public void Should_refuser_la_pose_au_motif_de_duree_nulle_When_la_fin_du_slot_est_egale_au_debut()
    {
        var handler = HandlerAvecLieuEcole(out _, out _);
        var instant = new System.DateTime(2025, 7, 15, 16, 30, 0);
        var commande = new SlotBuilder().De(instant).A(instant).Build();

        var resultat = handler.Handle(commande);

        Assert.False(resultat.EstSucces);
    }

    // Test #2 — un refus pour durée nulle ne produit aucun effet de bord
    // (conditional : le slot ne doit pas être inscrit dans le planning partagé)
    [Fact]
    public void Should_n_inscrire_aucun_slot_dans_le_planning_partage_When_la_pose_est_refusee_pour_duree_nulle()
    {
        var handler = HandlerAvecLieuEcole(out var slots, out _);
        var instant = new System.DateTime(2025, 7, 15, 16, 30, 0);
        var commande = new SlotBuilder().De(instant).A(instant).Build();

        handler.Handle(commande);

        Assert.Empty(slots.AllSnapshots());
    }
}
