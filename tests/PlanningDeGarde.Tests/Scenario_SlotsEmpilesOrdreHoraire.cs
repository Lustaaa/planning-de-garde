using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 5 — Plusieurs slots d'un même jour sont empilés dans l'ordre horaire (@limite)
//   Given On est le 24/06/2026 et trois slots de Léa sont enregistrés le vendredi 26/06/2026 :
//         'domicile A 07h00–08h30', 'école 08h30–16h30', 'nounou 16h30–18h30'
//   When  Un Parent ouvre le hub /planning
//   Then  La case du vendredi 26/06/2026 liste les trois slots dans l'ordre 'domicile A 07h00–08h30'
//         puis 'école 08h30–16h30' puis 'nounou 16h30–18h30'
//
// Projection backend GrilleAgendaQuery — testée sans Blazor. Les trois slots sont enregistrés
// DANS LE DÉSORDRE dans le fake (≠ ordre d'insertion) pour piloter le tri par heure de début.
public class Scenario_SlotsEmpilesOrdreHoraire
{
    private const string DomicileA = "domicile-a";
    private const string Ecole = "ecole";
    private const string Nounou = "nounou";

    private static readonly DateOnly Date_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly Vendredi_26_06_2026 = new(2026, 6, 26);

    private static readonly TimeOnly H07 = new(7, 0);
    private static readonly TimeOnly H08h30 = new(8, 30);
    private static readonly TimeOnly H16h30 = new(16, 30);
    private static readonly TimeOnly H18h30 = new(18, 30);

    // Les trois slots de Léa du vendredi 26/06, enregistrés DANS LE DÉSORDRE
    // (nounou 16h30, puis domicile A 07h00, puis école 08h30) — l'ordre d'insertion
    // diffère de l'ordre horaire attendu, ce qui force un tri explicite par Debut.
    private static FakeSlotRepository TroisSlotsDeLeaLe_26_06_DansLeDesordre()
    {
        var slots = new FakeSlotRepository();
        slots.Enregistrer(SlotDeLocalisation
            .Poser("lea", Nounou, new DateTime(2026, 6, 26, 16, 30, 0), new DateTime(2026, 6, 26, 18, 30, 0))
            .Valeur!);
        slots.Enregistrer(SlotDeLocalisation
            .Poser("lea", DomicileA, new DateTime(2026, 6, 26, 7, 0, 0), new DateTime(2026, 6, 26, 8, 30, 0))
            .Valeur!);
        slots.Enregistrer(SlotDeLocalisation
            .Poser("lea", Ecole, new DateTime(2026, 6, 26, 8, 30, 0), new DateTime(2026, 6, 26, 16, 30, 0))
            .Valeur!);
        return slots;
    }

    private static GrilleAgendaQuery Query()
        => new(
            TroisSlotsDeLeaLe_26_06_DansLeDesordre(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    private static JourCase CaseDuVendredi(GrilleAgenda grille)
        => grille.Jours.Single(j => j.Date == Vendredi_26_06_2026);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Lister_les_trois_slots_du_vendredi_26_06_2026_dans_l_ordre_domicile_A_07h00_puis_ecole_08h30_puis_nounou_16h30_When_trois_slots_de_Lea_sont_enregistres_ce_jour_la()
    {
        // Given — trois slots de Léa le 26/06, enregistrés dans le désordre
        var query = Query();

        // When — un Parent consulte la grille le 24/06/2026
        var grille = query.Projeter(Date_24_06_2026);

        // Then — la case du vendredi 26/06 liste les trois slots dans l'ordre horaire
        var caseVendredi = CaseDuVendredi(grille);
        Assert.Equal(
            new[] { DomicileA, Ecole, Nounou },
            caseVendredi.Slots.Select(s => s.Libelle).ToArray());
        Assert.Equal(
            new[] { H07, H08h30, H16h30 },
            caseVendredi.Slots.Select(s => s.Debut).ToArray());
        Assert.Equal(
            new[] { H08h30, H16h30, H18h30 },
            caseVendredi.Slots.Select(s => s.Fin).ToArray());
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — ordre : les SlotCase de la case sont triés par heure de début, indépendamment
    // de l'ordre d'insertion dans le repository. (TPP unconditional → tri.) Driver : les slots
    // sont enregistrés désordonnés ; une projection qui conserve l'ordre d'insertion échoue.
    [Fact]
    public void Should_Empiler_les_trois_slots_du_vendredi_26_06_2026_dans_l_ordre_des_heures_de_debut_When_ils_sont_enregistres_dans_le_desordre()
    {
        var grille = Query().Projeter(Date_24_06_2026);

        var debuts = CaseDuVendredi(grille).Slots.Select(s => s.Debut).ToArray();
        Assert.Equal(new[] { H07, H08h30, H16h30 }, debuts);
    }

    // Test #2 — présence + cardinalité : les TROIS slots distincts coexistent dans la même case
    // (pas de fusion ni de perte), couplé à l'ordre du #1. Driver anti early-green : une
    // implémentation qui n'en garderait qu'un (ou les fusionnerait) échoue ici.
    [Fact]
    public void Should_Conserver_les_trois_slots_distincts_dans_la_meme_case_When_ils_partagent_le_meme_jour()
    {
        var grille = Query().Projeter(Date_24_06_2026);

        var libelles = CaseDuVendredi(grille).Slots.Select(s => s.Libelle).ToArray();
        Assert.Equal(3, libelles.Length);
        Assert.Equal(new[] { DomicileA, Ecole, Nounou }, libelles);
        Assert.Equal(3, libelles.Distinct().Count());
    }
}
