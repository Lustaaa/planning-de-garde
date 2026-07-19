using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.2 — Délégation ciblée "Léa" n'écrit RIEN sur "Tom" (@back)
//   Étant donné deux enfants "Léa" et "Tom" et un jour J
//   Quand je délègue la récupération de "Léa" le jour J à un autre acteur
//   Alors une surcharge est écrite pour (Léa, J) et son transfert bicolore dérivé s31 apparaît
//   Et le store des surcharges de "Tom" est STRICTEMENT intact (sa surcharge propre survit)
//   Et la case (Tom, J) reste résolue exactement comme avant l'écriture
//   Et l'isolation est prouvée à l'identique sur InMemory ET Mongo durable
//
// Ancré sur la règle : responsable = surcharge(enfant,jour) > fond(enfant,jour) > neutre.
public class Scenario53_S2_DelegationCibleeNEcritRienSurLautre
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string PA = "p-a", PB = "p-b", PC = "p-c";

    // Cycle PARTAGÉ N=2 {0:pA, 1:pB} : J résolu par le fond (pA) pour Léa → déléguer à pB est un vrai changement.
    private static readonly CycleDeFond CyclePartage = new(2, new Dictionary<int, string> { [0] = PA, [1] = PB });
    private static readonly System.DateOnly J = new(2026, 7, 8); // mercredi, ISO 28 → index 0 → fond pA

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange", [PC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob", [PC] = "Carla" }),
            new FakeReferentielCycleDeFond(CyclePartage),
            new FakeEnumerationActeursFoyer(PA, PB, PC));

    private static JourCase Case(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J);

    [Fact]
    public void Acceptation_Deleguer_Lea_laisse_la_surcharge_de_Tom_intacte()
    {
        var periodes = new FakePeriodeRepository();
        var grille = Grille(periodes);

        // Given — Tom a SA PROPRE surcharge le jour J (Carla), scope enfant = Tom.
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            PC, J.ToDateTime(System.TimeOnly.MinValue), J.ToDateTime(System.TimeOnly.MinValue), TomId).Valeur!);

        // Précondition : Léa résolue par le fond (pA) ; Tom résolu sur SA surcharge (Carla).
        Assert.Equal(PA, Case(grille, LeaId).ResponsableId);
        Assert.Equal(PC, Case(grille, TomId).ResponsableId);

        // When — je délègue la récupération de Léa le jour J à pB.
        var resultat = new DeleguerRecuperationHandler(grille, periodes, new FakeEnumerationActeursFoyer(PA, PB, PC))
            .Handle(new DeleguerRecuperationCommand(J, LeaId, PB));
        Assert.True(resultat.EstSucces);

        // Then — DEUX surcharges coexistent : (Léa,J)→pB et (Tom,J)→pC, chacune scope à son enfant.
        var toutes = periodes.AllSnapshots();
        Assert.Equal(2, toutes.Count);
        var surchargeLea = Assert.Single(toutes, p => p.EnfantId == LeaId);
        Assert.Equal(PB, surchargeLea.ResponsableId);
        var surchargeTom = Assert.Single(toutes, p => p.EnfantId == TomId);
        Assert.Equal(PC, surchargeTom.ResponsableId); // store de Tom STRICTEMENT intact

        // Then — Léa fait primer pB + transfert dérivé pA → pB.
        var caseLea = Case(grille, LeaId);
        Assert.Equal(PB, caseLea.ResponsableId);
        Assert.NotNull(caseLea.Transfert);
        Assert.Equal("Alice", caseLea.Transfert!.NomDepart);
        Assert.Equal("Bob", caseLea.Transfert.NomArrivee);

        // Then — la case (Tom, J) reste résolue EXACTEMENT comme avant (Carla, aucun transfert issu de Léa).
        Assert.Equal(PC, Case(grille, TomId).ResponsableId);
    }

    [Fact]
    public void Acceptation_InMemory_Isolation_delegation()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // Tom a sa surcharge (Carla) le jour J.
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            carla, J.ToDateTime(System.TimeOnly.MinValue), J.ToDateTime(System.TimeOnly.MinValue), TomId).Valeur!);

        var resultat = new DeleguerRecuperationHandler(grille, periodes, config)
            .Handle(new DeleguerRecuperationCommand(J, LeaId, bob));
        Assert.True(resultat.EstSucces);

        Assert.Equal(bob, grille.Projeter(J, VuePlanning.Semaine, LeaId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.Equal(carla, grille.Projeter(J, VuePlanning.Semaine, TomId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.Equal(carla, Assert.Single(periodes.AllSnapshots(), p => p.EnfantId == TomId).ResponsableId);
    }
}
