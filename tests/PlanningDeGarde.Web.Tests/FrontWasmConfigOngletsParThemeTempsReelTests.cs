using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Refonte « Studio » (2026-07) — l'écran de configuration réellement câblé
/// (<see cref="ConfigurationFoyer"/>, API distante réelle) présente une <b>barre latérale d'onglets</b>
/// façon settings. Depuis la relocalisation PO (rework in-goal s40), un onglet « Foyer » est <b>en PREMIER</b>
/// (il héberge la vue graphe lecture seule s38 + badges de complétude s40, auparavant étalée à l'arrivée) et
/// <b>actif par défaut</b> ; suivent « Acteurs », « Rôles », « Cycles »… Tous les panneaux restent rendus dans
/// le DOM (navigation par onglet purement présentationnelle, masquage CSS) : le contenu existant est réparti
/// dans les sections, rien de perdu, rien de dupliqué.
///
/// Rempart anti « vert qui ment » : les contrôles d'onglet doivent exister, « Foyer » doit être le premier ET
/// actif par défaut (« Acteurs » ne l'est PLUS), la vue graphe doit vivre DANS l'onglet Foyer (sinon rouge) ;
/// le sélecteur d'édition + la liste + le cycle doivent tous rester présents. Un bUnit à doublure ne prouverait
/// pas le rendu réellement câblé.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigOngletsParThemeTempsReelTests : TestContext
{
    [Fact]
    public void L_ecran_de_configuration_presente_une_barre_laterale_d_onglets_Foyer_en_premier_et_actif_par_defaut()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé),
        // avec une identité Parent (le contenu d'écriture des sections est visible, gating Sc.7).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran énumère les acteurs DEPUIS LE STORE (GET HTTP réel) : on attend la fin du chargement.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Then — « Foyer » est le PREMIER onglet de la barre et est actif par défaut (relocalisation s40).
        var onglets = config.FindAll(".config-nav .config-nav-item");
        Assert.Equal("onglet-foyer", onglets[0].GetAttribute("data-testid"));
        var ongletFoyer = config.Find("[data-testid='onglet-foyer']");
        Assert.Contains("actif", ongletFoyer.GetAttribute("class"));

        // … « Acteurs » n'est PLUS l'onglet actif par défaut (le graphe ne s'étale plus à l'arrivée).
        var ongletActeurs = config.Find("[data-testid='onglet-acteurs']");
        Assert.DoesNotContain("actif", ongletActeurs.GetAttribute("class"));
        Assert.NotEmpty(config.FindAll("[data-testid='onglet-roles']"));
        Assert.NotEmpty(config.FindAll("[data-testid='onglet-cycles']"));

        // … le panneau Foyer héberge la vue graphe lecture seule s38 (+ badges s40), plus aucun graphe hors onglet.
        var panneauFoyer = config.Find("[data-testid='panneau-foyer']");
        Assert.NotEmpty(panneauFoyer.QuerySelectorAll("[data-testid='graphe-foyer']"));

        // … tous les panneaux sont rendus (navigation par onglet = présentation, masquage CSS).
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-acteurs']"));
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-roles']"));
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-periode-garde']"));

        // … le contenu du panneau Acteurs (refonte s32 : crayon d'édition par ligne + bouton « Ajouter un
        // acteur » + table de lecture) et le cycle de fond sont présents.
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='liste-acteurs']"));
        // Refonte s33 Sc.10 : le cycle est présent via son tableau lecture + crayon « Éditer le cycle ».
        Assert.NotEmpty(config.FindAll("[data-testid='liste-cycles']"));
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-cycle']"));
    }
}
