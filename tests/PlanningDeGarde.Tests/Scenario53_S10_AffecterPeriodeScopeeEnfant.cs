using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.10 — Affecter une période EN VUE enfant A (chemin d'ÉCRITURE RÉEL) → visible A SEUL (@back)
//   Étant donné deux enfants "Léa" et "Tom" et l'enfant "Léa" courant (vue)
//   Quand j'affecte une période via AffecterPeriodeHandler (use case d'écriture s06) EN VUE de "Léa"
//   Alors la période porte EnfantId="Léa" et n'apparaît QUE dans la grille de "Léa", jamais chez "Tom"
//   Et l'isolation est prouvée à l'identique sur InMemory (et sur Mongo durable dans Api.Tests)
//
// Le TROU du gate G3 : Sc.1-6 seedaient des périodes DÉJÀ estampillées ; ici on passe par le use case
// d'écriture RÉEL (comme la dialog), qui doit estampiller l'enfant courant (Option A).
public class Scenario53_S10_AffecterPeriodeScopeeEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private static readonly DateOnly J = new(2026, 7, 8); // mercredi, ISO 28 → index 0 → fond parent-a

    [Fact]
    public void Acceptation_InMemory_Periode_affectee_en_vue_Lea_visible_de_Lea_seul()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        var cycleDef = new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }); cycle.DefinirCycle(cycleDef, "enfant-lea"); cycle.DefinirCycle(cycleDef, "enfant-tom");

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // When — j'affecte une période (Carla) le jour J EN VUE de Léa via le use case d'écriture RÉEL (s06).
        var resultat = new AffecterPeriodeHandler(periodes, new FakeResponsableRepository())
            .Handle(new AffecterPeriodeCommand(carla, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), LeaId));
        Assert.True(resultat.EstSucces);

        // Then — la période persistée porte bien EnfantId = Léa (pas le bucket partagé "").
        Assert.Equal(LeaId, Assert.Single(periodes.AllSnapshots()).EnfantId);

        // Then — la case J de Léa fait primer Carla (surcharge) ; celle de Tom résout SON fond (Alice), jamais Carla.
        Assert.Equal(carla, grille.Projeter(J, VuePlanning.Semaine, LeaId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.Equal(alice, grille.Projeter(J, VuePlanning.Semaine, TomId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.DoesNotContain(grille.Projeter(J, VuePlanning.Semaine, TomId).Jours, j => j.ResponsableId == carla);
    }
}
