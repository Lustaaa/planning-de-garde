using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ IHM, <c>@nominal</c>) — deux responsables distincts
/// donnent deux entrées de légende, Alice n'apparaissant qu'une fois malgré ses deux créneaux. La
/// dédoublonnance est garantie côté projection (read model) ; on prouve ici qu'elle <b>surface</b>
/// correctement sur la grille réellement câblée (cases nommées + légende dédoublonnée), palette réelle
/// (<c>parent-a → bleu</c>, <c>parent-b → orange</c> — les couleurs Gherkin sont illustratives).
/// </summary>
public sealed class FrontWasmGrilleLegendeDedoublonneeTests : TestContext
{
    [Fact]
    public void Should_Afficher_Alice_sur_ses_deux_jours_et_Bruno_sur_le_sien_avec_une_legende_de_deux_entrees_Alice_une_seule_fois_When_la_grille_reellement_cablee_porte_deux_responsables()
    {
        // Given — Alice (parent-a) les 29/06 et 01/07, Bruno (parent-b) le 30/06, dans le store réel.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 6, 29), new DateTime(2026, 6, 29));
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 1), new DateTime(2026, 7, 1));
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 6, 30), new DateTime(2026, 6, 30));

        // When — la grille réellement câblée est affichée.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — les deux jours d'Alice affichent « Alice » sur bleu …
        foreach (var jour in new[] { "29/06", "01/07" })
        {
            var caseAlice = GrilleRuntimeHarness.CaseDuJour(grille, jour);
            Assert.Equal("Alice", caseAlice.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            Assert.Equal("bleu", caseAlice.GetAttribute("data-couleur"));
        }

        // … et le jour de Bruno affiche « Bruno » sur sa couleur réelle (orange).
        var caseBruno = GrilleRuntimeHarness.CaseDuJour(grille, "30/06");
        Assert.Equal("Bruno", caseBruno.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", caseBruno.GetAttribute("data-couleur"));

        // … et la légende contient exactement deux entrées : Alice (une seule fois) et Bruno.
        var entrees = grille.FindAll("[data-testid='legende-entree']");
        Assert.Equal(2, entrees.Count);
        var noms = entrees.Select(e => e.QuerySelector(".legende-nom")!.TextContent.Trim()).ToList();
        Assert.Equal(1, noms.Count(n => n == "Alice"));
        Assert.Contains("Bruno", noms);
    }
}
