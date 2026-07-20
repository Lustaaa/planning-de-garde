using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.17 — Un enfant SANS cycle propre résout NEUTRE, jamais le cycle d'un autre / le cycle '' (@back)
//   Étant donné un cycle propre à "Léa" (Alice) ET un cycle legacy PARTAGÉ "" (Bob), et "Tom" SANS cycle propre
//   Quand je résous la grille pour "Tom"
//   Alors "Tom" résout NEUTRE (personne assignée) — jamais le cycle de "Léa", jamais le cycle partagé "" (Bob)
//   Et l'isolation est prouvée sur InMemory (et sur Mongo durable dans Api.Tests)
//
// Gate G3 (4e passage) : « sur le planning de Charlie, Cyril/Esther apparaissent alors que seule Mélanie
// correspond à Charlie » — la LECTURE du cycle retombait sur le bucket partagé '' pour un enfant sans cycle propre.
public class Scenario53_S17_EnfantSansCyclePropreResoutNeutre
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string PA = "p-a", PB = "p-b";
    private static readonly DateOnly J = new(2026, 7, 8);

    private static GrilleAgendaQuery Grille(IReferentielCycleDeFond cycles)
        => new(
            new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob" }),
            cycles,
            new FakeEnumerationActeursFoyer(PA, PB));

    private static JourCase Case(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J);

    [Fact]
    public void Acceptation_Tom_sans_cycle_propre_resout_neutre()
    {
        var cycles = new FakeReferentielCycleDeFond();
        cycles.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = PB })); // cycle legacy PARTAGÉ "" (Bob)
        cycles.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = PA }), LeaId); // cycle propre de Léa (Alice)
        var grille = Grille(cycles);

        // Léa résout SON cycle (Alice).
        Assert.Equal(PA, Case(grille, LeaId).ResponsableId);

        // Tom n'a PAS de cycle propre → NEUTRE : ni Bob (cycle partagé ""), ni Alice (cycle de Léa).
        Assert.Null(Case(grille, TomId).ResponsableId);
        Assert.Equal("", Case(grille, TomId).NomResponsable);
    }

    [Fact]
    public void Acceptation_InMemory_Tom_sans_cycle_propre_resout_neutre()
    {
        var cycles = new CycleDeFondEnMemoire();
        cycles.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = PB })); // "" legacy
        cycles.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = PA }), LeaId);

        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob" }),
            cycles, new FakeEnumerationActeursFoyer(PA, PB), new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        Assert.Equal(PA, Case(grille, LeaId).ResponsableId);
        Assert.Null(Case(grille, TomId).ResponsableId); // Tom neutre, aucune fuite du cycle "" ni de Léa
    }
}
