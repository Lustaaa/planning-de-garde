using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.5 — Cycle d'une seule semaine : aucune alternance, même responsable partout (@limite)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (⚠️ early green ATTENDU, tranché CP). N=1 ⇒
//   index = ISOWeek(jour) mod 1 = 0 pour TOUTE date ⇒ index 0 ⇒ Parent A partout, mécaniquement
//   (aucune branche neuve). Le cas N=1 = « responsable de fond unique » (tranche de secours, option 3)
//   est couvert PAR CONSTRUCTION par la couche de résolution du Sc.1 : ce filet verrouille l'absence
//   d'alternance et la légende à un seul responsable, sans inventer de faux rouge.
//
//   L'acceptation RUNTIME (grille câblée rendant le même fond sur 5 semaines, légende à une entrée,
//   front WASM + API distante + SignalR) est menée séparément par ihm-builder. On NE teste PAS ici
//   un rendu Blazor.
//
//   Données (déterministes, via Projeter(dateReference) — jamais Now) : parent-a = Alice bleu (seed
//   Foyer). Cycle N=1, index 0 → parent-a. Aucune période. Date de référence = lundi 29/06/2026
//   (ISO 27). Fenêtre 5 semaines : ISO 27→31, toutes Parent A bleu, aucune alternance.
public class Scenario5_CycleUneSemaineSansAlternance
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27

    private static Dictionary<int, string> MappingIndexZeroParentA()
        => new() { [0] = ParentA }; // N=1 : un seul index, index 0 → Parent A

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le Gherkin sans IHM : via DefinirCycleHandler (cycle d'UNE seule semaine, index 0 →
    // Parent A, SUCCÈS) puis GrilleAgendaQuery.Projeter(29/06/2026) sur le MÊME store de cycle, les
    // 35 jours (ISO 27→31) affichent tous Alice/bleu en fond, sans aucune alternance, et la légende
    // ne comporte qu'une seule entrée : Parent A bleu, sur l'identifiant stable.
    [Fact]
    public void Acceptation_Should_Afficher_Alice_bleu_sur_toutes_les_semaines_affichees_sans_alternance_et_une_legende_a_un_seul_responsable_When_le_cycle_de_fond_ne_compte_qu_une_seule_semaine()
    {
        var foyer = new ConfigurationFoyerEnMemoire();              // parent-a=Alice/bleu (seed Foyer)
        var cycleStore = new CycleDeFondEnMemoire();
        var handler = new DefinirCycleHandler(cycleStore, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new DefinirCycleCommand(1, MappingIndexZeroParentA()));
        Assert.True(resultat.EstSucces);

        var grille = QueryAvec(cycleStore, foyer).Projeter(Lundi_29_06_2026);

        // Toutes les semaines affichées (ISO 27→31) → Parent A bleu, sans alternance
        Assert.All(grille.Jours, j =>
        {
            Assert.Equal(Alice, j.NomResponsable);
            Assert.Equal(Bleu, j.CouleurResponsable);
        });
        // Légende : un seul responsable de fond, Parent A bleu, sur l'id stable
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(ParentA, entree.IdentifiantStable);
        Assert.Equal(Alice, entree.Nom);
        Assert.Equal(Bleu, entree.Couleur);
    }

    // ---------- Test unitaire (boucle interne, TDD) ----------

    // Test #1 — Caractérisation (⚠️ early green attendu, PAS driver) : N=1 ⇒ ISOWeek mod 1 = 0 pour
    // toute date ⇒ même index 0 sur les 35 jours de la fenêtre ⇒ Parent A partout, aucune alternance.
    // Filet de non-régression verrouillant le cas dégénéré « cycle à 1 semaine = responsable unique »
    // (couvert par construction par la formule du Sc.1) et la légende dédoublonnée à une seule entrée.
    [Fact]
    public void Should_Afficher_le_meme_responsable_de_fond_sur_toutes_les_semaines_sans_alternance_et_une_legende_a_un_seul_responsable_When_le_cycle_ne_compte_qu_une_seule_semaine()
    {
        var grille = QueryAvec(CycleUneSemaine()).Projeter(Lundi_29_06_2026);

        // Aucune alternance : un seul responsable distinct de fond sur toute la fenêtre, et c'est Alice
        var responsablesDistincts = grille.Jours.Select(j => j.NomResponsable).Distinct().ToList();
        Assert.Equal(new[] { Alice }, responsablesDistincts);
        Assert.All(grille.Jours, j => Assert.Equal(Bleu, j.CouleurResponsable));

        // Légende : exactement un responsable, Parent A bleu, sur l'id stable
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(ParentA, entree.IdentifiantStable);
        Assert.Equal(Alice, entree.Nom);
        Assert.Equal(Bleu, entree.Couleur);
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle, ConfigurationFoyerEnMemoire foyer)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(), foyer, foyer, cycle);

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice }),
            cycle);

    private static IReferentielCycleDeFond CycleUneSemaine()
        => new FakeReferentielCycleDeFond(new CycleDeFond(1, MappingIndexZeroParentA()));
}
