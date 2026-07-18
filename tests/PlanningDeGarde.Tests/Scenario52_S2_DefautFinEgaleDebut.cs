using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 52 — Sc.2 — Défaut fin=début : échange mono-jour s47 STRICTEMENT inchangé (non-régression) (@back)
//   Étant donné un enfant et un délégataire connus
//   Quand un parent propose un échange SANS borne de fin (JourFin absent → fin = début = J1)
//   Alors le comportement est celui de l'échange d'UN jour s47 : la Proposition pending porte [J1..J1]
//   Et aucune régression du flux mono-jour s47 n'est introduite (JourFin == Jour)
public class Scenario52_S2_DefautFinEgaleDebut
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8); // ISO 28 paire → fond Parent A

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    [Fact]
    public void Should_Porter_un_intervalle_ponctuel_When_fin_absente()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();

        // Aucune borne de fin (JourFin absent, comme l'appel mono-jour s47).
        var resultat = new ProposerEchangeHandler(Grille(periodes), propositions, new FakeEnumerationActeursFoyer(ParentA, ParentB))
            .Handle(new ProposerEchangeCommand(Mercredi_08, LeaId, ParentB));

        Assert.True(resultat.EstSucces);
        var proposition = Assert.Single(propositions.AllSnapshots());

        // Intervalle ponctuel [J1..J1] : parité s47 (JourFin == Jour), aucune écriture (pending).
        Assert.Equal(Mercredi_08, proposition.Jour);
        Assert.Equal(Mercredi_08, proposition.JourFin);
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Empty(periodes.AllSnapshots());
    }
}
