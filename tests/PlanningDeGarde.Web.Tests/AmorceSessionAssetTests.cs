using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.1 (🖥️ @ihm, garde d'asset) : le mécanisme durable qui fait <b>survivre la session au F5</b>
/// est réellement embarqué dans <c>index.html</c>. Le module <c>window.pdgSession</c> (adaptateur JS du port
/// <see cref="PlanningDeGarde.Web.IPersistanceSession"/>) persiste/relit un jeton de session dans
/// <c>localStorage['pdg-session']</c> — c'est ce stockage, hors mémoire du runtime WASM, qui survit au
/// rechargement. Le test de restauration au runtime double le port (pas de vrai navigateur en test) ; cette
/// garde d'asset couvre l'implémentation réelle du stockage (comme la garde du module <c>pdgTheme</c>, s26).
/// </summary>
public sealed class AmorceSessionAssetTests
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

    private static string LireIndexHtmlCompact()
        => File.ReadAllText(Path.Combine(RacineWeb(), "wwwroot", "index.html"))
            .Replace(" ", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);

    [Fact]
    public void Le_module_pdgSession_persiste_et_relit_localStorage()
    {
        var html = LireIndexHtmlCompact();
        Assert.Contains("pdgSession", html, StringComparison.Ordinal);
        // persister : écrit le jeton dans le stockage durable (survit au F5).
        Assert.Contains("localStorage.setItem('pdg-session'", html, StringComparison.Ordinal);
        // lire : relit le jeton au démarrage.
        Assert.Contains("localStorage.getItem('pdg-session')", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Le_module_pdgSession_purge_localStorage_au_logout()
    {
        // purger : le logout (Sc.3) efface le jeton persisté du stockage durable → un F5 ultérieur ne
        // restaure plus aucune session (le logout reste effectif au rechargement).
        var html = LireIndexHtmlCompact();
        Assert.Contains("localStorage.removeItem('pdg-session')", html, StringComparison.Ordinal);
    }
}
