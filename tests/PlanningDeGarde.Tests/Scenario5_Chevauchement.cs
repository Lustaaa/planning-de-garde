using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 5 — Chevauchement de localisation pour le même enfant (@limite)
//   Given un slot « Léa à l'école 8h30–16h30 le 15/07 » existe dans le planning
//   And un Parent connecté
//   When le Parent place Léa chez la nounou de 16h à 18h le mardi 15/07
//   Then le slot « Léa chez la nounou 16h–18h le 15/07 » est créé et apparaît dans le planning partagé
//   And le planning affiche un avertissement de chevauchement entre les slots de Léa le 15/07
//
// Règle clé : le second slot recouvrant EST créé (le chevauchement n'est pas un invariant
// d'écriture) ; l'avertissement est une projection de LECTURE (CQRS), jamais dans l'agrégat.
public class Scenario5_Chevauchement
{
    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_creer_le_second_slot_et_signaler_un_chevauchement_entre_les_slots_de_l_enfant_le_meme_jour_When_un_Parent_pose_un_slot_qui_recouvre_un_slot_existant_du_meme_enfant()
    {
        // Given — un slot Léa à l'école 8h30–16h30 le 15/07 existe déjà
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielActivites().AvecActivite("ecole").AvecActivite("nounou");
        var notificateur = new FakeNotificateurPlanning();
        var handler = new PoserSlotHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), notificateur);
        handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 16, 30, 0))
            .Build());

        // When — le Parent place Léa chez la nounou 16h–18h le 15/07 (recouvre 16h–16h30)
        var resultat = handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("nounou")
            .De(new System.DateTime(2025, 7, 15, 16, 0, 0))
            .A(new System.DateTime(2025, 7, 15, 18, 0, 0))
            .Build());

        // Then — le second slot est créé et apparaît dans le planning partagé
        Assert.True(resultat.EstSucces);
        Assert.Equal(2, slots.AllSnapshots().Count(s => s.EnfantId == "lea"));

        // And — la projection de lecture signale un chevauchement pour Léa le 15/07
        var query = new JourneeEnfantQuery(slots);
        var avertissements = query.Chevauchements("lea", new System.DateTime(2025, 7, 15));
        Assert.NotEmpty(avertissements);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    private static PoserSlotHandler Handler(out FakeSlotRepository slots)
    {
        slots = new FakeSlotRepository();
        var lieux = new FakeReferentielActivites().AvecActivite("ecole").AvecActivite("nounou");
        var notificateur = new FakeNotificateurPlanning();
        return new PoserSlotHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), notificateur);
    }

    // Test #1 — le second slot recouvrant EST créé : le chevauchement n'est pas un invariant
    // d'écriture (les deux slots de l'enfant apparaissent dans le planning partagé)
    [Fact]
    public void Should_creer_le_second_slot_et_le_faire_apparaitre_dans_le_planning_partage_When_il_recouvre_un_slot_existant_du_meme_enfant()
    {
        var handler = Handler(out var slots);
        handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 16, 30, 0))
            .Build());

        var resultat = handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("nounou")
            .De(new System.DateTime(2025, 7, 15, 16, 0, 0))
            .A(new System.DateTime(2025, 7, 15, 18, 0, 0))
            .Build());

        Assert.True(resultat.EstSucces);
        Assert.Equal(2, slots.AllSnapshots().Count(s => s.EnfantId == "lea"));
    }

    // Test #2 — baseline lecture : deux slots disjoints d'un enfant le même jour ne déclenchent
    // aucun avertissement (refus inconditionnel d'avertir d'abord)
    [Fact]
    public void Should_ne_signaler_aucun_chevauchement_When_les_slots_d_un_enfant_un_meme_jour_ne_se_recouvrent_pas()
    {
        var handler = Handler(out var slots);
        handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 12, 0, 0))
            .Build());
        handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("nounou")
            .De(new System.DateTime(2025, 7, 15, 14, 0, 0))
            .A(new System.DateTime(2025, 7, 15, 18, 0, 0))
            .Build());

        var query = new JourneeEnfantQuery(slots);
        var avertissements = query.Chevauchements("lea", new System.DateTime(2025, 7, 15));

        Assert.Empty(avertissements);
    }

    // Test #3 — deux slots du même enfant le même jour qui se recouvrent déclenchent
    // l'avertissement (contredit le « jamais d'avertissement » du stub)
    [Fact]
    public void Should_signaler_un_chevauchement_entre_les_slots_de_l_enfant_le_meme_jour_When_deux_slots_du_meme_enfant_se_recouvrent()
    {
        var handler = Handler(out var slots);
        handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("ecole")
            .De(new System.DateTime(2025, 7, 15, 8, 30, 0))
            .A(new System.DateTime(2025, 7, 15, 16, 30, 0))
            .Build());
        handler.Handle(new SlotBuilder()
            .PourEnfant("lea").DansLieu("nounou")
            .De(new System.DateTime(2025, 7, 15, 16, 0, 0))
            .A(new System.DateTime(2025, 7, 15, 18, 0, 0))
            .Build());

        var query = new JourneeEnfantQuery(slots);
        var avertissements = query.Chevauchements("lea", new System.DateTime(2025, 7, 15));

        var avertissement = Assert.Single(avertissements);
        Assert.Equal("lea", avertissement.EnfantId);
        Assert.Equal(new System.DateTime(2025, 7, 15), avertissement.Jour);
    }
}
