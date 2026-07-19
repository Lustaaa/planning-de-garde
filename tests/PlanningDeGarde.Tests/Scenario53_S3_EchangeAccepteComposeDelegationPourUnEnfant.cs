using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.3 — Échange accepté ciblé "Léa" compose la délégation pour "Léa" SEUL (@back)
//   Étant donné une proposition d'échange pending sur (Léa, J) — store des surcharges intact
//   Quand la proposition est acceptée
//   Alors AccepterProposition compose la délégation s44 pour (Léa, J) uniquement
//   Et la surcharge + transfert dérivé apparaissent pour "Léa"
//   Et aucune surcharge ni transfert n'est écrit pour "Tom" (résolution de Tom inchangée)
//
// La proposition porte le cédant RÉSOLU DE L'ENFANT ciblé (Léa), jamais pollué par la surcharge de Tom.
public class Scenario53_S3_EchangeAccepteComposeDelegationPourUnEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private static readonly DateOnly J = new(2026, 7, 8); // mercredi, ISO 28 → index 0 → fond alice

    private static JourCase Case(GrilleAgendaQuery grille, DateOnly jour, string enfantId)
        => grille.Projeter(jour, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Acceptation_InMemory_Echange_Lea_compose_delegation_isolee_de_Tom()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        var periodes = new InMemoryPeriodeRepository();
        var propositions = new InMemoryPropositionEchangeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var delegation = new DeleguerRecuperationHandler(grille, periodes, config);
        var proposer = new ProposerEchangeHandler(grille, propositions, config);
        var accepter = new AccepterPropositionHandler(propositions, delegation);

        // Given — Tom a sa propre surcharge (Carla) le jour J ; Léa résolue par le fond (Alice).
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            carla, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), TomId).Valeur!);
        Assert.Equal(alice, Case(grille, J, LeaId).ResponsableId);
        Assert.Equal(carla, Case(grille, J, TomId).ResponsableId);

        // When — proposer un échange (Léa, J) → Bob : la proposition porte le cédant DE LÉA (Alice), pas Carla.
        var proposition = proposer.Handle(new ProposerEchangeCommand(J, LeaId, bob)).Valeur!;
        Assert.Equal(alice, proposition.DeActeurId);           // cédant résolu de Léa, isolé de la surcharge de Tom
        Assert.Single(periodes.AllSnapshots());                 // pending n'écrit rien : store surcharges intact (juste Tom)

        // When — accepter.
        Assert.True(accepter.Handle(new AccepterPropositionCommand(proposition.Id)).EstSucces);

        // Then — délégation composée pour Léa SEUL : 2 surcharges coexistent (Léa→Bob, Tom→Carla).
        var toutes = periodes.AllSnapshots();
        Assert.Equal(2, toutes.Count);
        Assert.Equal(bob, Assert.Single(toutes, p => p.EnfantId == LeaId).ResponsableId);
        Assert.Equal(carla, Assert.Single(toutes, p => p.EnfantId == TomId).ResponsableId);

        // Then — Léa : Bob prime + transfert dérivé Alice → Bob ; Tom : Carla inchangé, aucun transfert issu de Léa.
        var caseLea = Case(grille, J, LeaId);
        Assert.Equal(bob, caseLea.ResponsableId);
        Assert.Equal("Alice", caseLea.Transfert!.NomDepart);
        Assert.Equal("Bob", caseLea.Transfert.NomArrivee);
        Assert.Equal(carla, Case(grille, J, TomId).ResponsableId);
    }
}
