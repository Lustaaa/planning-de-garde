using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du sprint 29 — S14 (🖥️ scénario IHM, non-régression visuelle) : un jour
/// SANS transfert reste unicolore, inchangé (aucune diagonale, aucune entrée de légende de motif). Garde de
/// non-régression du rendu bicolore (S13) : la présentation antérieure des cases sans transfert n'est pas
/// altérée. Rendu sur la grille réellement câblée (front WASM + API distante réelle, projection réelle).
/// </summary>
public sealed class FrontWasmCaseSansTransfertUnicoloreTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    [Fact]
    public void Should_Rendre_les_cases_sans_transfert_en_unicolore_sans_diagonale_ni_legende_motif_When_aucun_transfert_ne_couvre_la_fenetre()
    {
        // Given — l'API distante réelle porte une période affectant Alice le 29/06, mais AUCUN transfert.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 6, 29), new DateTime(2026, 6, 29));

        // When — la grille réellement câblée est affichée.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // Then — aucune case ne porte de rendu bicolore (unicolore inchangé sur toute la fenêtre).
        Assert.Empty(grille.FindAll("[data-testid='case-transfert-bicolore']"));

        // … la case du 29/06 reste unicolore : nom du responsable + teinte de responsabilité inchangés.
        var caseLundi = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        Assert.Equal("Alice", caseLundi.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseLundi.GetAttribute("data-couleur"));

        // … et la légende ne signale aucun motif bicolore (absence de transfert dans la fenêtre).
        Assert.Empty(grille.FindAll("[data-testid='legende-motif']"));
    }
}
