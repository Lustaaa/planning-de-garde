using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 52 — Sc.3 — Accepter COMPOSE la délégation-plage s45 (surcharge multi-jours + transferts 2 frontières) (@back)
//   Étant donné une Proposition pending sur la plage [J1..J3] pour un enfant, vers un délégataire B
//   Quand le recevant ACCEPTE la proposition
//   Alors une surcharge est posée sur CHAQUE jour [J1..J3] (le délégataire prime sur le fond)
//   Et un transfert bicolore AUTO-DÉRIVÉ s31 apparaît à l'ENTRÉE (frontière J1)
//   Et un transfert bicolore AUTO-DÉRIVÉ s31 apparaît à la SORTIE (frontière J3+1)
//   Et la Proposition passe à « accepté »
//
// Frontière Application : ACCEPTER COMPOSE DeleguerRecuperationHandler s45 sur la PLAGE portée par la proposition.
public class Scenario52_S3_AccepterComposeDelegationPlage
{
    private const string LeaId = "enfant-lea";

    // ISO 28 (semaine du lundi 06/07/2026) → index 0 (pair) → Parent A par le fond.
    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);    // J1
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8); // J2
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);    // J3
    private static readonly DateOnly Vendredi_10 = new(2026, 7, 10); // J3+1 (sortie, fond A de nouveau)

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_Should_Composer_la_delegation_plage_s45_a_l_acceptation()
    {
        // --- Given : foyer InMemory réel, cycle N=2 (index 0 pair → Alice), plage résolue par le fond Alice ---
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

        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(aliceId, CaseDuJour(grille, j).ResponsableId);

        // --- Une proposition pending Alice → Bruno sur la PLAGE [J1..J3] ---
        var proposition = proposer.Handle(new ProposerEchangeCommand(Mardi_07, LeaId, brunoId, Jeudi_09)).Valeur!;
        Assert.Empty(periodes.AllSnapshots()); // pending n'écrit rien

        // --- When : le recevant ACCEPTE ---
        var accepte = accepter.Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // --- Then : la délégation-plage s45 est composée — UNE surcharge couvrant [J1..J3], responsable Bruno ---
        var surcharge = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(brunoId, surcharge.ResponsableId);
        Assert.Equal(Mardi_07, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Jeudi_09, DateOnly.FromDateTime(surcharge.Fin));

        // --- Then : chaque jour de la plage fait PRIMER Bruno (surcharge > fond) ---
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(brunoId, CaseDuJour(grille, j).ResponsableId);

        // --- Then : transfert AUTO-DÉRIVÉ à l'ENTRÉE (J1) et à la SORTIE (J3+1) ---
        var entree = CaseDuJour(grille, Mardi_07);
        Assert.NotNull(entree.Transfert);
        Assert.Equal("Alice", entree.Transfert!.NomDepart);
        Assert.Equal("Bruno", entree.Transfert.NomArrivee);

        var sortie = CaseDuJour(grille, Vendredi_10);
        Assert.NotNull(sortie.Transfert);
        Assert.Equal("Bruno", sortie.Transfert!.NomDepart);
        Assert.Equal("Alice", sortie.Transfert.NomArrivee);

        // --- Then : la Proposition passe à « accepté » ---
        Assert.Equal(StatutProposition.Acceptee, propositions.ParId(proposition.Id)!.Statut);
    }
}
