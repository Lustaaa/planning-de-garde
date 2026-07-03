using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.13 (🖥️ @ihm, transverse responsive) — rendu correct sur Safari iOS / WebKit à parité
/// avec le PC. Garde d'asset sur les feuilles réellement livrées :
/// <list type="bullet">
///   <item>le viewport active les zones sûres (<c>viewport-fit=cover</c>, index.html) ;</item>
///   <item>la hauteur pleine page utilise <c>100dvh</c> (évite le piège <c>100vh</c> de la barre d'URL iOS)
///     et respecte <c>env(safe-area-inset-*)</c> (encoche / barre home) — barres sticky comprises ;</item>
///   <item>les cibles tactiles font ≥ 44px sur pointeur grossier (<c>@media (pointer: coarse)</c>).</item>
/// </list>
/// Les data-testid, observables et flux restent intacts (Sc.14 le prouve).
/// </summary>
public sealed class FrontWasmResponsiveSafariIosTests
{
    private static string RacineWeb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "PlanningDeGarde.Web");
    }

    private static string Lire(params string[] segments)
        => File.ReadAllText(Path.Combine(RacineWeb(), Path.Combine(segments)));

    [Fact]
    public void Le_viewport_active_les_zones_sures_iOS()
    {
        var html = Lire("wwwroot", "index.html");
        Assert.Contains("viewport-fit=cover", html, StringComparison.Ordinal);
    }

    [Fact]
    public void La_hauteur_pleine_page_est_robuste_et_respecte_les_zones_sures()
    {
        var css = Lire("Components", "Layout", "MainLayout.razor.css");
        Assert.Contains("100dvh", css, StringComparison.Ordinal);            // évite le piège 100vh iOS
        Assert.Contains("env(safe-area-inset-top", css, StringComparison.Ordinal); // encoche / sticky
    }

    [Fact]
    public void Les_cibles_tactiles_font_au_moins_44px_sur_pointeur_grossier()
    {
        var css = Lire("wwwroot", "app.css");
        Assert.Contains("pointer: coarse", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", css, StringComparison.Ordinal);
    }
}
