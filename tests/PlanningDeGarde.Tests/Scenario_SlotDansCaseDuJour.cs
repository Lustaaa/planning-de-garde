using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 2 — Un slot enregistré apparaît dans la case de son jour avec son horaire (@nominal)
//   Given On est le 24/06/2026 et un slot 'école' pour Léa est enregistré le mardi 23/06/2026
//         de 08h00 à 17h00
//   When  Un Parent ouvre le hub /planning
//   Then  La case du mardi 23/06/2026 contient le slot libellé 'école 08h00–17h00', et aucune
//         autre case de la grille ne le contient
//
// Projection backend GrilleAgendaQuery — testée sans Blazor. Date de référence injectée,
// FakeSlotRepository peuplé via l'agrégat SlotDeLocalisation (copy-on-read de snapshot).
public class Scenario_SlotDansCaseDuJour
{
    private static readonly DateOnly Mardi_23_06_2026 = new(2026, 6, 23);
    private static readonly DateOnly Date_24_06_2026 = new(2026, 6, 24);
    private static readonly TimeOnly H08 = new(8, 0);
    private static readonly TimeOnly H17 = new(17, 0);

    private static FakeSlotRepository SlotsAvecEcoleDeLeaLe_23_06_de_08h_a_17h()
    {
        var slots = new FakeSlotRepository();
        var slot = SlotDeLocalisation
            .Poser("lea", "ecole", new DateTime(2026, 6, 23, 8, 0, 0), new DateTime(2026, 6, 23, 17, 0, 0))
            .Valeur!;
        slots.Enregistrer(slot);
        return slots;
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Placer_le_slot_ecole_08h00_17h00_de_Lea_dans_la_seule_case_du_mardi_23_06_2026_When_un_Parent_consulte_la_grille_le_24_06_2026()
    {
        // Given — un slot 'école' de Léa enregistré le mardi 23/06/2026 08h00→17h00
        var query = new GrilleAgendaQuery(SlotsAvecEcoleDeLeaLe_23_06_de_08h_a_17h(), new FakePeriodeRepository());

        // When — un Parent consulte la grille le 24/06/2026
        var grille = query.Projeter(Date_24_06_2026);

        // Then — la case du mardi 23/06 contient le slot 'école 08h00–17h00'
        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        var slotCase = Assert.Single(caseMardi.Slots);
        Assert.Equal("ecole", slotCase.Libelle);
        Assert.Equal(H08, slotCase.Debut);
        Assert.Equal(H17, slotCase.Fin);

        // And — aucune autre case ne le contient
        Assert.All(
            grille.Jours.Where(j => j.Date != Mardi_23_06_2026),
            j => Assert.Empty(j.Slots));
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — présence : la projection lit ISlotRepository et rattache le slot à la case de
    // sa date (TPP tableau vide → tableau peuplé). La grille du Sc.1 ne lit pas les slots.
    [Fact]
    public void Should_Faire_apparaitre_le_slot_de_Lea_dans_la_case_du_mardi_23_06_2026_When_ce_slot_est_enregistre_dans_la_fenetre()
    {
        var query = new GrilleAgendaQuery(SlotsAvecEcoleDeLeaLe_23_06_de_08h_a_17h(), new FakePeriodeRepository());

        var grille = query.Projeter(Date_24_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        Assert.NotEmpty(caseMardi.Slots);
    }

    // Test #2 — valeurs : le SlotCase expose le libellé 'école' et l'horaire 08h00→17h00
    // (TPP présence → valeurs). Un slot rattaché mais sans libellé/bornes échoue ; force le
    // mapping SlotCase { Libelle, Debut, Fin } depuis le snapshot (LieuId, partie horaire).
    [Fact]
    public void Should_Exposer_le_libelle_ecole_et_l_horaire_08h00_a_17h00_du_slot_When_le_slot_est_place_dans_sa_case()
    {
        var query = new GrilleAgendaQuery(SlotsAvecEcoleDeLeaLe_23_06_de_08h_a_17h(), new FakePeriodeRepository());

        var grille = query.Projeter(Date_24_06_2026);

        var slotCase = grille.Jours.Single(j => j.Date == Mardi_23_06_2026).Slots.Single();
        Assert.Equal("ecole", slotCase.Libelle);
        Assert.Equal(H08, slotCase.Debut);
        Assert.Equal(H17, slotCase.Fin);
    }

    // Test #3 — unicité (anti-duplication) : le slot n'est rattaché qu'à la case de son jour
    // (JourCase.Date == slot.Debut.Date). Une projection qui placerait le slot dans toutes les
    // cases (ou la mauvaise) échoue. Couplé au #1 (présence) pour qu'une grille vide ne passe pas.
    [Fact]
    public void Should_Ne_rattacher_le_slot_a_aucune_autre_case_que_celle_de_son_jour_When_la_grille_est_projetee()
    {
        var query = new GrilleAgendaQuery(SlotsAvecEcoleDeLeaLe_23_06_de_08h_a_17h(), new FakePeriodeRepository());

        var grille = query.Projeter(Date_24_06_2026);

        // présence dans la case du mardi 23/06...
        Assert.Single(grille.Jours.Single(j => j.Date == Mardi_23_06_2026).Slots);
        // ...et absence dans toutes les autres cases (exactement un slot dans toute la grille)
        Assert.All(
            grille.Jours.Where(j => j.Date != Mardi_23_06_2026),
            j => Assert.Empty(j.Slots));
        Assert.Equal(1, grille.Jours.Sum(j => j.Slots.Count));
    }
}
