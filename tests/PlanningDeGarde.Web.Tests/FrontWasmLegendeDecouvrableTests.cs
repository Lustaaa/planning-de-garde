using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.12 (🖥️ @ihm, légende) — la légende est découvrable et cohérente avec les couleurs de
/// responsabilité, en clair ET en sombre. Garde d'asset sur le composant réellement livré :
/// <list type="bullet">
///   <item>le style est tokenisé (nom en <c>--pdg-ink</c>, plus de couleurs codées en dur qui cassaient
///     le thème sombre) ;</item>
///   <item>chaque entrée est présentée en « chip » repérable (pastille de couleur + nom).</item>
/// </list>
/// La pastille conserve sa couleur de responsabilité INLINE (<c>CouleursTheme.Pleine</c>), et les
/// data-testid (<c>legende</c>, <c>legende-entree</c>) restent intacts (Sc.14 le prouve).
/// </summary>
public sealed class FrontWasmLegendeDecouvrableTests
{
    private static string LireLegende()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "PlanningDeGarde.Web", "Components", "Shared", "Legende.razor"));
    }

    [Fact]
    public void Le_nom_de_legende_est_tokenise_sans_couleur_codee_en_dur()
    {
        var razor = LireLegende();
        Assert.Contains("var(--pdg-ink)", razor, StringComparison.Ordinal);
        Assert.DoesNotContain("#333", razor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#555", razor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#dee2e6", razor, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Les_entrees_sont_presentees_en_chips_reperables()
    {
        var razor = LireLegende();
        // Chip repérable = pastille encadrée dans une puce arrondie tokenisée.
        Assert.Contains("border-radius: 999px", razor, StringComparison.Ordinal);
        Assert.Contains("var(--pdg-border)", razor, StringComparison.Ordinal);
    }
}
