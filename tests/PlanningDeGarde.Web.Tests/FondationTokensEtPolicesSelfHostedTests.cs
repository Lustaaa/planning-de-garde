using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.1 (🖥️ @ihm, fondation « Cocon élevé ») — garde-fou d'ASSET sur la feuille globale
/// et les polices self-hosted. Ce n'est pas une preuve bUnit (aucun rendu, aucune DI, aucun render mode) :
/// c'est une assertion pure sur les fichiers statiques réellement livrés au navigateur WASM. Le vert
/// « visuel » (Fraunces titres / Inter corps, surfaces tokenisées) est constaté au runtime + gate G3 ;
/// ce test verrouille le contrat structurel qui le rend possible :
/// <list type="bullet">
///   <item>les tokens <c>--pdg-*</c> (bg, card, accent, ink, muted, border) sont exposés dans <c>:root</c> ;</item>
///   <item>les polices Fraunces (titres) et Inter (corps) sont déposées sous <c>wwwroot/fonts/</c> et
///     déclarées <c>@font-face</c> avec une source LOCALE (offline-friendly) ;</item>
///   <item>aucun écran n'émet de requête vers un CDN de polices externe (fonts.googleapis.com /
///     fonts.gstatic.com) — ni dans <c>app.css</c>, ni dans <c>index.html</c>.</item>
/// </list>
/// </summary>
public sealed class FondationTokensEtPolicesSelfHostedTests
{
    private static string RacineWwwroot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir); // la racine de la solution doit être retrouvable depuis les tests
        return Path.Combine(dir!.FullName, "src", "PlanningDeGarde.Web", "wwwroot");
    }

    private static string LireAppCss() => File.ReadAllText(Path.Combine(RacineWwwroot(), "app.css"));

    private static string LireIndexHtml() => File.ReadAllText(Path.Combine(RacineWwwroot(), "index.html"));

    [Theory]
    [InlineData("--pdg-bg")]
    [InlineData("--pdg-card")]
    [InlineData("--pdg-accent")]
    [InlineData("--pdg-ink")]
    [InlineData("--pdg-muted")]
    [InlineData("--pdg-border")]
    public void Le_token_pdg_est_expose_dans_root(string token)
    {
        var css = LireAppCss();
        var root = css[css.IndexOf(":root", StringComparison.Ordinal)..];
        Assert.Contains(token + ":", root.Replace(" ", string.Empty), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("fraunces-latin.woff2")]
    [InlineData("inter-latin.woff2")]
    public void La_police_self_hosted_est_deposee_et_non_vide(string fichier)
    {
        var chemin = Path.Combine(RacineWwwroot(), "fonts", fichier);
        Assert.True(File.Exists(chemin), $"police self-hosted attendue : {chemin}");
        Assert.True(new FileInfo(chemin).Length > 1000, "le fichier de police doit être un vrai woff2");
    }

    [Theory]
    [InlineData("Inter")]
    public void La_police_est_declaree_font_face_avec_source_locale(string famille)
    {
        var css = LireAppCss();
        Assert.Contains($"font-family: '{famille}'", css, StringComparison.Ordinal);
        Assert.Contains("url('fonts/", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Les_titres_et_le_corps_utilisent_Inter()
    {
        var css = LireAppCss().Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        // Identité « Studio » (refonte 2026-07) : sans-serif intégrale — titres ET corps sur Inter (le
        // caractère vient du poids et du tracking, plus d'un serif éditorial). Un seul token de police utile.
        Assert.Contains("--pdg-font-corps:'Inter'", css, StringComparison.Ordinal);
        Assert.Contains("--pdg-font-titre:'Inter'", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Aucune_reference_a_un_cdn_de_polices_externe()
    {
        foreach (var contenu in new[] { LireAppCss(), LireIndexHtml() })
        {
            Assert.DoesNotContain("fonts.googleapis.com", contenu, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fonts.gstatic.com", contenu, StringComparison.OrdinalIgnoreCase);
        }
    }
}
