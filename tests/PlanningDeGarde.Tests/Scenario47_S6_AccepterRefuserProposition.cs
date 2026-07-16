using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 47 — Sc.6 — ACCEPTER compose la délégation s44 ; REFUSER retire sans écriture (@back)
//   Étant donné une Proposition "proposé" (pending) sur un jour, adressée à un recevant connu
//   Quand le recevant ACCEPTE (AccepterProposition)
//   Alors la délégation EXISTANTE s44 est composée : une surcharge du jour est écrite, le recevant prime (surcharge > fond)
//   Et le transfert cédant → recevant est AUTO-DÉRIVÉ (s31, R24), jamais réécrit
//   Et la Proposition passe à "accepté"
//   Quand une autre Proposition pending est REFUSÉE (RefuserProposition)
//   Alors elle passe à "refusé", AUCUNE surcharge n'est écrite, le store reste intact
//
// Frontière Application : ACCEPTER COMPOSE DeleguerRecuperationHandler (s44) ; REFUSER n'écrit rien.
public class Scenario47_S6_AccepterRefuserProposition
{
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond acteur index 0

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Composer_la_delegation_s44_a_l_acceptation_et_ne_rien_ecrire_au_refus()
    {
        // --- Given : foyer InMemory réel, cycle N=2 (index 0 pair → Alice), jour J résolu par le fond Alice ---
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

        var delegation = new DeleguerRecuperationHandler(grille, periodes, config);
        var proposer = new ProposerEchangeHandler(grille, propositions, config);
        var accepter = new AccepterPropositionHandler(propositions, delegation);
        var refuser = new RefuserPropositionHandler(propositions);

        Assert.Equal(aliceId, CaseDuJour(grille, Mercredi_08_07_2026).ResponsableId);

        // --- Une proposition pending Alice → Bruno sur le jour J ---
        var proposition = proposer.Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, LeaId, brunoId)).Valeur!;
        Assert.Empty(periodes.AllSnapshots()); // pending n'écrit rien

        // --- When : le recevant ACCEPTE ---
        var accepte = accepter.Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // --- Then : la délégation s44 est composée — surcharge d'UN jour, responsable Bruno ---
        var surcharge = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(brunoId, surcharge.ResponsableId);
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(surcharge.Fin));

        // --- Then : la case fait primer Bruno (surcharge > fond) + transfert AUTO-DÉRIVÉ Alice → Bruno ---
        var caseJour = CaseDuJour(grille, Mercredi_08_07_2026);
        Assert.Equal(brunoId, caseJour.ResponsableId);
        Assert.NotNull(caseJour.Transfert);
        Assert.Equal("Alice", caseJour.Transfert!.NomDepart);
        Assert.Equal("Bruno", caseJour.Transfert.NomArrivee);

        // --- Then : la Proposition passe à "accepté" ---
        Assert.Equal(StatutProposition.Acceptee, propositions.ParId(proposition.Id)!.Statut);

        // --- REFUSER : une autre pending (jour différent, MÊME semaine ISO → fond Alice) refusée sans écriture ---
        var autreJour = new DateOnly(2026, 7, 10); // vendredi, semaine ISO 28 (paire) → fond Alice, distinct de Bruno
        var pending2 = proposer.Handle(new ProposerEchangeCommand(autreJour, LeaId, brunoId)).Valeur!;
        var surchargesAvantRefus = periodes.AllSnapshots().Count;

        var refuse = refuser.Handle(new RefuserPropositionCommand(pending2.Id));
        Assert.True(refuse.EstSucces);

        // --- Then : "refusé", AUCUNE surcharge écrite (store des surcharges inchangé) ---
        Assert.Equal(StatutProposition.Refusee, propositions.ParId(pending2.Id)!.Statut);
        Assert.Equal(surchargesAvantRefus, periodes.AllSnapshots().Count);
        Assert.Equal(aliceId, CaseDuJour(grille, autreJour).ResponsableId); // case du jour refusé inchangée
    }
}
