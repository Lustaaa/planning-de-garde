using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ IHM, <c>@nominal</c>) — une période affectée à Alice
/// affiche son <b>nom</b> dans la case (sur sa teinte de responsabilité) <b>et</b> entre dans la
/// <b>légende</b>. La grille réelle <see cref="PlanningDeGarde.Web.Components.Planning.PlanningPartage"/>
/// est rendue câblée à une API distante réelle (référentiel réel <c>parent-a → « Alice »</c>, palette
/// réelle <c>parent-a → bleu</c>) : le nom rendu provient du référentiel réel, lu via HTTP.
///
/// Anti « vert qui ment » : si la case ne rend pas le nom ou si le composant Légende n'existe pas, les
/// observables (texte « Alice » dans la case, entrée de légende) sont absents → rouge. Un bUnit à
/// doublure de transport ne prouverait ni le câblage HTTP ni la résolution réelle du référentiel.
/// </summary>
public sealed class FrontWasmGrilleNomEtLegendeTests : TestContext
{
    [Fact]
    public void Should_Afficher_le_nom_Alice_dans_la_case_du_lundi_29_06_2026_sur_sa_teinte_de_responsabilite_et_une_entree_de_legende_Alice_When_la_grille_reellement_cablee_est_affichee_avec_une_periode_affectee_a_Alice()
    {
        // Given — l'API distante réelle (store vierge, palette + référentiel réels du foyer) porte une
        // période de garde affectée à parent-a (« Alice », bleu) le lundi 29/06/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a",
            new DateTime(2026, 6, 29), new DateTime(2026, 6, 29));

        // When — la grille réellement câblée est affichée à la date de référence 29/06/2026.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — la case du lundi 29/06/2026 affiche « Alice » sur sa teinte de responsabilité bleue.
        var caseLundi = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        Assert.Equal("Alice", caseLundi.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseLundi.GetAttribute("data-couleur"));

        // … et la légende contient exactement une entrée Alice (bleu).
        var entrees = grille.FindAll("[data-testid='legende-entree']");
        var entree = Assert.Single(entrees);
        Assert.Contains("Alice", entree.TextContent);
        Assert.Equal("bleu", entree.GetAttribute("data-couleur"));
    }
}
