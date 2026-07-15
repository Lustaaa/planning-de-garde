using System;
using System.Linq;
using Bunit;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 44 — Sc.7 (🖥️ @ihm, décision PO au gate G3) — acceptation de NIVEAU RUNTIME : la page planning
/// réellement câblée (API distante réelle, store réel, projection réelle) ne rend PLUS ni la carte
/// « Aujourd'hui » (s42) ni le panneau « À venir » (s43) — ces deux surfaces de LECTURE sont retirées. La
/// GRILLE AGENDA devient la SEULE surface de lecture de la page, et la délégation par le MENU CLIC-CASE
/// (Sc.4-6) demeure fonctionnelle jusqu'au store distant réel.
///
/// Anti « vert qui ment » : l'absence des surfaces est prouvée sur la page réellement rendue (câblage réel),
/// et la délégation transite jusqu'au store réel (relecture par la projection réelle) — pas une doublure.
/// </summary>
public sealed class FrontWasmPlanningSansCarteNiPanneauRuntimeTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026

    [Fact]
    public void La_page_ne_rend_ni_carte_aujourdhui_ni_panneau_a_venir_la_grille_reste_seule_surface()
    {
        // Given — la page planning réellement câblée à l'API distante (store réel), rendue.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Then — ni carte « Aujourd'hui » (s42) ni panneau « À venir » (s43) ne sont rendus (surfaces retirées).
        Assert.Empty(grille.FindAll("[data-testid='carte-aujourdhui']"));
        Assert.Empty(grille.FindAll("[data-testid='panneau-a-venir']"));

        // … et la grille agenda reste la SEULE surface de lecture (28 cases-jour de la fenêtre par défaut).
        Assert.Equal(28, grille.FindAll("[data-testid='jour-case']").Count);
    }

    [Fact]
    public void La_delegation_par_le_menu_clic_case_demeure_fonctionnelle_sur_la_grille()
    {
        // Given — grille câblée réelle (store vierge), Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — délégation via le MENU CLIC-CASE sur la case du jour (29/06) : clic → menu → entrée
        // « déléguer ce jour » → mini-dialog → choix d'Alice (parent-a) → valider (canal d'écriture).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-deleguer']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — la dialog se ferme ET la case du jour de la grille converge vers « Alice » (surcharge relue).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']"));
                Assert.Equal(
                    "Alice",
                    GrilleRuntimeHarness.CaseDuJour(grille, "29/06").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));
    }
}
