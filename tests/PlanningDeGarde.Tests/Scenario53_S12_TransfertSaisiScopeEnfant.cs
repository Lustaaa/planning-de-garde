using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.12 — Un transfert SAISI (s29) issu d'une action sur l'enfant A n'apparaît QUE chez A (@back)
//   Étant donné deux enfants "Léa" et "Tom" et un transfert SAISI défini EN VUE de "Léa" le jour J
//   Quand je projette la grille pour "Léa" puis pour "Tom"
//   Alors la case J de "Léa" porte le transfert bicolore (+ motif de légende), la case J de "Tom" NON
//   Et l'isolation est prouvée sur InMemory (et sur Mongo durable dans Api.Tests)
//
// Gate G3 : « le transfert de Mia apparaît toujours sur le planning de Charlie » — les transferts SAISIS
// n'étaient pas scopés (le modèle ne portait pas d'EnfantId). Correctif = même isolation que les périodes.
public class Scenario53_S12_TransfertSaisiScopeEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string PA = "p-a", PB = "p-b";
    private static readonly DateOnly J = new(2026, 7, 8);

    private static GrilleAgendaQuery Grille(ITransfertRepository transferts)
        => new(
            new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange", ["ecole"] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob" }),
            new FakeReferentielCycleDeFond(),
            new FakeEnumerationActeursFoyer(PA, PB),
            null,
            transferts);

    private static JourCase Case(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J);

    [Fact]
    public void Acceptation_Transfert_saisi_en_vue_Lea_visible_de_Lea_seul()
    {
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(
            PA, PB, "ecole", new TimeSpan(8, 30, 0), J.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);
        var grille = Grille(transferts);

        // Then — la case J de Léa porte le transfert bicolore (Alice → Bob) ; Léa a le motif de légende.
        var caseLea = Case(grille, LeaId);
        Assert.NotNull(caseLea.Transfert);
        Assert.Equal("Alice", caseLea.Transfert!.NomDepart);
        Assert.Equal("Bob", caseLea.Transfert.NomArrivee);
        Assert.NotEmpty(grille.Projeter(J, VuePlanning.Semaine, LeaId).LégendeMotifs);

        // Then — la case J de Tom ne porte AUCUN transfert (le transfert de Léa ne fuit pas), pas de motif.
        Assert.Null(Case(grille, TomId).Transfert);
        Assert.Empty(grille.Projeter(J, VuePlanning.Semaine, TomId).LégendeMotifs);
    }

    [Fact]
    public void Acceptation_InMemory_Transfert_saisi_scope_enfant()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;

        var transferts = new InMemoryTransfertRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(), config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), transferts);

        new DefinirTransfertHandler(transferts)
            .Handle(new DefinirTransfertCommand(alice, bob, "ecole", new TimeSpan(8, 30, 0), J.ToDateTime(TimeOnly.MinValue), LeaId));

        Assert.NotNull(Case(grille, LeaId).Transfert);
        Assert.Null(Case(grille, TomId).Transfert);
        Assert.Equal(LeaId, Assert.Single(transferts.AllSnapshots()).EnfantId);
    }
}
