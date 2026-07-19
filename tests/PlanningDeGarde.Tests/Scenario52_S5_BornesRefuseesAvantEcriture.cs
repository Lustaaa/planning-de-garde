using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 52 — Sc.5 — Bornes refusées AVANT écriture, sans écriture partielle (@back)
//   - fin < début (plage vide) → refusée AVANT écriture, AUCUNE Proposition créée
//   - délégataire = soi-même → refusée sans écriture
//   - délégataire inconnu / orphelin → refusée AVANT écriture, AUCUN jour de la plage écrit
//
// Frontière Application (ProposerEchangeHandler) + règle « fin < début » dans l'agrégat PropositionEchange.
public class Scenario52_S5_BornesRefuseesAvantEcriture
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    private static ProposerEchangeHandler Handler(IPeriodeRepository periodes, IPropositionEchangeRepository propositions)
        => new(Grille(periodes), propositions, new FakeEnumerationActeursFoyer(ParentA, ParentB));

    [Fact]
    public void Should_Refuser_avant_ecriture_When_fin_anterieure_au_debut()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();

        // fin (Mardi 07) < début (Jeudi 09) → plage vide, refus AVANT écriture.
        var resultat = Handler(periodes, propositions)
            .Handle(new ProposerEchangeCommand(Jeudi_09, LeaId, ParentB, Mardi_07));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(propositions.AllSnapshots()); // AUCUNE Proposition créée
        Assert.Empty(periodes.AllSnapshots());     // aucun jour écrit
    }

    [Fact]
    public void Should_Refuser_sans_ecrire_When_delegataire_est_soi_meme_sur_la_plage()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();

        // La plage [J1..J3] est résolue par le fond = Parent A. Proposer à Parent A (soi-même) → refus.
        var resultat = Handler(periodes, propositions)
            .Handle(new ProposerEchangeCommand(Mardi_07, LeaId, ParentA, Jeudi_09));

        Assert.False(resultat.EstSucces);
        Assert.Empty(propositions.AllSnapshots());
        Assert.Empty(periodes.AllSnapshots());
    }

    [Fact]
    public void Should_Refuser_avant_ecriture_When_delegataire_inconnu_orphelin()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var orphelin = "acteur-absent-" + Guid.NewGuid().ToString("N");

        var resultat = Handler(periodes, propositions)
            .Handle(new ProposerEchangeCommand(Mardi_07, LeaId, orphelin, Jeudi_09));

        Assert.False(resultat.EstSucces);
        Assert.Empty(propositions.AllSnapshots());
        Assert.Empty(periodes.AllSnapshots()); // AUCUN jour de la plage écrit
    }
}
