using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du sprint 29 — S13 (🖥️ scénario IHM, gate G3) : rendu du transfert
/// BICOLORE sur la grille réellement câblée (front WASM <see cref="Web.Components.Pages.PlanningPartage"/>
/// + API distante réelle, projection réelle). Un jour portant un transfert affiche une case coupée par une
/// DIAGONALE séparant la couleur de départ (déposant) de la couleur d'arrivée (récupérant), le nom du
/// responsable et la légende restant lisibles ; la légende signale le motif bicolore = transfert.
///
/// Anti « vert qui ment » : les couleurs départ/arrivée proviennent du référentiel RÉEL des acteurs résolu
/// côté API et lu via HTTP ; si le rendu bicolore n'est pas câblé, l'observable (élément bicolore + ses
/// couleurs) est absent → rouge.
/// </summary>
public sealed class FrontWasmTransfertBicoloreTests : TestContext
{
    // Lundi 29/06/2026 : ancre de référence. Le transfert (Alice→Bruno) est posé ce jour-là.
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    [Fact]
    public void Should_Rendre_la_case_du_transfert_en_diagonale_bicolore_depart_bleu_arrivee_orange_avec_nom_et_legende_lisibles_When_un_transfert_couvre_le_jour()
    {
        // Given — l'API distante réelle porte, le lundi 29/06/2026 : une période affectant Alice (parent-a,
        // bleu) ET un transfert déposé par Alice (parent-a, bleu) et récupéré par Bruno (parent-b, orange).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 6, 29), new DateTime(2026, 6, 29));
        GrilleRuntimeHarness.SemerTransfert(api, "parent-a", "parent-b", new DateTime(2026, 6, 29));

        // When — la grille réellement câblée est affichée à la date de référence 29/06/2026.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // Then — la case du 29/06 porte une diagonale bicolore : départ bleu (Alice), arrivée orange (Bruno).
        var caseLundi = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        var bicolore = caseLundi.QuerySelector("[data-testid='case-transfert-bicolore']");
        Assert.NotNull(bicolore);
        Assert.Equal("bleu", bicolore!.GetAttribute("data-couleur-depart"));
        Assert.Equal("orange", bicolore!.GetAttribute("data-couleur-arrivee"));

        // … le nom du responsable reste lisible dans la case (R18–R22).
        Assert.Equal("Alice", caseLundi.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // … et la légende signale le motif bicolore = transfert.
        var motifs = grille.FindAll("[data-testid='legende-motif']");
        Assert.Contains(motifs, m => m.TextContent.Contains("Transfert", StringComparison.OrdinalIgnoreCase));
    }
}
