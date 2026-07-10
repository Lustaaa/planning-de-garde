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
/// façon settings — « Acteurs », « Rôles », « Cycles » — avec l'onglet « Acteurs » <b>actif par
/// défaut</b>. Les trois panneaux (Acteurs, Rôles, Cycle de fond) restent rendus dans le DOM (navigation
/// par onglet purement présentationnelle, masquage CSS) : le contenu existant est réparti dans les trois
/// sections, rien de perdu, rien de dupliqué.
///
/// Rempart anti « vert qui ment » : les contrôles d'onglet doivent exister et « Acteurs » doit être actif
/// par défaut (sinon rouge) ; le sélecteur d'édition + la liste + le cycle doivent tous être présents.
/// Un bUnit à doublure ne prouverait pas le rendu réellement câblé.
/// </summary>
public sealed class FrontWasmConfigOngletsParThemeTempsReelTests : TestContext
{
    [Fact]
    public void L_ecran_de_configuration_presente_une_barre_laterale_d_onglets_Acteurs_actif_par_defaut()
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

        // Then — la barre latérale expose les trois onglets, « Acteurs » actif par défaut.
        var ongletActeurs = config.Find("[data-testid='onglet-acteurs']");
        Assert.Contains("actif", ongletActeurs.GetAttribute("class"));
        Assert.NotEmpty(config.FindAll("[data-testid='onglet-roles']"));
        Assert.NotEmpty(config.FindAll("[data-testid='onglet-cycles']"));

        // … les trois panneaux sont rendus (navigation par onglet = présentation, masquage CSS).
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-acteurs']"));
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-roles']"));
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-periode-garde']"));

        // … le contenu du panneau Acteurs (refonte s32 : crayon d'édition par ligne + bouton « Ajouter un
        // acteur » + table de lecture) et le cycle de fond sont présents.
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='liste-acteurs']"));
        Assert.NotEmpty(config.FindAll("[data-testid='champ-nombre-semaines']"));
    }
}
