using System;
using System.IO;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.6 (🖥️ @ihm, calendrier) — la barre de navigation temporelle et les sélecteurs sont
/// regroupés proprement. Test de NIVEAU RUNTIME (grille réelle, harnais Sc.07) : les trois contrôles de
/// navigation temporelle (semaine précédente / aujourd'hui / semaine suivante) sont réunis dans un même
/// groupe segmenté, à l'intérieur de la barre de navigation. Une garde d'asset vérifie que la barre se
/// replie proprement (flex-wrap) et reste tokenisée (clair ET sombre). Les data-testid, observables et
/// flux restent intacts (Sc.14 le prouve) : seule la présentation est regroupée.
/// </summary>
public sealed class FrontWasmGrilleBarreNavRegroupeeTests : TestContext
{
    [Fact]
    public void Les_controles_de_navigation_temporelle_sont_reunis_dans_un_groupe_segmente()
    {
        // Given — la grille réelle rendue (Parent connecté via le harnais).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — les 3 boutons de navigation temporelle sont réunis dans un même groupe, dans la barre.
        var barre = grille.Find("[data-testid='barre-navigation']");
        var groupeTemps = barre.QuerySelector(".barre-nav-temporelle");
        Assert.NotNull(groupeTemps);
        Assert.NotNull(groupeTemps!.QuerySelector("[data-testid='nav-semaine-precedente']"));
        Assert.NotNull(groupeTemps.QuerySelector("[data-testid='nav-aujourdhui']"));
        Assert.NotNull(groupeTemps.QuerySelector("[data-testid='nav-semaine-suivante']"));
    }

    [Fact]
    public void La_barre_se_replie_proprement_et_reste_tokenisee()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var razor = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "PlanningDeGarde.Web", "Components", "Pages", "PlanningPartage.razor"));

        // Repli responsive (pas de casse de la grille sur écran étroit) et groupe segmenté tokenisé.
        Assert.Contains("barre-nav-temporelle", razor, StringComparison.Ordinal);
        Assert.Contains("flex-wrap", razor, StringComparison.Ordinal);
        Assert.Contains(".barre-navigation", razor, StringComparison.Ordinal);
    }
}
