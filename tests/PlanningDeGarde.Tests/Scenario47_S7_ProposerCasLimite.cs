using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 47 — Sc.7 — Cas limite & erreur : refus AVANT écriture, idempotence, robustesse (@back)
//   - PROPOSER à SOI-MÊME (recevant = responsable déjà résolu) → REFUS explicite, aucune Proposition, aucune surcharge
//   - PROPOSER vers un délégataire INCONNU / orphelin (id absent du store) → refus AVANT écriture, store intact
//   - SECONDE proposition sur même jour/enfant déjà porteur d'un pending → last-write-wins R11, une seule pending sans doublon
//   - Proposition sur un jour HORS fenêtre chargée → enregistrement valide (une date), sans crash
//
// Frontière Application (ProposerEchangeHandler).
public class Scenario47_S7_ProposerCasLimite
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string ParentC = "parent-c";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);    // ISO 28 paire → fond Parent A
    private static readonly DateOnly LointainHorsFenetre = new(2027, 3, 17);   // très loin de toute fenêtre par défaut

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange", [ParentC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno", [ParentC] = "Chloe" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    private static ProposerEchangeHandler Handler(IPeriodeRepository periodes, IPropositionEchangeRepository propositions)
        => new(Grille(periodes), propositions, new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    [Fact]
    public void Should_Refuser_sans_ecrire_When_proposer_a_soi_meme()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        // Jour J résolu par le fond = Parent A. Proposer à Parent A (soi-même) → refus.
        var resultat = Handler(periodes, propositions).Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, ParentA));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(propositions.AllSnapshots()); // aucune Proposition
        Assert.Empty(periodes.AllSnapshots());     // aucune surcharge
    }

    [Fact]
    public void Should_Refuser_avant_ecriture_When_recevant_inconnu_orphelin()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var orphelin = "acteur-absent-" + Guid.NewGuid().ToString("N");

        var resultat = Handler(periodes, propositions).Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, orphelin));

        Assert.False(resultat.EstSucces);
        Assert.Empty(propositions.AllSnapshots());
        Assert.Empty(periodes.AllSnapshots());
    }

    [Fact]
    public void Should_Last_write_wins_sans_doublon_When_seconde_proposition_sur_le_meme_jour_enfant()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var handler = Handler(periodes, propositions);

        var premiere = handler.Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, ParentB));
        Assert.True(premiere.EstSucces);
        var seconde = handler.Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, ParentC));
        Assert.True(seconde.EstSucces);

        // Une seule Proposition pending subsiste (celle de la dernière écriture), sans doublon.
        var pending = Assert.Single(propositions.AllSnapshots().Where(p => p.Statut == StatutProposition.Proposee));
        Assert.Equal(ParentC, pending.VersActeurId);
        Assert.NotEqual(premiere.Valeur!.Id, pending.Id); // l'ancienne pending a bien été retirée
    }

    [Fact]
    public void Should_Enregistrer_sans_crash_When_le_jour_est_hors_de_la_fenetre_chargee()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();

        // Recevant Parent C (hors cycle de fond → jamais le résolu, donc distinct du cédant quel qu'il soit) :
        // le seul enjeu ici est que l'enregistrement d'un jour lointain ne crashe pas.
        var resultat = Handler(periodes, propositions).Handle(new ProposerEchangeCommand(LointainHorsFenetre, LeaId, ParentC));

        Assert.True(resultat.EstSucces);
        var pending = Assert.Single(propositions.AllSnapshots());
        Assert.Equal(LointainHorsFenetre, pending.Jour);
        Assert.Equal(ParentC, pending.VersActeurId);
        Assert.Empty(periodes.AllSnapshots()); // toujours aucune surcharge (pending n'écrit rien)
    }
}
