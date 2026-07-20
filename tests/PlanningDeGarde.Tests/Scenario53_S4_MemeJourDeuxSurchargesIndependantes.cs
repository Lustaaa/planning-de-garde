using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.4 — Même jour, deux enfants : deux surcharges INDÉPENDANTES (@back)
//   Étant donné deux enfants "Léa" et "Tom" et un même jour J
//   Quand je délègue (Léa, J) vers l'acteur A, puis (Tom, J) vers l'acteur B
//   Alors DEUX surcharges coexistent : (Léa, J)→A et (Tom, J)→B
//   Et la seconde écriture n'écrase PAS la première (le LWW R11 ne joue que par (enfant, jour))
//   Et chaque case résout son propre responsable et son propre transfert dérivé
//
// L'isolation du LWW ENTRE enfants a été RED-prouvée en Sc.2 ; ici la SÉQUENCE de deux délégations le confirme.
public class Scenario53_S4_MemeJourDeuxSurchargesIndependantes
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private static readonly DateOnly J = new(2026, 7, 8); // mercredi, ISO 28 → index 0 → fond alice (les deux enfants)

    private static JourCase Case(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J);

    [Fact]
    public void Acceptation_InMemory_Deux_delegations_le_meme_jour_coexistent()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;
        var david = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("David")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        var cycleDef = new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }); cycle.DefinirCycle(cycleDef, "enfant-lea"); cycle.DefinirCycle(cycleDef, "enfant-tom");

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());
        var delegation = new DeleguerRecuperationHandler(grille, periodes, config);

        // When — déléguer (Léa, J) → Carla, PUIS (Tom, J) → David.
        Assert.True(delegation.Handle(new DeleguerRecuperationCommand(J, LeaId, carla)).EstSucces);
        Assert.True(delegation.Handle(new DeleguerRecuperationCommand(J, TomId, david)).EstSucces);

        // Then — DEUX surcharges coexistent, la seconde n'écrase PAS la première.
        var toutes = periodes.AllSnapshots();
        Assert.Equal(2, toutes.Count);
        Assert.Equal(carla, Assert.Single(toutes, p => p.EnfantId == LeaId).ResponsableId);
        Assert.Equal(david, Assert.Single(toutes, p => p.EnfantId == TomId).ResponsableId);

        // Then — chaque case résout SON responsable et SON transfert dérivé (fond Alice → chacun son délégataire).
        var caseLea = Case(grille, LeaId);
        Assert.Equal(carla, caseLea.ResponsableId);
        Assert.Equal("Alice", caseLea.Transfert!.NomDepart);
        Assert.Equal("Carla", caseLea.Transfert.NomArrivee);

        var caseTom = Case(grille, TomId);
        Assert.Equal(david, caseTom.ResponsableId);
        Assert.Equal("Alice", caseTom.Transfert!.NomDepart);
        Assert.Equal("David", caseTom.Transfert.NomArrivee);
    }
}
