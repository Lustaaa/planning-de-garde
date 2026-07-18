using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 52 — Sc.4 — Refuser une proposition de plage retire SANS écriture (@back)
//   Étant donné une Proposition pending sur la plage [J1..J3]
//   Quand le recevant REFUSE la proposition
//   Alors la Proposition passe à « refusé »
//   Et AUCUNE surcharge n'est posée sur aucun jour de la plage
//   Et le store des surcharges reste STRICTEMENT intact
public class Scenario52_S4_RefuserPlageSansEcriture
{
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);    // J1
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8); // J2
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);    // J3

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Refuser_la_plage_sans_ecrire()
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

        var proposer = new ProposerEchangeHandler(grille, propositions, config);
        var refuser = new RefuserPropositionHandler(propositions);

        var proposition = proposer.Handle(new ProposerEchangeCommand(Mardi_07, LeaId, brunoId, Jeudi_09)).Valeur!;
        Assert.Empty(periodes.AllSnapshots());

        // --- When : le recevant REFUSE la plage ---
        var refuse = refuser.Handle(new RefuserPropositionCommand(proposition.Id));
        Assert.True(refuse.EstSucces);

        // --- Then : « refusé », AUCUNE surcharge sur aucun jour, store intact, plage inchangée (Alice au fond) ---
        Assert.Equal(StatutProposition.Refusee, propositions.ParId(proposition.Id)!.Statut);
        Assert.Empty(periodes.AllSnapshots());
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
        {
            Assert.Equal(aliceId, CaseDuJour(grille, j).ResponsableId);
            Assert.Null(CaseDuJour(grille, j).Transfert);
        }
    }
}
