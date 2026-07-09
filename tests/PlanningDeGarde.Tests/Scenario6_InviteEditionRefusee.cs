using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 6 — Un Invité tente d'éditer un slot (@erreur)
//   Given un slot « Léa à l'école 8h30–16h30 le 15/07 » existe dans le planning
//   And un Invité connecté en consultation seule
//   When l'Invité tente de déplacer ce slot chez la nounou
//   Then l'action est refusée car l'Invité est en consultation seule
//   And le slot reste « Léa à l'école 8h30–16h30 le 15/07 »
//
// Le droit Parent/Invité est gardé à l'ENTRÉE de l'Application (pas dans l'agrégat).
public class Scenario6_InviteEditionRefusee
{
    private static readonly System.DateTime Debut = new(2025, 7, 15, 8, 30, 0);
    private static readonly System.DateTime Fin = new(2025, 7, 15, 16, 30, 0);

    private static FakeSlotRepository PlanningAvecSlotLeaEcole()
    {
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielLieux().AvecLieu("ecole").AvecLieu("nounou");
        var poser = new PoserSlotHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), new FakeNotificateurPlanning());
        poser.Handle(new SlotBuilder().PourEnfant("lea").DansLieu("ecole").De(Debut).A(Fin).Build());
        return slots;
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_refuser_le_deplacement_du_slot_car_l_Invite_est_en_consultation_seule_et_laisser_le_slot_inchange_When_un_Invite_tente_de_deplacer_un_slot_existant()
    {
        // Given
        var slots = PlanningAvecSlotLeaEcole();
        var handler = new DeplacerSlotHandler(slots);
        var commande = new DeplacerSlotCommand(RoleAuteur.Invite, "lea", Debut, "nounou");

        // When
        var resultat = handler.Handle(commande);

        // Then — l'action est refusée car l'Invité est en consultation seule
        Assert.False(resultat.EstSucces);

        // And — le slot reste « Léa à l'école 8h30–16h30 le 15/07 »
        var slot = Assert.Single(slots.AllSnapshots());
        Assert.Equal("lea", slot.EnfantId);
        Assert.Equal("ecole", slot.LieuId);
        Assert.Equal(Debut, slot.Debut);
        Assert.Equal(Fin, slot.Fin);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — la modification est refusée quand l'auteur est un Invité (consultation seule)
    // Contrôle de droit à l'entrée de l'Application (refus inconditionnel d'abord sur rôle Invité)
    [Fact]
    public void Should_refuser_la_modification_au_motif_de_consultation_seule_When_l_auteur_de_l_action_est_un_Invite()
    {
        var slots = PlanningAvecSlotLeaEcole();
        var handler = new DeplacerSlotHandler(slots);
        var commande = new DeplacerSlotCommand(RoleAuteur.Invite, "lea", Debut, "nounou");

        var resultat = handler.Handle(commande);

        Assert.False(resultat.EstSucces);
    }

    // Test #2 — un Parent doit pouvoir modifier : contredit le toujours-refuser et force
    // la garde conditionnelle sur le rôle de l'auteur
    [Fact]
    public void Should_autoriser_la_modification_When_l_auteur_de_l_action_est_un_Parent()
    {
        var slots = PlanningAvecSlotLeaEcole();
        var handler = new DeplacerSlotHandler(slots);
        var commande = new DeplacerSlotCommand(RoleAuteur.Parent, "lea", Debut, "nounou");

        var resultat = handler.Handle(commande);

        Assert.True(resultat.EstSucces);
    }

    // Test #3 — le refus d'un Invité ne produit aucun effet de bord : le slot existant
    // reste à ses bornes et son lieu d'origine (snapshot inchangé)
    [Fact]
    public void Should_laisser_le_slot_inchange_dans_le_planning_partage_When_la_modification_d_un_Invite_est_refusee()
    {
        var slots = PlanningAvecSlotLeaEcole();
        var handler = new DeplacerSlotHandler(slots);
        var commande = new DeplacerSlotCommand(RoleAuteur.Invite, "lea", Debut, "nounou");

        handler.Handle(commande);

        var slot = Assert.Single(slots.AllSnapshots());
        Assert.Equal("lea", slot.EnfantId);
        Assert.Equal("ecole", slot.LieuId);
        Assert.Equal(Debut, slot.Debut);
        Assert.Equal(Fin, slot.Fin);
    }
}
