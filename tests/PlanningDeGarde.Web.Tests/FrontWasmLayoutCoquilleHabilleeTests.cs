using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.10 (🖥️ @ihm, layout) — la coquille applicative (nav, marque, menu utilisateur, bandeaux
/// d'alerte, switch de thème) est cohérente « Cocon élevé ». Garde d'asset (pas bUnit) sur les feuilles de
/// style réellement livrées :
/// <list type="bullet">
///   <item>l'ossature (barre latérale / barre supérieure) est tokenisée <c>--pdg-*</c> (plus de couleurs de
///     chrome codées en dur) — condition du rendu correct clair ET sombre ;</item>
///   <item>les bandeaux d'alerte sont adoucis (variantes tendres, pas le rouge/jaune Bootstrap brut) ;</item>
///   <item>le switch de thème (Sc.3) est intégré/habillé (règle <c>.bascule-theme</c>).</item>
/// </list>
/// Les data-testid, observables et flux restent intacts (Sc.14 le prouve).
/// </summary>
public sealed class FrontWasmLayoutCoquilleHabilleeTests
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
    public void L_ossature_du_layout_est_tokenisee_sans_couleur_de_chrome_codee_en_dur()
    {
        var css = Lire("Components", "Shared", "Layout", "MainLayout.razor.css");
        Assert.Contains("var(--pdg-accent", css, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-card)", css, StringComparison.Ordinal);
        // Les couleurs de chrome codées en dur cassaient la cohérence / le thème sombre.
        Assert.DoesNotContain("#34a890", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#fffdf9", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Les_bandeaux_d_alerte_sont_adoucis()
    {
        var css = Lire("wwwroot", "app.css");
        Assert.Contains(".content .alert-danger", css, StringComparison.Ordinal);
        Assert.Contains(".content .alert-warning", css, StringComparison.Ordinal);
        Assert.Contains(".content .alert-success", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Le_switch_de_theme_est_integre_et_habille()
    {
        var css = Lire("wwwroot", "app.css");
        Assert.Contains(".bascule-theme", css, StringComparison.Ordinal);
    }
}
