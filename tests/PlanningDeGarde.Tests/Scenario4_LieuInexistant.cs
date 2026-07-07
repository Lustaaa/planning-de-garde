using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 4 — Lieu inexistant (@erreur)
//   Given un Parent connecté + l'enfant « Léa » existe
//   And le lieu « ancienne crèche » n'existe pas dans la liste des lieux du foyer
//   When le Parent place Léa au lieu « ancienne crèche » de 8h30 à 16h30 le mardi 15/07
//   Then la création est refusée car le lieu n'existe pas
//   And aucun slot « Léa le 15/07 » n'apparaît dans le planning partagé
public class Scenario4_LieuInexistant
{
    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_refuser_la_pose_car_le_lieu_n_existe_pas_et_n_inscrire_aucun_slot_When_un_Parent_place_un_enfant_dans_un_lieu_absent_des_lieux_du_foyer()
    {
        // Given — aucun lieu enregistré : « ancienne crèche » n'existe pas
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielLieux();
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotHandler(slots, lieux, notificateur);
        var commande = new SlotBuilder()
            .PourEnfant("lea")
            .DansLieu("ancienne-creche")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 16, 30, 0))
            .Build();

        // When
        var resultat = handler.Handle(commande);

        // Then — la création est refusée car le lieu n'existe pas
        Assert.False(resultat.EstSucces);

        // And — aucun slot n'apparaît dans le planning partagé
        Assert.Empty(slots.AllSnapshots());
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    private static PoserSlotHandler Handler(FakeReferentielLieux lieux, out FakeSlotRepository slots)
    {
        slots = new FakeSlotRepository();
        var notificateur = new FakeNotificateurPlanning();
        return new PoserSlotHandler(slots, lieux, notificateur);
    }

    // Test #1 — la pose est refusée quand le lieu visé n'est pas dans les lieux du foyer
    // (refus introduit par la consultation du port ILieuRepository — vrai rouge)
    [Fact]
    public void Should_refuser_la_pose_au_motif_de_lieu_inexistant_When_le_lieu_vise_n_est_pas_dans_les_lieux_du_foyer()
    {
        var handler = Handler(new FakeReferentielLieux(), out _);
        var commande = new SlotBuilder().DansLieu("ancienne-creche").Build();

        var resultat = handler.Handle(commande);

        Assert.False(resultat.EstSucces);
    }

    // Test #2 — un lieu existant fait réussir la pose : force la vraie garde conditionnelle
    // de référence du lieu (contredit le toujours-refuser)
    [Fact]
    public void Should_poser_le_slot_au_lieu_designe_When_le_lieu_vise_existe_dans_les_lieux_du_foyer()
    {
        var handler = Handler(new FakeReferentielLieux().AvecLieu("ecole"), out _);
        var commande = new SlotBuilder().DansLieu("ecole").Build();

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
        Assert.Equal("ecole", resultat.Valeur!.LieuId);
    }

    // Test #3 — un refus pour lieu inexistant ne produit aucun effet de bord
    // (pas d'inscription partielle dans le planning partagé)
    [Fact]
    public void Should_n_inscrire_aucun_slot_dans_le_planning_partage_When_la_pose_est_refusee_pour_lieu_inexistant()
    {
        var handler = Handler(new FakeReferentielLieux(), out var slots);
        var commande = new SlotBuilder().DansLieu("ancienne-creche").Build();

        handler.Handle(commande);

        Assert.Empty(slots.AllSnapshots());
    }
}
