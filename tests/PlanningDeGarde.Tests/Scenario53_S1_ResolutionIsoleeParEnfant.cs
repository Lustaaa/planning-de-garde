using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.1 — Résolution ISOLÉE par enfant : deux enfants, chacun son cycle et ses surcharges (@back)
//   Étant donné un foyer peuplé de DEUX enfants "Léa" et "Tom" (cycle + surcharges propres à chacun)
//   Quand je résous la grille pour "Léa" puis pour "Tom" sur la même fenêtre
//   Alors chaque case est résolue sur le cycle / les surcharges de SON enfant
//   Et le responsable résolu de "Léa" un jour donné n'est pas imposé à "Tom"
//   Et aucune case de "Tom" ne reflète une surcharge posée pour "Léa"
//
// Attendu ANCRÉ SUR LA RÈGLE : responsable = surcharge(enfant,jour) > fond(enfant,jour) > neutre.
// Aucun index codé en dur : chaque attendu est dérivé du cycle de l'enfant (ResponsableDeFond).
public class Scenario53_S1_ResolutionIsoleeParEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";

    // Acteurs : pA/pB portent le cycle de Léa, pC/pD celui de Tom, pE est le délégataire d'une surcharge Léa.
    private const string PA = "p-a", PB = "p-b", PC = "p-c", PD = "p-d", PE = "p-e";

    // Cycles N=2 DISTINCTS par enfant (mapping index→responsable propre à chacun).
    private static readonly CycleDeFond CycleLea = new(2, new Dictionary<int, string> { [0] = PA, [1] = PB });
    private static readonly CycleDeFond CycleTom = new(2, new Dictionary<int, string> { [0] = PC, [1] = PD });

    // Un jour de surcharge Léa (index 0 : ISO 28 paire) et un jour SANS surcharge d'une autre parité (index 1).
    private static readonly System.DateOnly J = new(2026, 7, 8);   // mercredi, ISO 28 → index 0
    private static readonly System.DateOnly J2 = new(2026, 7, 15);  // mercredi suivant, ISO 29 → index 1

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes, IReferentielCycleDeFond cycles)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "vert", [PC] = "orange", [PD] = "rouge", [PE] = "violet" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob", [PC] = "Carla", [PD] = "David", [PE] = "Ève" }),
            cycles,
            new FakeEnumerationActeursFoyer(PA, PB, PC, PD, PE));

    private static IReferentielCycleDeFond CyclesParEnfant()
    {
        var c = new FakeReferentielCycleDeFond();
        c.DefinirCycle(CycleLea, LeaId);
        c.DefinirCycle(CycleTom, TomId);
        return c;
    }

    private static JourCase Case(GrilleAgendaQuery grille, System.DateOnly jour, string enfantId)
        => grille.Projeter(jour, VuePlanning.QuatreSemaines, enfantId).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Chaque_enfant_resolu_sur_son_propre_cycle_et_ses_propres_surcharges()
    {
        var periodes = new FakePeriodeRepository();
        var cycles = CyclesParEnfant();
        var grille = Grille(periodes, cycles);

        // Given — une surcharge (délégation) posée pour LÉA le jour J (responsable Ève), scope enfant = Léa.
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            PE, J.ToDateTime(System.TimeOnly.MinValue), J.ToDateTime(System.TimeOnly.MinValue), LeaId).Valeur!);

        // Then — case J de LÉA : la surcharge prime (surcharge > fond) → Ève.
        Assert.Equal(PE, Case(grille, J, LeaId).ResponsableId);

        // Then — case J de TOM : résolue sur SON cycle (fond), la surcharge de Léa ne fuit PAS.
        Assert.Equal(CycleTom.ResponsableDeFond(J), Case(grille, J, TomId).ResponsableId);
        Assert.NotEqual(PE, Case(grille, J, TomId).ResponsableId);

        // Then — le responsable résolu de Léa (ici Ève par surcharge, sinon son fond) n'est PAS imposé à Tom :
        // même le FOND de Léa (Alice) diffère du fond de Tom (Carla).
        Assert.NotEqual(CycleLea.ResponsableDeFond(J), CycleTom.ResponsableDeFond(J));
        Assert.Equal(CycleLea.ResponsableDeFond(J), CycleLea.ResponsableDeFond(J)); // ancre lisible : pA
        Assert.NotEqual(Case(grille, J, LeaId).ResponsableId, Case(grille, J, TomId).ResponsableId);

        // Then — un jour SANS surcharge (J2, autre parité) : chacun sur son fond, distincts.
        Assert.Equal(CycleLea.ResponsableDeFond(J2), Case(grille, J2, LeaId).ResponsableId);
        Assert.Equal(CycleTom.ResponsableDeFond(J2), Case(grille, J2, TomId).ResponsableId);
        Assert.NotEqual(Case(grille, J2, LeaId).ResponsableId, Case(grille, J2, TomId).ResponsableId);

        // Then — AUCUNE case de Tom (sur toute la fenêtre) ne reflète la surcharge Léa (Ève).
        var casesTom = grille.Projeter(J, VuePlanning.QuatreSemaines, TomId).Jours;
        Assert.DoesNotContain(casesTom, j => j.ResponsableId == PE);
    }

    [Fact]
    public void Acceptation_InMemory_Isolation_sur_adaptateurs_reels()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var eve = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Ève")).Valeur!.ActeurId;
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        var cycles = new CycleDeFondEnMemoire();
        cycles.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = alice }), LeaId);
        cycles.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = carla }), TomId);

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycles, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // Une surcharge Léa le jour J (Ève).
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            eve, J.ToDateTime(System.TimeOnly.MinValue), J.ToDateTime(System.TimeOnly.MinValue), LeaId).Valeur!);

        // Léa : surcharge prime ; Tom : son fond, aucune fuite.
        Assert.Equal(eve, grille.Projeter(J, VuePlanning.Semaine, LeaId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.Equal(carla, grille.Projeter(J, VuePlanning.Semaine, TomId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.DoesNotContain(grille.Projeter(J, VuePlanning.Semaine, TomId).Jours, j => j.ResponsableId == eve);
    }
}
