using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 51 — Sc.4 — Cas erreur / invariant : refus avant écriture, modèles imprévu/proposition séparés (@back)
//   Étant donné un imprévu consigné et une demande de proposition greffée en réaction
//   Quand le versActeur est SOI-MÊME, ou un délégataire INCONNU, ou un acteur ORPHELIN
//   Alors la proposition est REFUSÉE AVANT écriture (aucune Proposition créée, aucune écriture partielle, store intact)
//   Et dans tous les cas l'imprévu d'origine reste un FAIT informatif au journal, non muté, non « résolu »
//   Et la résolution ne consulte jamais le journal (imprévu s48 et proposition s47 restent des modèles SÉPARÉS)
//
// Frontière Application : la composition s51 hérite des gardes de ProposerEchange s47 (refus AVANT écriture).
public class Scenario51_S4_CasErreurInvariantSuiteImprevu
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Alice (index 0)

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    private sealed record Contexte(
        ProposerEchangeSuiteImprevuHandler Proposer, InMemoryPropositionEchangeRepository Propositions,
        InMemoryPeriodeRepository Periodes, InMemoryTransfertRepository Transferts,
        InMemoryJournalChangements Journal, GrilleAgendaQuery Grille, string AliceId, string BrunoId, string ImprevuId);

    private static Contexte Monter()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var periodes = new InMemoryPeriodeRepository();
        var transferts = new InMemoryTransfertRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 30, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), transferts);

        // Imprévu « malade » consigné sur le jour J (résolu par Alice) — signalé par Alice.
        var imprevu = new SignalerImprevuHandler(journal, horloge, grille)
            .Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, TypeImprevu.Malade, aliceId)).Valeur!;

        return new Contexte(
            new ProposerEchangeSuiteImprevuHandler(journal, new ProposerEchangeHandler(grille, propositions, config)),
            propositions, periodes, transferts, journal, grille, aliceId, brunoId, imprevu.Id);
    }

    private static void AssertRefusSansAucuneEcriture(Contexte c, Result<PropositionEchangeSnapshot> resultat)
    {
        // Refus AVANT écriture : aucun succès, aucune Proposition créée, aucune écriture partielle (store intact).
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(c.Propositions.AllSnapshots());
        Assert.Empty(c.Periodes.AllSnapshots());
        Assert.Empty(c.Transferts.AllSnapshots());

        // L'imprévu d'origine reste un FAIT informatif au journal, non muté, non « résolu » par la tentative.
        var imprevu = Assert.Single(c.Journal.Tout());
        Assert.Equal(c.ImprevuId, imprevu.Id);
        Assert.Equal(TypeChangement.Imprevu, imprevu.Type);
        Assert.Equal(TypeImprevu.Malade, imprevu.Imprevu);

        // La résolution ne consulte jamais le journal : la case reste résolue par le fond (Alice), imprévu ignoré.
        Assert.Equal(c.AliceId, CaseDuJour(c.Grille, Mercredi_08_07_2026).ResponsableId);
    }

    [Fact]
    public void Should_Refuser_avant_ecriture_When_versActeur_est_soi_meme()
    {
        var c = Monter();
        // Proposer vers Alice = responsable RÉSOLU du jour (soi-même) → refus AVANT écriture.
        var resultat = c.Proposer.Handle(new ProposerEchangeSuiteImprevuCommand(c.ImprevuId, c.AliceId));
        AssertRefusSansAucuneEcriture(c, resultat);
    }

    [Fact]
    public void Should_Refuser_avant_ecriture_When_versActeur_inconnu_ou_orphelin()
    {
        var c = Monter();
        var orphelin = "acteur-absent-" + Guid.NewGuid().ToString("N");
        var resultat = c.Proposer.Handle(new ProposerEchangeSuiteImprevuCommand(c.ImprevuId, orphelin));
        AssertRefusSansAucuneEcriture(c, resultat);
    }
}
