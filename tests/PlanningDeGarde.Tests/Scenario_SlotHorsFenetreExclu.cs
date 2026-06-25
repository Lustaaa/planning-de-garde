using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 7 — Un slot hors fenêtre est exclu tandis qu'un slot interne du même
// jour-semaine est rendu (@erreur)
//   Given On est le 24/06/2026 et deux slots 'école' de Léa sont enregistrés de 08h00 à
//         17h00 : l'un le mardi 23/06/2026 (dans la fenêtre), l'autre le lundi 03/08/2026
//         (hors fenêtre)
//   When  Un Parent ouvre le hub /planning
//   Then  Le slot 'école 08h00–17h00' apparaît dans la case du mardi 23/06/2026, aucune
//         case n'est rendue pour le 03/08/2026, et le slot du 03/08 n'apparaît dans aucune
//         case de la grille
//
// Projection backend GrilleAgendaQuery — testée sans Blazor. Couple présence (interne) +
// absence (hors fenêtre) dans la même grille : une grille vide ou un rattachement paresseux
// (par jour-de-semaine / par heure, sans vérifier la date) échoue.
public class Scenario_SlotHorsFenetreExclu
{
    private static readonly DateOnly Mardi_23_06_2026 = new(2026, 6, 23);
    private static readonly DateOnly Lundi_03_08_2026 = new(2026, 8, 3);
    private static readonly DateOnly Date_24_06_2026 = new(2026, 6, 24);
    private static readonly TimeOnly H08 = new(8, 0);
    private static readonly TimeOnly H17 = new(17, 0);

    // Deux slots 'école' de Léa au MÊME horaire 08h00→17h00 : l'un interne (mardi 23/06,
    // dans la fenêtre 22/06..26/07), l'autre hors fenêtre (lundi 03/08). Même libellé + même
    // horaire = anti early-green : un rattachement par jour-semaine ou par heure (sans la
    // date) ferait remonter le slot du 03/08 dans une case interne.
    private static FakeSlotRepository SlotsInterneEtHorsFenetre()
    {
        var slots = new FakeSlotRepository();
        var interne = SlotDeLocalisation
            .Poser("lea", "ecole", new DateTime(2026, 6, 23, 8, 0, 0), new DateTime(2026, 6, 23, 17, 0, 0))
            .Valeur!;
        var horsFenetre = SlotDeLocalisation
            .Poser("lea", "ecole", new DateTime(2026, 8, 3, 8, 0, 0), new DateTime(2026, 8, 3, 17, 0, 0))
            .Valeur!;
        slots.Enregistrer(interne);
        slots.Enregistrer(horsFenetre);
        return slots;
    }

    private static GrilleAgendaQuery Query()
        => new(
            SlotsInterneEtHorsFenetre(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Faire_apparaitre_le_slot_ecole_du_mardi_23_06_2026_et_n_inclure_aucune_case_ni_aucun_slot_pour_le_03_08_2026_When_un_slot_interne_et_un_slot_hors_fenetre_sont_enregistres()
    {
        // Given — un slot interne (23/06) et un slot hors fenêtre (03/08), même horaire
        var query = Query();

        // When — un Parent consulte la grille le 24/06/2026
        var grille = query.Projeter(Date_24_06_2026);

        // Then — la case du mardi 23/06 contient le slot 'école 08h00–17h00'
        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        var slotCase = Assert.Single(caseMardi.Slots);
        Assert.Equal("ecole", slotCase.Libelle);
        Assert.Equal(H08, slotCase.Debut);
        Assert.Equal(H17, slotCase.Fin);

        // And — aucune case n'est datée du 03/08/2026
        Assert.DoesNotContain(grille.Jours, j => j.Date == Lundi_03_08_2026);

        // And — le slot du 03/08 n'apparaît dans aucune case (exactement 1 slot dans la grille)
        Assert.Equal(1, grille.Jours.Sum(j => j.Slots.Count));
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — présence + absence couplées : le slot interne du 23/06 est rendu ET le slot
    // hors fenêtre du 03/08 (même jour-semaine n'est PAS partagé ici : mardi vs lundi, mais
    // même libellé + même horaire) n'apparaît dans aucune case. Driver du filtrage strict
    // slot.Date ∈ fenêtre + rattachement par date complète. Early-green ANTICIPÉ
    // (caractérisation) : l'implémentation rattache déjà par DateOnly.FromDateTime(slot.Debut),
    // donc le slot du 03/08 n'a aucune case d'accueil.
    [Fact]
    public void Should_Exclure_le_slot_du_03_08_2026_tout_en_rendant_le_slot_du_mardi_23_06_2026_When_un_slot_interne_et_un_slot_hors_fenetre_partagent_le_meme_jour_semaine_et_le_meme_horaire()
    {
        var query = Query();

        var grille = query.Projeter(Date_24_06_2026);

        // présence : le slot interne du 23/06 est rendu
        Assert.NotEmpty(grille.Jours.Single(j => j.Date == Mardi_23_06_2026).Slots);
        // absence : le slot hors fenêtre du 03/08 n'apparaît dans AUCUNE case (toute la grille
        // ne contient qu'un seul slot — celui du 23/06)
        Assert.Equal(1, grille.Jours.Sum(j => j.Slots.Count));
    }

    // Test #2 — absence de case hors borne couplée à la cardinalité 35 : aucune case n'est
    // datée du 03/08 alors que la grille compte exactement 35 cases internes. Une grille qui
    // s'étendrait jusqu'au slot le plus lointain (au lieu de 35 jours fixes) échoue.
    [Fact]
    public void Should_Ne_creer_aucune_case_datee_du_03_08_2026_When_la_fenetre_s_arrete_au_dimanche_26_07_2026()
    {
        var query = Query();

        var grille = query.Projeter(Date_24_06_2026);

        Assert.Equal(35, grille.Jours.Count);
        Assert.DoesNotContain(grille.Jours, j => j.Date == Lundi_03_08_2026);
    }
}
