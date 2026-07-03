using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.2 (🖥️ @ihm, fondation thème) — garde-fou d'ASSET sur l'amorçage du thème.
/// Pas une preuve bUnit : assertion pure sur <c>index.html</c> et <c>app.css</c> réellement livrés.
/// Contrat verrouillé :
/// <list type="bullet">
///   <item>un script <b>inline</b> est présent dans le <c>&lt;head&gt;</c> (donc AVANT le rendu du
///     <c>&lt;body&gt;</c> et AVANT <c>blazor.webassembly.js</c>) → aucun flash de thème ;</item>
///   <item>ce script lit la préférence système (<c>prefers-color-scheme</c>) et un éventuel choix
///     persisté (<c>localStorage</c>, prime — Sc.3), puis pose <c>data-theme="clair|sombre"</c> sur
///     <c>document.documentElement</c> ;</item>
///   <item><c>app.css</c> expose un bloc <c>[data-theme=sombre]</c> avec les valeurs de tokens sombres
///     du design doc (slate froid : fond #14161A, carte #1E222A, accent sauge-menthe #5FC9AC —
///     révision hors-sprint sur retour PO, l'ancien brun chaud ayant été écarté).</item>
/// </list>
/// Le rendu réel (fond sombre sans flash, contraste) est constaté au runtime + gate G3.
/// </summary>
public sealed class FondationThemeDefautSystemeZeroFlashTests
{
    private static string RacineWwwroot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "PlanningDeGarde.Web", "wwwroot");
    }

    private static string LireAppCss() => File.ReadAllText(Path.Combine(RacineWwwroot(), "app.css"));

    private static string LireIndexHtml() => File.ReadAllText(Path.Combine(RacineWwwroot(), "index.html"));

    [Fact]
    public void Le_script_d_amorce_du_theme_est_inline_dans_le_head_avant_blazor()
    {
        var html = LireIndexHtml();
        var finHead = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        var posBlazor = html.IndexOf("blazor.webassembly.js", StringComparison.OrdinalIgnoreCase);
        var posScriptTheme = html.IndexOf("data-theme", StringComparison.OrdinalIgnoreCase);

        Assert.True(posScriptTheme >= 0, "un script d'amorce du thème doit exister");
        Assert.InRange(posScriptTheme, 0, finHead); // dans le <head> → avant le rendu du body (zéro flash)
        Assert.True(posScriptTheme < posBlazor, "l'amorce du thème doit précéder le chargement de Blazor");
    }

    [Fact]
    public void L_amorce_lit_la_preference_systeme_le_choix_persiste_et_pose_data_theme()
    {
        var html = LireIndexHtml();
        Assert.Contains("prefers-color-scheme", html, StringComparison.Ordinal);
        Assert.Contains("localStorage", html, StringComparison.Ordinal); // choix explicite prime (Sc.3)
        Assert.Contains("documentElement", html, StringComparison.Ordinal);
        Assert.Contains("sombre", html, StringComparison.Ordinal);
        Assert.Contains("clair", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--pdg-bg", "#14161A")]
    [InlineData("--pdg-card", "#1E222A")]
    [InlineData("--pdg-accent", "#5FC9AC")]
    public void Le_bloc_data_theme_sombre_expose_les_tokens_du_design_doc(string token, string valeur)
    {
        var css = LireAppCss();
        var idxSombre = css.IndexOf("[data-theme=", StringComparison.Ordinal);
        Assert.True(idxSombre >= 0, "un bloc [data-theme=sombre] doit exister");
        var bloc = css[idxSombre..].Replace(" ", string.Empty);
        Assert.Contains(token + ":" + valeur, bloc, StringComparison.OrdinalIgnoreCase);
    }
}
