using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : l'onglet « Slot récurrent » de l'écran
/// de configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle) est
/// <b>RÉSERVÉ</b> — à son ouverture il n'affiche qu'un <b>placeholder « à venir »</b> (structure des
/// onglets proposée par le PO tenue SANS fonctionnalité neuve : le slot récurrent n'existe pas encore) et
/// ne propose <b>aucune affordance d'écriture</b> (aucun formulaire, champ, sélecteur ou bouton de
/// soumission) : aucune écriture ni persistance ne peut être déclenchée depuis cet onglet.
///
/// Rempart : si le panneau réservé exposait par erreur un formulaire (fonctionnalité neuve hors iso-
/// fonctionnel) ou si le placeholder manquait, le test rougirait. Prouvé sur l'écran réellement câblé.
/// </summary>
public sealed class FrontWasmConfigOngletSlotRecurrentReserveTempsReelTests : TestContext
{
    [Fact]
    public void L_onglet_Slot_recurrent_affiche_un_placeholder_a_venir_et_ne_propose_aucune_affordance_d_ecriture()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle, identité Parent (si une
        // écriture DEVAIT exister, elle serait visible pour un Parent : contrôle du « aucune écriture »).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // When — j'ouvre l'onglet « Slot récurrent ».
        config.Find("[data-testid='onglet-slot-recurrent']").Click();

        // Then — l'onglet est actif et les panneaux des autres thèmes ne sont plus rendus (cloisonnement).
        Assert.Equal("true", config.Find("[data-testid='onglet-slot-recurrent']").GetAttribute("aria-selected"));
        Assert.Empty(config.FindAll("[data-testid='panneau-acteurs']"));
        Assert.Empty(config.FindAll("[data-testid='panneau-periode-garde']"));

        // … il affiche un placeholder « à venir » (structure tenue sans fonctionnalité neuve).
        var placeholder = config.Find("[data-testid='placeholder-slot-recurrent']");
        Assert.Contains("À venir", placeholder.TextContent);

        // … et AUCUNE affordance d'écriture n'est proposée depuis cet onglet (aucun formulaire, champ,
        // sélecteur, ni bouton de soumission) : aucune écriture ni persistance ne peut être déclenchée.
        var panneau = config.Find("[data-testid='panneau-slot-recurrent']");
        Assert.Empty(panneau.QuerySelectorAll("form"));
        Assert.Empty(panneau.QuerySelectorAll("input"));
        Assert.Empty(panneau.QuerySelectorAll("select"));
        Assert.Empty(panneau.QuerySelectorAll("button[type='submit']"));
    }
}
