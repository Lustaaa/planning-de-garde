using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ IHM, <c>@limite</c>) — une fenêtre sans aucune période
/// affectée n'affiche aucun nom et le <b>bloc Légende est absent du rendu</b> (masqué, pas seulement
/// vide). Driver de masquage : tant que le composant Légende rend toujours son conteneur, l'élément
/// <c>[data-testid='legende']</c> est présent → rouge ; le masquage le retire du DOM quand la légende
/// est vide. Grille réellement câblée (store vierge, projection réelle → légende vide).
/// </summary>
public sealed class FrontWasmGrilleFenetreVideLegendeMasqueeTests : TestContext
{
    [Fact]
    public void Should_N_afficher_aucun_nom_et_masquer_completement_le_bloc_legende_When_la_grille_reellement_cablee_n_a_aucune_periode_dans_la_fenetre()
    {
        // Given — l'API distante réelle a un store vierge (aucune période dans la fenêtre).
        using var api = new ApiDistanteFactory();

        // When — la grille réellement câblée est affichée.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — aucune case ne porte de nom de responsable …
        Assert.Empty(grille.FindAll("[data-testid='nom-responsable']"));

        // … et le bloc Légende est absent du rendu (masqué, pas un conteneur vide).
        Assert.Empty(grille.FindAll("[data-testid='legende']"));
    }
}
