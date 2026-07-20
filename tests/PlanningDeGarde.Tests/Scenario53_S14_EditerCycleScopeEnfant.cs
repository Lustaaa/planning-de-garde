using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.14 — Éditer le cycle de fond EN VUE enfant A ne change QUE le cycle de A (@back)
//   Étant donné deux enfants "Léa" et "Tom" chacun avec son cycle de fond propre
//   Quand j'édite le cycle de "Léa" via DefinirCycleHandler (use case d'écriture RÉEL) EN VUE de "Léa"
//   Alors seule la résolution de "Léa" change ; celle de "Tom" reste STRICTEMENT inchangée
//   Et l'isolation est prouvée sur InMemory (et sur Mongo durable dans Api.Tests)
//
// Gate G3 (3e passage) : « Éditer le cycle de fond ne prend pas en compte l'enfant » — l'ÉCRITURE du cycle
// n'était pas scopée (DefinirCycle écrivait le bucket partagé), alors que la LECTURE l'était (CycleCourant(enfantId)).
public class Scenario53_S14_EditerCycleScopeEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string PA = "p-a", PB = "p-b", PC = "p-c";
    private static readonly DateOnly J = new(2026, 7, 8); // index 0 sur un cycle N=1 → l'unique responsable

    private static GrilleAgendaQuery Grille(IReferentielCycleDeFond cycles)
        => new(
            new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange", [PC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob", [PC] = "Carla" }),
            cycles,
            new FakeEnumerationActeursFoyer(PA, PB, PC));

    private static string? Resolu(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J).ResponsableId;

    [Fact]
    public void Acceptation_Editer_le_cycle_de_Lea_ne_change_pas_celui_de_Tom()
    {
        var cycles = new FakeReferentielCycleDeFond();
        var handler = new DefinirCycleHandler(cycles, new FakeNotificateurPlanning());

        // Given — chaque enfant a SON cycle propre (Léa → Alice, Tom → Bob) via le use case d'écriture RÉEL.
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = PA }, LeaId));
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = PB }, TomId));
        var grille = Grille(cycles);
        Assert.Equal(PA, Resolu(grille, LeaId));
        Assert.Equal(PB, Resolu(grille, TomId));

        // When — je RÉ-édite le cycle de Léa (Alice → Carla) EN VUE de Léa.
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = PC }, LeaId));

        // Then — Léa résout Carla ; Tom reste STRICTEMENT sur Bob (son cycle n'a pas bougé).
        Assert.Equal(PC, Resolu(grille, LeaId));
        Assert.Equal(PB, Resolu(grille, TomId));
    }

    [Fact]
    public void Acceptation_InMemory_Editer_cycle_scope_enfant()
    {
        var cycles = new CycleDeFondEnMemoire();
        var handler = new DefinirCycleHandler(cycles, new FakeNotificateurPlanning());
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = PA }, LeaId));
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = PB }, TomId));

        var config = new ConfigurationFoyerEnMemoire();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange", [PC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob", [PC] = "Carla" }),
            cycles, new FakeEnumerationActeursFoyer(PA, PB, PC), new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = PC }, LeaId));

        Assert.Equal(PC, Resolu(grille, LeaId));
        Assert.Equal(PB, Resolu(grille, TomId)); // Tom intact
    }
}
