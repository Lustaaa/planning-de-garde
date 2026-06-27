using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@limite</c>) — un responsable au nom long reste
/// <b>lisible dans la case</b> (troncature visuelle + nom complet accessible au survol via <c>title</c>)
/// et <b>complet dans la légende</b>. La donnée n'est jamais altérée (le read model porte le nom
/// intégral) : la troncature est purement de présentation. Grille réellement câblée, référentiel réel
/// (<c>parent-c → « Marie-Hélène Grand-Dubois »</c>, bleu).
///
/// Driver de présentation : tant que la case rend le nom brut sans <c>title</c> ni classe de troncature,
/// l'observable de lisibilité est absent → rouge ; on ajoute le survol + la troncature CSS.
/// </summary>
public sealed class FrontWasmGrilleNomLongLisibleTests : TestContext
{
    private const string NomComplet = "Marie-Hélène Grand-Dubois";

    [Fact]
    public void Should_Rendre_la_case_lisible_par_troncature_et_survol_du_nom_complet_et_porter_le_nom_complet_dans_la_legende_When_la_grille_reellement_cablee_porte_un_responsable_au_nom_long()
    {
        // Given — l'API distante réelle porte une période affectée au responsable au nom long
        // (parent-c, bleu) le vendredi 03/07/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-c", new DateTime(2026, 7, 3), new DateTime(2026, 7, 3));

        // When — la grille réellement câblée est affichée.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then — la case du 03/07 reste lisible : le nom complet est accessible au survol (title) et la
        // case applique une troncature visuelle (classe dédiée), sans altérer la donnée (texte complet).
        var caseNomLong = GrilleRuntimeHarness.CaseDuJour(grille, "03/07");
        var nom = caseNomLong.QuerySelector("[data-testid='nom-responsable']")!;
        Assert.Equal(NomComplet, nom.GetAttribute("title"));
        Assert.Contains("nom-tronque", nom.ClassList);
        Assert.Equal(NomComplet, nom.TextContent.Trim()); // donnée intègre (la troncature est visuelle/CSS)

        // … et la légende porte le nom COMPLET (non tronqué), en bleu.
        var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal(NomComplet, entree.QuerySelector(".legende-nom")!.TextContent.Trim());
        Assert.Equal("bleu", entree.GetAttribute("data-couleur"));
    }
}
