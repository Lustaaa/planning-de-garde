using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.5 (🖥️ @ihm, calendrier) — le menu clic-case sépare visuellement les actions primaires
/// (poser / affecter / transfert / éditer) des actions destructives (supprimer), pour réduire l'hésitation.
/// Test de NIVEAU RUNTIME sur la grille réelle (harnais Sc.07, Parent connecté) : on ouvre le menu d'une
/// case et on vérifie la STRUCTURE de regroupement — les 6 entrées, leurs data-testid, leurs libellés et
/// leurs commandes restent inchangés (Sc.14 le prouve), seule leur présentation est hiérarchisée.
/// </summary>
public sealed class FrontWasmMenuClicCaseGroupesTests : TestContext
{
    [Fact]
    public void Le_menu_regroupe_les_actions_primaires_et_les_destructives_dans_des_groupes_distincts()
    {
        // Given — la grille réelle rendue (Parent connecté via le harnais).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // When — j'ouvre le menu d'actions sur une case.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
        grille.WaitForElement("[data-testid='menu-actions-case']", TimeSpan.FromSeconds(10));

        // Then — deux groupes distincts : primaires vs destructives.
        var primaire = grille.Find("[data-testid='menu-groupe-primaire']");
        var destructif = grille.Find("[data-testid='menu-groupe-destructif']");

        // Les actions primaires sont dans le groupe primaire.
        Assert.NotNull(primaire.QuerySelector("[data-testid='action-poser-slot']"));
        Assert.NotNull(primaire.QuerySelector("[data-testid='action-affecter-periode']"));
        Assert.NotNull(primaire.QuerySelector("[data-testid='action-definir-transfert']"));
        Assert.NotNull(primaire.QuerySelector("[data-testid='action-editer-periode']"));

        // Les actions destructives sont regroupées à part.
        Assert.NotNull(destructif.QuerySelector("[data-testid='action-supprimer-periode']"));
        Assert.NotNull(destructif.QuerySelector("[data-testid='action-supprimer-slot']"));

        // Séparation nette : aucune destructive dans le groupe primaire, aucune primaire dans le destructif.
        Assert.Null(primaire.QuerySelector("[data-testid='action-supprimer-periode']"));
        Assert.Null(primaire.QuerySelector("[data-testid='action-supprimer-slot']"));
        Assert.Null(destructif.QuerySelector("[data-testid='action-poser-slot']"));
    }
}
