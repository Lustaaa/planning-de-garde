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
    public void L_ecran_de_configuration_empile_ses_sections_sur_une_seule_page_sans_onglets()
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

        // Then — fusion des sections (hors-sprint, retour PO) : PLUS d'onglets ; les sections sont
        // empilées sur une seule page. La barre d'onglets et les onglets ont disparu.
        Assert.Empty(config.FindAll("[data-testid='onglets-config']"));
        Assert.Empty(config.FindAll("[data-testid='onglet-acteurs']"));

        // … les deux sections (Acteurs & rôles, Cycle de fond) sont présentes SIMULTANÉMENT sur la page.
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-acteurs']"));
        Assert.NotEmpty(config.FindAll("[data-testid='panneau-periode-garde']"));

        // … la section « Slot récurrent » (placeholder « à venir ») a été retirée.
        Assert.Empty(config.FindAll("[data-testid='panneau-slot-recurrent']"));

        // … le contenu du thème Acteurs (sélecteur d'édition + liste) est présent.
        Assert.NotEmpty(config.FindAll("[data-testid='selecteur-acteur-edition']"));
        Assert.NotEmpty(config.FindAll("[data-testid='liste-acteurs']"));

        // … et le cycle de fond est désormais visible sur la MÊME page (empilé, plus cloisonné).
        Assert.NotEmpty(config.FindAll("[data-testid='champ-nombre-semaines']"));
    }
}
