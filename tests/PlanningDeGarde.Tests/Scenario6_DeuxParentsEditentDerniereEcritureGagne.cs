using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.6 — Deux parents éditent le cycle en même temps : dernière écriture gagne (@limite, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (⚠️ early green ATTENDU — tranché CP, 99-sprint10-retours.md,
//   décision « édition concurrente du cycle de fond », patron s08 Sc.7). Le store cycle singleton est une
//   UNITÉ DE COHÉRENCE PARTAGÉE éditée par AFFECTATION : deux DefinirCycleHandler (deux écrans) écrivent
//   successivement sur le MÊME store ; les deux éditions RÉUSSISSENT (aucune garde de conflit, aucun jeton
//   optimiste, aucun rejet) et la DERNIÈRE écriture ÉCRASE — dernière écriture gagne PAR CONSTRUCTION.
//   GrilleAgendaQuery relit le store partagé → résout le dernier mapping (Parent C vert sur l'index pair).
//   Aucune logique neuve : ce test DOCUMENTE l'absence de version/rejet ; il ne pilote aucun rouge.
//
//   La convergence des DEUX grilles « sans rechargement » est un fait RUNTIME/SignalR : prouvé séparément
//   par ihm-builder sur l'app câblée (deux clients, hub réel). On NE teste PAS ici un rendu Blazor ni la
//   diffusion réelle.
//
//   Données : cycle N=2 (pair → A, impair → B). parent-a = Alice bleu, parent-b = Bruno orange,
//   parent-c = Parent C VERT (acteur résolu vert). Écran 1 règle l'index pair sur Parent A ; écran 2,
//   juste après, règle l'index pair sur Parent C. ISO 28 (06–12/07/2026, PAIRE) doit afficher Parent C
//   vert, en case comme en légende ; aucune des deux éditions n'est rejetée.
public class Scenario6_DeuxParentsEditentDerniereEcritureGagne
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string ParentC = "parent-c";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string ParentCNom = "Parent C";
    private const string Bleu = "bleu";
    private const string Orange = "orange";
    private const string Vert = "vert";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27 — date de référence (fenêtre ISO 27→31)
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);  // ISO 28 (paire)
    private static readonly DateOnly Dimanche_12_07_2026 = new(2026, 7, 12);

    private static Dictionary<int, string> MappingPairAImpairB()
        => new() { [0] = ParentA, [1] = ParentB }; // écran 1 : index pair → Parent A, impair → Parent B

    private static Dictionary<int, string> MappingPairCImpairB()
        => new() { [0] = ParentC, [1] = ParentB }; // écran 2 : index pair → Parent C, impair → Parent B

    // Test #1 — Caractérisation (⚠️ early green attendu, pas driver) : deux écrans règlent successivement
    // l'index pair du cycle sur le MÊME store partagé (écran 1 → Parent A, écran 2 → Parent C). Les deux
    // éditions réussissent (aucun rejet, pas de version) et la dernière écrase ; une projection relisant le
    // store partagé résout Parent C vert sur ISO 28, en case comme en légende — dernière écriture gagne par
    // construction (patron s08 Sc.7). Aucune logique neuve.
    [Fact]
    public void Should_Resoudre_le_dernier_mapping_ecrit_sans_rejeter_aucune_edition_When_deux_ecrans_reglent_le_meme_index_du_cycle_l_un_apres_l_autre_dans_le_store_partage()
    {
        var cycleStorePartage = new CycleDeFondEnMemoire();             // unité de cohérence partagée (singleton)
        var ecran1 = new DefinirCycleHandler(cycleStorePartage, new FakeNotificateurPlanning());
        var ecran2 = new DefinirCycleHandler(cycleStorePartage, new FakeNotificateurPlanning());

        // Écran 1 puis écran 2 règlent l'index pair l'un après l'autre — les DEUX éditions sont acceptées
        var resultatEcran1 = ecran1.Handle(new DefinirCycleCommand(2, MappingPairAImpairB()));
        var resultatEcran2 = ecran2.Handle(new DefinirCycleCommand(2, MappingPairCImpairB()));
        Assert.True(resultatEcran1.EstSucces); // aucun rejet de la 1ʳᵉ écriture
        Assert.True(resultatEcran2.EstSucces); // aucun rejet de la 2ᵉ écriture (pas de jeton optimiste)

        // La grille relit le store partagé → dernier mapping écrit (écran 2) : Parent C vert sur ISO 28
        var grille = QueryAvec(cycleStorePartage).Projeter(Lundi_29_06_2026);
        Assert.All(JoursEntre(grille, Lundi_06_07_2026, Dimanche_12_07_2026), j =>
        {
            Assert.Equal(ParentCNom, j.NomResponsable);
            Assert.Equal(Vert, j.CouleurResponsable);
        });
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == ParentC && e.Nom == ParentCNom && e.Couleur == Vert);
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange, [ParentC] = Vert }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno, [ParentC] = ParentCNom }),
            cycle);

    private static IEnumerable<JourCase> JoursEntre(GrilleAgenda grille, DateOnly debut, DateOnly fin)
        => grille.Jours.Where(j => j.Date >= debut && j.Date <= fin);
}
