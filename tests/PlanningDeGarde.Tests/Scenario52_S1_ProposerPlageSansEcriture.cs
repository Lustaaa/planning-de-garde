using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 52 — Sc.1 — Proposer sur une PLAGE [J1..J3] crée UNE Proposition pending SANS aucune écriture (@back)
//   Étant donné un enfant et un délégataire connus du foyer, les cases J1, J2, J3 résolues sur le fond (A)
//   Quand un parent PROPOSE un échange de l'enfant sur la plage [J1..J3] vers B
//   Alors UNE SEULE Proposition « pending » est créée, portant l'intervalle [J1..J3] et l'enfant
//   Et AUCUNE surcharge n'est posée (store des surcharges STRICTEMENT intact)
//   Et les cases J1, J2, J3 restent résolues sur le fond (aucune bascule de responsable)
//
// ANTI vert-qui-ment (s47) : un pending qui teinterait déjà une case de la plage serait une délégation
// déguisée (s45), pas un échange consenti. On PROUVE le store des surcharges INTACT + les 3 cases inchangées.
public class Scenario52_S1_ProposerPlageSansEcriture
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    // ISO 28 (semaine du lundi 06/07/2026) → index 0 (pair) → Parent A par le fond ; aucune surcharge ces jours-là.
    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);    // J1
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8); // J2
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);    // J3

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Enregistrer_une_proposition_pending_sur_la_plage_sans_ecrire()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var grille = Grille(periodes);

        // Précondition : les 3 jours de la plage sont RÉSOLUS PAR LE FOND (Parent A), aucune surcharge.
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
        {
            Assert.Equal(ParentA, CaseDuJour(grille, j).ResponsableId);
            Assert.Null(CaseDuJour(grille, j).Transfert);
        }
        Assert.Empty(periodes.AllSnapshots());

        // When — un parent PROPOSE l'échange de la PLAGE [J1..J3] vers Parent B.
        var resultat = new ProposerEchangeHandler(grille, propositions, new FakeEnumerationActeursFoyer(ParentA, ParentB))
            .Handle(new ProposerEchangeCommand(Mardi_07, LeaId, ParentB, Jeudi_09));

        Assert.True(resultat.EstSucces);

        // Then — UNE SEULE Proposition « pending » portant l'intervalle [J1..J3] et l'enfant, cédant résolu A.
        var proposition = Assert.Single(propositions.AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(ParentB, proposition.VersActeurId);
        Assert.Equal(ParentA, proposition.DeActeurId);
        Assert.Equal(LeaId, proposition.EnfantId);
        Assert.Equal(Mardi_07, proposition.Jour);
        Assert.Equal(Jeudi_09, proposition.JourFin);

        // Then — ANTI vert-qui-ment : AUCUNE surcharge écrite, store des surcharges STRICTEMENT intact.
        Assert.Empty(periodes.AllSnapshots());

        // Then — les 3 cases de la plage restent RÉSOLUES PAR LE FOND (aucune bascule, aucun transfert dérivé).
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
        {
            Assert.Equal(ParentA, CaseDuJour(grille, j).ResponsableId);
            Assert.Null(CaseDuJour(grille, j).Transfert);
        }
    }

    // ---------- Acceptation runtime — adaptateurs InMemory RÉELS ----------
    [Fact]
    public void Acceptation_InMemory_Should_Proposer_une_plage_via_les_adaptateurs_reels_sans_ecrire()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var periodes = new InMemoryPeriodeRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(aliceId, CaseDuJour(grille, j).ResponsableId);

        var resultat = new ProposerEchangeHandler(grille, propositions, config)
            .Handle(new ProposerEchangeCommand(Mardi_07, LeaId, brunoId, Jeudi_09));
        Assert.True(resultat.EstSucces);

        var proposition = Assert.Single(propositions.AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(brunoId, proposition.VersActeurId);
        Assert.Equal(aliceId, proposition.DeActeurId);
        Assert.Equal(Mardi_07, proposition.Jour);
        Assert.Equal(Jeudi_09, proposition.JourFin);

        // Store des surcharges INTACT, les 3 cases inchangées (Alice par le fond).
        Assert.Empty(periodes.AllSnapshots());
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(aliceId, CaseDuJour(grille, j).ResponsableId);
    }
}
