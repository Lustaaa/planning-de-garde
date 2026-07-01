using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.2 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : l'écran de configuration réellement
/// câblé (<see cref="ConfigurationFoyer"/>, API distante réelle) présente <b>trois onglets par thème</b>
/// — « Acteurs », « Période de garde », « Slot récurrent » — avec l'onglet « Acteurs » <b>actif par
/// défaut</b>. Le contenu existant est <b>réparti</b> dans ces onglets (rien de perdu, rien de
/// dupliqué) : à l'ouverture, seul le panneau « Acteurs » est affiché (sélecteur d'édition + liste des
/// acteurs), tandis que le contenu d'un autre thème (le cycle de fond) n'est PAS visible sous l'onglet
/// actif — preuve que le contenu est bien cloisonné par onglet et non tout empilé sur un seul écran.
///
/// Rempart anti « vert qui ment » : tant que l'écran empile tout son contenu sans onglets, les
/// contrôles d'onglet manquent (rouge) ; si le cycle restait affiché sous l'onglet Acteurs, la
/// répartition serait factice (rouge). Un bUnit à doublure ne prouverait pas le rendu réellement câblé.
/// </summary>
public sealed class FrontWasmConfigOngletsParThemeTempsReelTests : TestContext
{
    [Fact]
    public void L_ecran_de_configuration_presente_trois_onglets_par_theme_avec_l_onglet_Acteurs_actif_par_defaut_et_le_contenu_reparti()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé),
        // avec une identité Parent (le contenu d'écriture des onglets est visible, gating Sc.7).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran énumère les acteurs DEPUIS LE STORE (GET HTTP réel) : on attend la fin du chargement.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Then — trois onglets par thème, avec les libellés attendus.
        Assert.Equal("Acteurs",
            config.Find("[data-testid='onglet-acteurs']").TextContent.Trim());
        Assert.Equal("Période de garde",
            config.Find("[data-testid='onglet-periode-garde']").TextContent.Trim());
        Assert.Equal("Slot récurrent",
            config.Find("[data-testid='onglet-slot-recurrent']").TextContent.Trim());

        // … l'onglet « Acteurs » est actif par défaut ; les deux autres ne le sont pas.
        Assert.Equal("true", config.Find("[data-testid='onglet-acteurs']").GetAttribute("aria-selected"));
        Assert.Equal("false", config.Find("[data-testid='onglet-periode-garde']").GetAttribute("aria-selected"));
        Assert.Equal("false", config.Find("[data-testid='onglet-slot-recurrent']").GetAttribute("aria-selected"));

        // … le contenu du thème Acteurs (sélecteur d'édition + liste des acteurs) est présent sous l'onglet actif.
        Assert.NotEmpty(config.FindAll("[data-testid='selecteur-acteur-edition']"));
        Assert.NotEmpty(config.FindAll("[data-testid='liste-acteurs']"));

        // … et le contenu d'un AUTRE thème (le cycle de fond) n'est PAS empilé sous l'onglet Acteurs :
        // preuve de répartition cloisonnée par onglet (rien de dupliqué sur un écran unique).
        Assert.Empty(config.FindAll("[data-testid='champ-nombre-semaines']"));
    }
}
