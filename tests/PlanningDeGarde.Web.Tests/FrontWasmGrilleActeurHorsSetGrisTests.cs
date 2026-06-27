using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.5 (🖥️ IHM, <c>@limite</c>) — un acteur hors set couleur
/// (« grand-père », identifiant stable valide mais absent de la palette) <b>conserve son nom</b> dans
/// la case et la légende, sur une teinte <b>neutre (gris)</b>. Le nom se résout via le référentiel réel
/// même quand la couleur retombe en neutre : nom et couleur sont indépendants (règle 17). Gris
/// <b>assumé</b> (acteur non encore colorié), pas un défaut de résolution.
///
/// Caractérisation runtime (early-green attendu) : le référentiel et la palette réels du foyer portent
/// déjà « grand-père » et son repli neutre ; on verrouille que cela surface fidèlement sur la grille
/// réellement câblée.
/// </summary>
public sealed class FrontWasmGrilleActeurHorsSetGrisTests : TestContext
{
    [Fact]
    public void Should_Afficher_le_nom_grand_pere_sur_teinte_grise_neutre_dans_la_case_et_la_legende_When_la_grille_reellement_cablee_porte_un_acteur_hors_set_couleur()
    {
        // Given — l'API distante réelle porte une période affectée à « grand-père » (hors set couleur)
        // le samedi 04/07/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "grand-pere", new DateTime(2026, 7, 4), new DateTime(2026, 7, 4));

        // When — la grille réellement câblée est affichée.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — la case du 04/07 affiche « grand-père » sur fond gris neutre (nom conservé, couleur effondrée).
        var caseGrandPere = GrilleRuntimeHarness.CaseDuJour(grille, "04/07");
        Assert.Equal("grand-père", caseGrandPere.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("gris", caseGrandPere.GetAttribute("data-couleur"));

        // … et la légende contient une entrée grand-père (gris).
        var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Contains("grand-père", entree.TextContent);
        Assert.Equal("gris", entree.GetAttribute("data-couleur"));
    }
}
