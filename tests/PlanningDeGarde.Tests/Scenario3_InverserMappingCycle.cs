using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.3 — Inverser le mapping du cycle met à jour la grille sans rechargement (@nominal, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (⚠️ early green ATTENDU — tranché CP, 99-sprint10-retours.md).
//   Ré-définir le cycle écrase le mapping sur le store (dernière écriture gagne, sans version) ;
//   GrilleAgendaQuery RELIT le port cycle à chaque projection → résout le NOUVEAU mapping. Aucune logique
//   neuve : re-projection d'un read model inchangé sur un état réécrit (réutilise la résolution du fond de
//   Sc.1 + l'écrasement du cycle par DefinirCycleHandler). Ce test verrouille la non-régression de la
//   re-projection après ré-définition ; il ne pilote aucun rouge.
//
//   Le « sans rechargement » est un fait RUNTIME/SignalR : il est prouvé séparément par ihm-builder sur
//   l'app câblée (la grille suit la diffusion). On NE teste PAS ici un rendu Blazor ni la diffusion réelle.
//
//   Données : cycle N=2, pair → parent-a (Alice bleu), impair → parent-b (Bruno orange). ISO 28
//   (06–12/07/2026, PAIRE) affiche d'abord Parent A bleu. Inversion : pair → parent-b, impair → parent-a.
//   Attendu après re-projection : ISO 28 affiche Parent B orange, en case comme en légende.
public class Scenario3_InverserMappingCycle
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27 — date de référence (fenêtre ISO 27→31)
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);  // ISO 28 (paire)
    private static readonly DateOnly Dimanche_12_07_2026 = new(2026, 7, 12);

    private static Dictionary<int, string> MappingPairAImpairB()
        => new() { [0] = ParentA, [1] = ParentB }; // index pair → Parent A, impair → Parent B

    private static Dictionary<int, string> MappingInverse()
        => new() { [0] = ParentB, [1] = ParentA }; // mapping inversé : index pair → Parent B, impair → Parent A

    // Test #1 — Caractérisation (⚠️ early green attendu, pas driver) : la grille affiche d'abord Parent A
    // bleu sur ISO 28 (cycle pair → Parent A) ; après ré-définition du cycle (mapping inversé) via le même
    // store, une nouvelle projection résout Parent B orange sur ISO 28, en case comme en légende — le read
    // model relit simplement le cycle réécrit (dernière écriture gagne, aucune logique neuve).
    [Fact]
    public void Should_Resoudre_Parent_B_orange_sur_la_semaine_ISO_28_When_le_mapping_du_cycle_a_ete_inverse()
    {
        var foyer = new ConfigurationFoyerEnMemoire();          // parent-a=Alice/bleu, parent-b=Bruno/orange (seed Foyer)
        var cycleStore = new CycleDeFondEnMemoire();
        var handler = new DefinirCycleHandler(cycleStore, new FakeNotificateurPlanning());
        var query = QueryAvec(cycleStore, foyer);

        // Cycle initial : index pair → Parent A → ISO 28 affiche Parent A bleu (état de départ)
        Assert.True(handler.Handle(new DefinirCycleCommand(2, MappingPairAImpairB())).EstSucces);
        var grilleAvant = query.Projeter(Lundi_29_06_2026);
        Assert.All(JoursEntre(grilleAvant, Lundi_06_07_2026, Dimanche_12_07_2026), j =>
        {
            Assert.Equal(Alice, j.NomResponsable);
            Assert.Equal(Bleu, j.CouleurResponsable);
        });

        // Un parent inverse le mapping depuis la configuration (ré-définition → écrasement du store)
        Assert.True(handler.Handle(new DefinirCycleCommand(2, MappingInverse())).EstSucces);

        // Nouvelle projection : ISO 28 affiche désormais Parent B orange, en case comme en légende
        var grilleApres = query.Projeter(Lundi_29_06_2026);
        Assert.All(JoursEntre(grilleApres, Lundi_06_07_2026, Dimanche_12_07_2026), j =>
        {
            Assert.Equal(Bruno, j.NomResponsable);
            Assert.Equal(Orange, j.CouleurResponsable);
        });
        Assert.Contains(grilleApres.Légende, e => e.IdentifiantStable == ParentB && e.Nom == Bruno && e.Couleur == Orange);
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle, ConfigurationFoyerEnMemoire foyer)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(), foyer, foyer, cycle);

    private static IEnumerable<JourCase> JoursEntre(GrilleAgenda grille, DateOnly debut, DateOnly fin)
        => grille.Jours.Where(j => j.Date >= debut && j.Date <= fin);
}
