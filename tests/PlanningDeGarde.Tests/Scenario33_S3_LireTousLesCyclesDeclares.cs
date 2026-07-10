using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 33 — Sc.3 — Lire TOUS les cycles déclarés / actifs du foyer (@back)
//   Tranche BACKEND (frontière Application) : corrige le trou de lecture (retour PO gate s32) — des
//   cycles déclarés n'apparaissaient PAS dans la config (aucune query ne les exposait, seule l'écriture
//   existait). Le cycle de fond porte plusieurs affectations de semaine ; la query en restitue
//   l'intégralité, chacune identifiée de façon stable (index de semaine) avec son attribut persisté
//   (id de responsable). Un foyer sans cycle déclaré renvoie une liste vide. On pilote le store réel
//   du cycle (CycleDeFondEnMemoire) via le use case d'écriture, puis on lit via la query.
public class Scenario33_S3_LireTousLesCyclesDeclares
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un foyer où PLUSIEURS cycles (affectations de semaine) sont déclarés : la query de configuration
    // les renvoie TOUS, chacun identifié de façon stable (index) avec son id de responsable persisté.
    [Fact]
    public void Acceptation_Should_Renvoyer_l_integralite_des_cycles_declares_chacun_identifie_stablement_avec_son_attribut_persiste_When_plusieurs_cycles_sont_declares()
    {
        var store = new CycleDeFondEnMemoire();
        new DefinirCycleHandler(store, new FakeNotificateurPlanning())
            .Handle(new DefinirCycleCommand(2, new Dictionary<int, string> { [0] = ParentA, [1] = ParentB }));
        var query = new CyclesFoyerQuery(store);

        var cycles = query.Lire();

        Assert.Equal(2, cycles.Count); // l'INTÉGRALITÉ des cycles déclarés (aucun invisible)
        Assert.Contains(cycles, c => c.IndexSemaine == 0 && c.ResponsableId == ParentA);
        Assert.Contains(cycles, c => c.IndexSemaine == 1 && c.ResponsableId == ParentB);
    }

    // ---------- Test #1 — Driver : un foyer sans cycle déclaré renvoie une liste vide, sans erreur ----------
    [Fact]
    public void Should_Renvoyer_une_liste_vide_sans_erreur_When_aucun_cycle_n_est_declare()
    {
        var store = new CycleDeFondEnMemoire();
        var query = new CyclesFoyerQuery(store);

        var cycles = query.Lire();

        Assert.Empty(cycles); // pas d'erreur, liste vide
    }
}
