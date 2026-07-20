using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 51 — Sc.1 — Proposer un échange EN RÉACTION à un imprévu crée une proposition pending SANS écriture (@back)
//   Étant donné un imprévu « malade » consigné au journal s48 sur un jour J pour un enfant E
//   Quand un parent propose un échange EN RÉACTION à cet imprévu vers un délégataire éligible (versActeur)
//   Alors une Proposition s47 « pending » est créée, PRÉ-REMPLIE avec le jour J et l'enfant E de l'imprévu
//   Et le store des surcharges reste INTACT (aucune surcharge, aucun transfert dérivé, case J inchangée)
//   Et l'imprévu d'origine reste consigné au journal, INCHANGÉ (fait informatif non muté par la proposition)
//   Et un événement de proposition distinct entre dans le flux (proposition ≠ imprévu, deux événements)
//
// Frontière Application : ProposerEchangeSuiteImprevuHandler (COMPOSITION journal s48 + ProposerEchange s47).
public class Scenario51_S1_ProposerEchangeSuiteImprevu
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond index 0 (Alice)

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Proposer_un_echange_greffe_sur_l_imprevu_sans_ecrire_ni_muter_l_imprevu()
    {
        // --- Given : foyer InMemory réel, cycle N=2 (index 0 pair → Alice), jour J résolu par le fond Alice ---
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        var cycleDef = new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }); cycle.DefinirCycle(cycleDef); cycle.DefinirCycle(cycleDef, LeaId);

        var periodes = new InMemoryPeriodeRepository();
        var transferts = new InMemoryTransfertRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 30, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), transferts);

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- Given : un imprévu « malade » consigné au journal s48 sur le jour J pour Léa, signalé par Alice ---
        var imprevu = new SignalerImprevuHandler(journal, horloge, grille)
            .Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, TypeImprevu.Malade, aliceId)).Valeur!;
        Assert.Single(journal.Tout());

        // --- When : Alice propose un échange EN RÉACTION à cet imprévu, vers Bruno (versActeur) ---
        var handler = new ProposerEchangeSuiteImprevuHandler(
            journal, new ProposerEchangeHandler(grille, propositions, config));
        var resultat = handler.Handle(new ProposerEchangeSuiteImprevuCommand(imprevu.Id, brunoId));

        // --- Then : une Proposition « pending » PRÉ-REMPLIE avec le jour J et l'enfant E de l'imprévu ---
        Assert.True(resultat.EstSucces);
        var proposition = Assert.Single(propositions.AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(Mercredi_08_07_2026, proposition.Jour); // jour hérité de l'imprévu
        Assert.Equal(LeaId, proposition.EnfantId);           // enfant hérité de l'imprévu
        Assert.Equal(aliceId, proposition.DeActeurId);       // cédant = responsable résolu du jour
        Assert.Equal(brunoId, proposition.VersActeurId);     // recevant choisi par le proposant

        // --- Then (anti vert-qui-ment) : store des surcharges INTACT, aucun transfert dérivé, case inchangée ---
        Assert.Empty(periodes.AllSnapshots());
        Assert.Empty(transferts.AllSnapshots());
        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- Then : l'imprévu d'origine reste consigné au journal, INCHANGÉ (fait informatif non muté) ---
        var imprevuApres = Assert.Single(journal.Tout());
        Assert.Equal(imprevu.Id, imprevuApres.Id);
        Assert.Equal(TypeChangement.Imprevu, imprevuApres.Type);
        Assert.Equal(TypeImprevu.Malade, imprevuApres.Imprevu);
        Assert.Equal(Mercredi_08_07_2026, imprevuApres.Jour);

        // --- Then : proposition ≠ imprévu — DEUX événements distincts (imprévu au journal, proposition au store) ---
        Assert.NotEqual(imprevu.Id, proposition.Id);
    }
}
