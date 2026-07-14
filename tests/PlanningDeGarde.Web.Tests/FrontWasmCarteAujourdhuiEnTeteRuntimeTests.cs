using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 42 — Sc.4 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la carte « Aujourd'hui : qui récupère ce
/// soir » est rendue EN TÊTE du planning réellement câblé (API distante réelle, projection
/// <c>GrilleAgendaQuery</c> réelle, résolution surcharge&gt;fond&gt;neutre + transfert + slots). Elle
/// restitue le QUI (responsable résolu, couleur de la grille), le OÙ (slot de l'enfant sélectionné) et le
/// TRANSFERT éventuel (bicolore réutilisé). STRICTEMENT en lecture : aucun contrôle d'édition dans la carte.
///
/// Anti « vert qui ment » : la carte est reprojetée depuis la grille RÉELLE lue via HTTP — si le câblage,
/// la résolution ou le rendu manquaient, les observables (nom, slot, transfert) seraient absents → rouge.
/// </summary>
public sealed class FrontWasmCarteAujourdhuiEnTeteRuntimeTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;

    [Fact]
    public void La_carte_du_jour_rend_qui_ou_transfert_en_tete_en_lecture_seule()
    {
        // Given — l'API distante réelle porte, le jour courant (29/06/2026) : une période affectée à
        // parent-a (« Alice », bleu), un slot de Léa à l'école, et un transfert parent-a → parent-b.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Aujourdhui, Aujourdhui);
        GrilleRuntimeHarness.SemerSlot(api, "Léa", "école",
            new DateTime(2026, 6, 29, 8, 30, 0), new DateTime(2026, 6, 29, 16, 30, 0));
        GrilleRuntimeHarness.SemerTransfert(api, "parent-a", "parent-b", Aujourdhui);

        // When — la grille réellement câblée est affichée (le jour courant est dans la fenêtre par défaut).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Then — une carte « Aujourd'hui » est rendue EN TÊTE (avant la grille agenda dans le DOM).
        var carte = grille.Find("[data-testid='carte-aujourdhui']");
        var markup = grille.Markup;
        Assert.True(
            markup.IndexOf("carte-aujourdhui", StringComparison.Ordinal)
                < markup.IndexOf("data-testid=\"grille-agenda\"", StringComparison.Ordinal),
            "la carte doit précéder la grille (en tête)");

        // … le QUI + le TRANSFERT bicolore (cédant Alice → recevant Bruno, couleurs de la grille).
        var transfert = carte.QuerySelector("[data-testid='carte-transfert-bicolore']")!;
        Assert.Equal("Alice", carte.QuerySelector("[data-testid='carte-transfert-cedant']")!.TextContent.Trim());
        Assert.Equal("Bruno", carte.QuerySelector("[data-testid='carte-transfert-recevant']")!.TextContent.Trim());
        Assert.Equal("bleu", transfert.GetAttribute("data-couleur-depart"));
        Assert.Equal("orange", transfert.GetAttribute("data-couleur-arrivee"));

        // … le OÙ : le slot de Léa (l'enfant sélectionné) à l'école.
        var slot = carte.QuerySelector("[data-testid='carte-slot']")!;
        Assert.Contains("école", slot.TextContent);

        // … STRICTEMENT en lecture : aucun contrôle d'édition (bouton) dans la carte.
        Assert.Empty(carte.QuerySelectorAll("button"));
    }
}
