using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 52 — Sc.6 — Ré-proposition last-write-wins R11 · fin hors fenêtre sans crash (@back)
//   - Une pending sur [J1..J3] vers A puis une nouvelle sur [J1..J3] vers B → la dernière gagne (R11), sans doublon
//   - Une plage dont la fin dépasse la fenêtre de grille chargée → proposée puis acceptée → écriture valide, sans crash
public class Scenario52_S6_RepropositionEtFinHorsFenetre
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string ParentC = "parent-c";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);    // J1
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);    // J3

    // Plage lointaine (hors de toute fenêtre par défaut) dont la fin dépasse largement le début.
    private static readonly DateOnly Lointain_debut = new(2027, 3, 15);
    private static readonly DateOnly Lointain_fin = new(2027, 3, 20);

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange", [ParentC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno", [ParentC] = "Chloe" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    private static ProposerEchangeHandler Proposer(IPeriodeRepository periodes, IPropositionEchangeRepository propositions)
        => new(Grille(periodes), propositions, new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    [Fact]
    public void Should_Last_write_wins_sans_doublon_When_reproposition_sur_la_meme_plage()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var handler = Proposer(periodes, propositions);

        // La plage résout Parent A par le fond ; on propose d'abord vers B, puis vers C sur la MÊME plage.
        var premiere = handler.Handle(new ProposerEchangeCommand(Mardi_07, LeaId, ParentB, Jeudi_09));
        Assert.True(premiere.EstSucces);
        var seconde = handler.Handle(new ProposerEchangeCommand(Mardi_07, LeaId, ParentC, Jeudi_09));
        Assert.True(seconde.EstSucces);

        // Une seule pending subsiste (dernière écriture, vers C), sans doublon.
        var pending = Assert.Single(propositions.AllSnapshots().Where(p => p.Statut == StatutProposition.Proposee));
        Assert.Equal(ParentC, pending.VersActeurId);
        Assert.Equal(Mardi_07, pending.Jour);
        Assert.Equal(Jeudi_09, pending.JourFin);
        Assert.NotEqual(premiere.Valeur!.Id, pending.Id);
        Assert.Empty(periodes.AllSnapshots()); // toujours aucune surcharge (pending n'écrit rien)
    }

    [Fact]
    public void Should_Proposer_puis_accepter_sans_crash_When_fin_hors_fenetre()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var grille = Grille(periodes);

        // Recevant Parent C (hors cycle de fond → jamais le résolu, donc distinct du cédant quel qu'il soit).
        var proposition = Proposer(periodes, propositions)
            .Handle(new ProposerEchangeCommand(Lointain_debut, LeaId, ParentC, Lointain_fin));
        Assert.True(proposition.EstSucces);

        var delegation = new DeleguerRecuperationHandler(grille, periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));
        var accepte = new AccepterPropositionHandler(propositions, delegation)
            .Handle(new AccepterPropositionCommand(proposition.Valeur!.Id));

        Assert.True(accepte.EstSucces);

        // Écriture valide sur TOUTE la plage lointaine, sans crash.
        var surcharge = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentC, surcharge.ResponsableId);
        Assert.Equal(Lointain_debut, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Lointain_fin, DateOnly.FromDateTime(surcharge.Fin));
    }
}
