using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Layout;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.3 (🖥️ @ihm, fondation switch) — le switch clair/sombre persiste le choix et prime sur
/// la préférence système. Deux appuis complémentaires :
/// <list type="bullet">
///   <item><b>garde d'asset</b> sur le module <c>window.pdgTheme</c> (index.html) : <c>definir</c> écrit
///     <c>localStorage['pdg-theme']</c> ET applique <c>data-theme</c> sur &lt;html&gt; (le choix persisté
///     prime au chargement suivant car l'amorce lit localStorage en premier — Sc.2) ; <c>lire</c> retourne
///     le thème appliqué ; et le switch est présent dans le layout ;</item>
///   <item><b>comportement du switch</b> : sur clic, il bascule et <b>persiste le choix via le port</b>
///     <see cref="IPreferencesTheme"/> (doublé à la main par un spy — seul le port est doublé). L'effet réel
///     (localStorage + data-theme) est porté par l'adaptateur JS et vérifié par la garde d'asset + gate G3.</item>
/// </list>
/// </summary>
public sealed class FondationSwitchThemePersisteTests : TestContext
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

    private static string LireIndexHtml()
        => File.ReadAllText(Path.Combine(RacineWeb(), "wwwroot", "index.html"));

    private static string LireMainLayout()
        => File.ReadAllText(Path.Combine(RacineWeb(), "Components", "Layout", "MainLayout.razor"));

    [Fact]
    public void Le_module_pdgTheme_definir_persiste_localStorage_et_applique_data_theme()
    {
        var html = LireIndexHtml().Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        Assert.Contains("pdgTheme", html, StringComparison.Ordinal);
        // definir : persiste le choix explicite ET l'applique immédiatement.
        Assert.Contains("localStorage.setItem('pdg-theme'", html, StringComparison.Ordinal);
        Assert.Contains("setAttribute('data-theme'", html, StringComparison.Ordinal);
        // lire : expose le thème appliqué pour le switch.
        Assert.Contains("lire", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Le_switch_de_theme_est_present_dans_le_layout()
        => Assert.Contains("<BasculeTheme", LireMainLayout(), StringComparison.Ordinal);

    [Fact]
    public void Basculer_persiste_le_choix_via_le_port_et_bascule_clair_sombre_clair()
    {
        // Given — le switch câblé au port de préférence (spy), thème courant initial « clair ».
        var spy = new SpyPreferencesTheme("clair");
        Services.AddSingleton<IPreferencesTheme>(spy);
        var switchTheme = RenderComponent<BasculeTheme>();

        // When — premier clic → sombre (persisté), second clic → clair (persisté).
        switchTheme.Find("[data-testid='bascule-theme']").Click();
        Assert.Equal("sombre", spy.DernierThemeDefini);

        switchTheme.Find("[data-testid='bascule-theme']").Click();
        Assert.Equal("clair", spy.DernierThemeDefini);

        // Then — chaque bascule a bien persisté un choix (deux écritures).
        Assert.Equal(new[] { "sombre", "clair" }, spy.ThemesDefinis);
    }

    private sealed class SpyPreferencesTheme : IPreferencesTheme
    {
        private readonly string _themeInitial;

        public SpyPreferencesTheme(string themeInitial) => _themeInitial = themeInitial;

        public List<string> ThemesDefinis { get; } = new();

        public string? DernierThemeDefini => ThemesDefinis.Count == 0 ? null : ThemesDefinis[^1];

        public ValueTask<string> ThemeCourantAsync() => ValueTask.FromResult(_themeInitial);

        public ValueTask DefinirAsync(string theme)
        {
            ThemesDefinis.Add(theme);
            return ValueTask.CompletedTask;
        }
    }
}
