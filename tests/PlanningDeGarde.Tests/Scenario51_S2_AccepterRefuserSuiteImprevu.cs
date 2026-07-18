using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 51 — Sc.2 — Accepter compose la délégation s44 ; refuser sans écriture (adaptateur InMemory) (@back)
//   Étant donné une Proposition pending ISSUE D'UN IMPRÉVU (jour J, enfant E, versActeur B)
//   Quand le recevant B ACCEPTE la proposition
//   Alors AccepterProposition COMPOSE la délégation s44 : surcharge du jour J vers B + transfert bicolore dérivé s31 (R24)
//   Et la proposition passe à « accepté », la case J converge sur le nouveau responsable
//   Et si le recevant REFUSE à la place, la proposition passe à « refusé » SANS aucune écriture (store intact)
//
// Frontière Application : la proposition greffée sur imprévu (Sc.1) est un échange s47 STANDARD — accepter/refuser
// se comportent à l'identique (aucun chemin d'écriture neuf). L'imprévu reste au journal, jamais lu par la résolution.
public class Scenario51_S2_AccepterRefuserSuiteImprevu
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);  // ISO 28 paire → fond Alice (index 0)
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10); // même semaine ISO 28 → fond Alice

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Composer_la_delegation_a_l_acceptation_et_ne_rien_ecrire_au_refus_dune_proposition_greffee()
    {
        // --- Given : foyer InMemory réel, cycle N=2 (index 0 pair → Alice), jour J résolu par le fond Alice ---
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var periodes = new InMemoryPeriodeRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var journal = new InMemoryJournalChangements();
        var horloge = new HorlogeFigee(new DateTime(2026, 7, 1, 8, 30, 0));
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var proposer = new ProposerEchangeSuiteImprevuHandler(journal, new ProposerEchangeHandler(grille, propositions, config));
        var accepter = new AccepterPropositionHandler(propositions, new DeleguerRecuperationHandler(grille, periodes, config));
        var refuser = new RefuserPropositionHandler(propositions);
        var signaler = new SignalerImprevuHandler(journal, horloge, grille);

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- Given : un imprévu « malade » consigné sur le jour J, PUIS une proposition greffée Alice → Bruno ---
        var imprevu = signaler.Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, LeaId, TypeImprevu.Malade, aliceId)).Valeur!;
        var proposition = proposer.Handle(new ProposerEchangeSuiteImprevuCommand(imprevu.Id, brunoId)).Valeur!;
        Assert.Empty(periodes.AllSnapshots()); // pending greffée n'écrit rien

        // --- When : le recevant Bruno ACCEPTE ---
        var accepte = accepter.Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // --- Then : la délégation s44 est composée — surcharge d'UN jour, responsable Bruno ---
        var surcharge = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(brunoId, surcharge.ResponsableId);
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(surcharge.Fin));

        // --- Then : la case J converge sur Bruno (surcharge > fond) + transfert bicolore AUTO-DÉRIVÉ Alice → Bruno ---
        var caseJour = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(brunoId, caseJour.ResponsableId);
        Assert.NotNull(caseJour.Transfert);
        Assert.Equal("Alice", caseJour.Transfert!.NomDepart);
        Assert.Equal("Bruno", caseJour.Transfert.NomArrivee);

        // --- Then : la Proposition passe à « accepté » ---
        Assert.Equal(StatutProposition.Acceptee, propositions.ParId(proposition.Id)!.Statut);

        // --- Then : l'imprévu d'origine reste au journal, inchangé (fait informatif, jamais lu par la résolution) ---
        var imprevuApres = journal.Tout().Single(e => e.Type == TypeChangement.Imprevu);
        Assert.Equal(imprevu.Id, imprevuApres.Id);

        // --- REFUSER : une autre proposition greffée (autre imprévu, même semaine ISO → fond Alice) refusée sans écriture ---
        var imprevu2 = signaler.Handle(new SignalerImprevuCommand(Vendredi_10_07_2026, LeaId, TypeImprevu.Retard, aliceId)).Valeur!;
        var pending2 = proposer.Handle(new ProposerEchangeSuiteImprevuCommand(imprevu2.Id, brunoId)).Valeur!;
        var surchargesAvantRefus = periodes.AllSnapshots().Count;

        var refuse = refuser.Handle(new RefuserPropositionCommand(pending2.Id));
        Assert.True(refuse.EstSucces);

        // --- Then : « refusé », AUCUNE surcharge écrite (store des surcharges inchangé), case du jour refusé intacte ---
        Assert.Equal(StatutProposition.Refusee, propositions.ParId(pending2.Id)!.Statut);
        Assert.Equal(surchargesAvantRefus, periodes.AllSnapshots().Count);
        Assert.Equal(aliceId, CaseDuJour(grille, Vendredi_10_07_2026).ResponsableId);
    }
}
