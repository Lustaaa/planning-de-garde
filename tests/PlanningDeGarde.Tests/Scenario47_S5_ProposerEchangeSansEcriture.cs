using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 47 — Sc.5 — PROPOSER crée une notif pending SANS écrire de surcharge (@back)
//   Étant donné un foyer avec un responsable de fond résolu pour un jour (A) et un acteur tiers connu (B)
//   Quand un parent PROPOSE l'échange de ce jour vers B (ProposerEchange)
//   Alors une Proposition "proposé" (pending) est enregistrée, adressée au recevant B
//   Et AUCUNE surcharge n'est écrite : le store des surcharges est INCHANGÉ (identique à avant)
//   Et la résolution de la case reste "surcharge > fond" inchangée (aucun basculement, aucun transfert dérivé)
//
// POINT DE VIGILANCE (anti vert-qui-ment) : un pending qui teinterait déjà la case serait une délégation
// déguisée (s44), pas un échange consenti. On PROUVE le store des surcharges INTACT + la case inchangée.
public class Scenario47_S5_ProposerEchangeSansEcriture
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Parent A

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB()), LeaId),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Enregistrer_une_proposition_pending_sans_ecrire_ni_changer_la_resolution()
    {
        var periodes = new FakePeriodeRepository();
        var propositions = new FakePropositionEchangeRepository();
        var grille = Grille(periodes);

        // Précondition : le jour J est RÉSOLU PAR LE FOND (Parent A), aucune surcharge, aucun transfert.
        var avant = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(ParentA, avant.ResponsableId);
        Assert.Null(avant.Transfert);
        Assert.Empty(periodes.AllSnapshots());

        // When — un parent PROPOSE l'échange du jour J vers Parent B.
        var resultat = new ProposerEchangeHandler(grille, propositions, new FakeEnumerationActeursFoyer(ParentA, ParentB))
            .Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, ParentB));

        Assert.True(resultat.EstSucces);

        // Then — une Proposition "proposé" (pending), adressée au recevant B, cédant = responsable résolu A.
        var proposition = Assert.Single(propositions.AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(ParentB, proposition.VersActeurId);
        Assert.Equal(ParentA, proposition.DeActeurId);
        Assert.Equal(Mercredi_08_07_2026, proposition.Jour);

        // Then — ANTI vert-qui-ment : AUCUNE surcharge écrite, store des surcharges INTACT.
        Assert.Empty(periodes.AllSnapshots());

        // Then — la résolution de la case est INCHANGÉE (A prime toujours par le fond, aucun transfert dérivé).
        var apres = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(ParentA, apres.ResponsableId);
        Assert.Null(apres.Transfert);
    }

    // ---------- Acceptation runtime — adaptateurs InMemory RÉELS (1er des deux adaptateurs) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Proposer_via_les_adaptateurs_reels_sans_ecrire()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        var cycleDef = new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }); cycle.DefinirCycle(cycleDef); cycle.DefinirCycle(cycleDef, LeaId);

        var periodes = new InMemoryPeriodeRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        var resultat = new ProposerEchangeHandler(grille, propositions, config)
            .Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, brunoId));
        Assert.True(resultat.EstSucces);

        var proposition = Assert.Single(propositions.AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(brunoId, proposition.VersActeurId);
        Assert.Equal(aliceId, proposition.DeActeurId);

        // Store des surcharges INTACT, case inchangée (Alice par le fond).
        Assert.Empty(periodes.AllSnapshots());
        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);
    }
}
