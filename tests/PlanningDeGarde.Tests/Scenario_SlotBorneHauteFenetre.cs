using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 4 — Saisie à la borne haute de la fenêtre reste visible (@limite)
//   Given la date de référence est le 26/06/2026 ; la grille affiche les 35 jours datés
//         depuis le lundi 22/06/2026 ; un slot 'domicile A' est posé au 26/07/2026 (dernier
//         jour de la fenêtre) et un autre au 27/07/2026 (premier jour hors fenêtre)
//   When  la grille est projetée à la date de référence
//   Then  la case du 26/07/2026 porte le slot 'domicile A' ; et le slot du 27/07/2026 ne
//         figure dans aucune case de la fenêtre
//
// CARACTÉRISATION (early green attendu) : l'invariant fenêtre 35 jours depuis le lundi +
// exclusion stricte hors fenêtre est déjà codé dans GrilleAgendaQuery (sprint 03). Ce
// scénario fixe la BORNE HAUTE précise (lundi 22/06 + 34j = dimanche 26/07/2026) comme filet
// de non-régression. Présence (26/07 rendu) + absence (27/07 exclu) couplées dans la même
// grille : une grille vide ou une fenêtre élastique échouerait.
public class Scenario_SlotBorneHauteFenetre
{
    private static readonly DateOnly Date_26_06_2026 = new(2026, 6, 26);
    private static readonly DateOnly Lundi_22_06_2026 = new(2026, 6, 22);
    private static readonly DateOnly Dimanche_26_07_2026 = new(2026, 7, 26);
    private static readonly DateOnly Lundi_27_07_2026 = new(2026, 7, 27);
    private static readonly TimeOnly H08 = new(8, 0);
    private static readonly TimeOnly H17 = new(17, 0);

    // Deux slots 'domicile A' au MÊME horaire 08h00→17h00 : l'un à la borne haute (26/07,
    // dernier jour de la fenêtre 22/06..26/07), l'autre juste au-delà (27/07, hors fenêtre).
    // Même libellé + même horaire = anti early-green : un rattachement paresseux (par
    // jour-semaine ou par heure, sans la date) ferait remonter le slot du 27/07 dans une case.
    private static FakeSlotRepository SlotsBorneHauteEtPremierHorsFenetre()
    {
        var slots = new FakeSlotRepository();
        var borneHaute = SlotDeLocalisation
            .Poser("acteur-a", "domicile-a", new DateTime(2026, 7, 26, 8, 0, 0), new DateTime(2026, 7, 26, 17, 0, 0))
            .Valeur!;
        var horsFenetre = SlotDeLocalisation
            .Poser("acteur-a", "domicile-a", new DateTime(2026, 7, 27, 8, 0, 0), new DateTime(2026, 7, 27, 17, 0, 0))
            .Valeur!;
        slots.Enregistrer(borneHaute);
        slots.Enregistrer(horsFenetre);
        return slots;
    }

    private static GrilleAgendaQuery Query()
        => new(
            SlotsBorneHauteEtPremierHorsFenetre(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Faire_apparaitre_le_slot_domicile_A_dans_la_case_du_26_07_2026_et_exclure_un_slot_du_27_07_2026_When_la_grille_est_projetee_a_la_semaine_du_lundi_22_06_2026()
    {
        // Given — un slot à la borne haute (26/07) et un slot hors fenêtre (27/07), même horaire
        var query = Query();

        // When — la grille est projetée à la date de référence 26/06/2026
        var grille = query.Projeter(Date_26_06_2026);

        // Then — la fenêtre commence au lundi 22/06/2026 et fait 35 jours datés
        Assert.Equal(Lundi_22_06_2026, grille.Jours.First().Date);
        Assert.Equal(35, grille.Jours.Count);

        // And — la case du dimanche 26/07/2026 (dernier jour de la fenêtre) porte 'domicile A'
        var caseBorneHaute = grille.Jours.Single(j => j.Date == Dimanche_26_07_2026);
        var slotCase = Assert.Single(caseBorneHaute.Slots);
        Assert.Equal("domicile-a", slotCase.Libelle);
        Assert.Equal(H08, slotCase.Debut);
        Assert.Equal(H17, slotCase.Fin);

        // And — aucune case n'est datée du 27/07/2026...
        Assert.DoesNotContain(grille.Jours, j => j.Date == Lundi_27_07_2026);
        // ...et le slot du 27/07 ne figure dans aucune case (un seul slot dans toute la grille)
        Assert.Equal(1, grille.Jours.Sum(j => j.Slots.Count));
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — présence à la borne haute : le slot du 26/07/2026 (35ᵉ jour depuis le lundi
    // 22/06) est rattaché à la case de son jour. Caractérise que la fenêtre va bien jusqu'au
    // 26/07 inclus et que le rattachement par DateOnly.FromDateTime(slot.Debut) y accède.
    // Early green ANTICIPÉ (caractérisation) : la fenêtre fait déjà 35 jours et le rattachement
    // est déjà par date complète — la case du 26/07 existe et accueille le slot sans nouveau code.
    [Fact]
    public void Should_Faire_apparaitre_le_slot_domicile_A_dans_la_case_du_26_07_2026_When_il_est_pose_au_dernier_jour_de_la_fenetre_de_35_jours()
    {
        var query = Query();

        var grille = query.Projeter(Date_26_06_2026);

        var caseBorneHaute = grille.Jours.Single(j => j.Date == Dimanche_26_07_2026);
        var slotCase = Assert.Single(caseBorneHaute.Slots);
        Assert.Equal("domicile-a", slotCase.Libelle);
    }

    // Test #2 — absence juste au-delà de la borne : le slot du 27/07/2026 (1er jour hors
    // fenêtre) n'apparaît dans aucune case et aucune case n'est datée du 27/07. Couple présence
    // (26/07 rendu) + absence (27/07 exclu) pour qu'une grille vide ou une fenêtre élastique
    // (qui s'étendrait jusqu'au slot le plus lointain) ne passe pas.
    [Fact]
    public void Should_Exclure_le_slot_du_27_07_2026_de_toutes_les_cases_When_il_est_pose_au_premier_jour_hors_fenetre()
    {
        var query = Query();

        var grille = query.Projeter(Date_26_06_2026);

        // présence : la borne haute du 26/07 est rendue
        Assert.NotEmpty(grille.Jours.Single(j => j.Date == Dimanche_26_07_2026).Slots);
        // absence : aucune case datée du 27/07 et un seul slot dans toute la grille
        Assert.DoesNotContain(grille.Jours, j => j.Date == Lundi_27_07_2026);
        Assert.Equal(1, grille.Jours.Sum(j => j.Slots.Count));
    }
}
