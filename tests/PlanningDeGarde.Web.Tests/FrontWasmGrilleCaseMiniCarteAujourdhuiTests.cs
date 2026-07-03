using System;
using System.IO;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.4 (🖥️ @ihm, cœur du calendrier) — la case du jour est une mini-carte tokenisée et
/// « aujourd'hui » est visuellement marqué. Deux appuis complémentaires :
/// <list type="bullet">
///   <item><b>runtime</b> (grille réelle câblée à l'API distante, harnais Sc.07) : la case dont la date
///     est celle du jour (port d'horloge figé) porte le marqueur <c>data-aujourdhui="1"</c>, les autres
///     non — comportement réel de la grille, pas une doublure ;</item>
///   <item><b>garde d'asset</b> sur le style de la grille : les cases sont des mini-cartes tokenisées
///     (surface/bordure via <c>var(--pdg-*)</c>, plus de couleurs de chrome codées en dur) et une règle
///     dédiée marque « aujourd'hui » — condition du rendu correct en clair ET en sombre.</item>
/// </list>
/// Les couleurs de responsabilité (bleu/orange, inline, <c>data-couleur</c>) et tous les data-testid
/// restent intacts (Sc.14 le prouve).
/// </summary>
public sealed class FrontWasmGrilleCaseMiniCarteAujourdhuiTests : TestContext
{
    [Fact]
    public void La_case_du_jour_est_marquee_aujourdhui_et_les_autres_non()
    {
        // Given — la grille réelle rendue avec le port d'horloge figé au 29/06/2026 (ancre par défaut).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — la case du 29/06 est marquée « aujourd'hui » ; une autre case (30/06) ne l'est pas.
        var aujourdhui = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        var autre = GrilleRuntimeHarness.CaseDuJour(grille, "30/06");
        Assert.Equal("1", aujourdhui.GetAttribute("data-aujourdhui"));
        Assert.Null(autre.GetAttribute("data-aujourdhui"));
    }

    [Fact]
    public void Les_cases_sont_des_mini_cartes_tokenisees_et_aujourdhui_a_un_style_dedie()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var razor = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "PlanningDeGarde.Web", "Components", "Pages", "PlanningPartage.razor"));

        // Chrome de la carte tokenisé (fonctionne en clair ET en sombre).
        Assert.Contains("var(--pdg-border)", razor, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-card)", razor, StringComparison.Ordinal);
        // Une règle dédiée marque « aujourd'hui ».
        Assert.Contains("grille-jour-aujourdhui", razor, StringComparison.Ordinal);
        // Plus de couleurs de chrome codées en dur (elles cassaient le thème sombre).
        Assert.DoesNotContain("#dee2e6", razor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#555", razor, StringComparison.OrdinalIgnoreCase);
    }
}
