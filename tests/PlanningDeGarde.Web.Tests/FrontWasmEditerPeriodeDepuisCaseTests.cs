using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ IHM, <c>@nominal</c> — 5ᵉ usage du menu clic-case). Le
/// comportement neuf vit dans le <c>.razor</c> : cliquer une case ouvre le menu d'actions dont « Éditer une
/// période » ouvre une dialog qui <b>liste les périodes couvrant la date</b> (canal de lecture réel) ;
/// « Éditer » une ligne ouvre un <b>formulaire pré-rempli</b> (bornes + responsable courant) ; réaffecter
/// puis « Enregistrer » transite par le canal d'écriture réel (<c>POST /api/canal/editer-periode</c>), lève
/// un <b>accusé non bloquant « Période modifiée »</b> et fait <b>re-résoudre la case + la légende</b> à la
/// relecture de la grille distante.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Planning.PlanningPartage"/> (front WASM) câblée à
/// une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, projection + référentiel
/// réels). Anti « vert qui ment » : le baseline « Nina la nounou » est asserté avant ; si le formulaire ne
/// se pré-remplit pas, si l'édition ne transite pas jusqu'au store distant, ou si la grille ne se re-résout
/// pas, l'observable reste figé → rouge. Un bUnit à doublure ne verrait ni le câblage distant ni la
/// résolution réelle du foyer.
/// </summary>
public sealed class FrontWasmEditerPeriodeDepuisCaseTests : TestContext
{
    // Mardi 16/06/2026 : la case ciblée (fenêtre démarrant au lundi 15/06, qui couvre le mardi 16/06).
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_reaffecter_la_periode_de_Nounou_a_Parent_A_via_le_formulaire_pre_rempli_puis_relire_la_grille_avec_accuse_When_un_parent_edite_depuis_le_menu_clic_case()
    {
        // Given — la grille réellement câblée à l'API distante, pour un Parent. Une période « Nina la
        // nounou » couvre le mardi 16/06/2026, surfacée dans la case et la légende.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … baseline : la case du 16/06 porte « Nina la nounou » et la légende le fait apparaître.
        Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Contains(grille.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");

        // When — clic sur la case du 16/06 → menu d'actions → « Éditer une période » → la dialog s'ouvre.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-editer-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-editer-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // … la dialog liste, depuis le canal de lecture réel, les périodes couvrant le 16/06 dont celle de
        // « Nina la nounou » (chargement async, attendu SANS re-cliquer la case).
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='periode-editable']"),
                l => l.TextContent.Contains("Nina la nounou")),
            TimeSpan.FromSeconds(10));

        // … on ouvre l'édition de la période de « Nina la nounou » : un formulaire s'ouvre, PRÉ-REMPLI avec
        // le responsable courant (l'identifiant stable « nounou » sélectionné).
        var ligneNounou = grille.FindAll("[data-testid='periode-editable']")
            .Single(l => l.TextContent.Contains("Nina la nounou"));
        this.SurDispatcher(() => ligneNounou.QuerySelector("[data-testid='bouton-editer-periode']")!.Click());

        var champResponsable = grille.WaitForElement("[data-testid='champ-responsable-edition']", TimeSpan.FromSeconds(10));
        Assert.Equal("nounou", ((AngleSharp.Html.Dom.IHtmlSelectElement)champResponsable).Value);

        // … je réaffecte la période à « Parent A » (identifiant stable parent-a) et j'enregistre.
        this.SurDispatcher(() => champResponsable.Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='bouton-enregistrer-edition']").Click());

        // Then — un accusé « Période modifiée » s'affiche à part (non bloquant), la case du 16/06 affiche
        // « Alice » / « bleu » (parent-a résolu par le référentiel réel), et la légende ne fait plus
        // apparaître « Nina la nounou ».
        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("Période modifiée",
                    grille.Find("[data-testid='accuse-periode-modifiee']").TextContent.Trim().TrimEnd('×').Trim());
                var caseMardi = GrilleRuntimeHarness.CaseDuJour(grille, "16/06");
                Assert.Equal("Alice", caseMardi.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                Assert.Equal("bleu", caseMardi.GetAttribute("data-couleur"));
                Assert.DoesNotContain(grille.FindAll("[data-testid='legende-entree']"),
                    e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");
            },
            TimeSpan.FromSeconds(10));

        // … et la période a réellement été ré-affectée dans le store de l'API distante (rempart anti
        // vert-qui-ment), observée via la lecture réelle des périodes couvrant le 16/06.
        using var scope = api.Services.CreateScope();
        var periodesDuJour = scope.ServiceProvider.GetRequiredService<PeriodesDuJourQuery>();
        var relue = Assert.Single(periodesDuJour.Lister(new DateOnly(2026, 6, 16)));
        Assert.Equal("parent-a", relue.ResponsableId);
    }
}
