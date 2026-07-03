using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : gating Invité + échec API à l'activation,
/// depuis l'onglet « Acteurs » de l'écran réellement câblé (<see cref="ConfigurationFoyer"/>, API distante
/// réelle, store réel, DI réelle).
///  - Volet gating : sous identité effective « Invité », l'action « Activer » n'est PAS offerte (gating sur
///    l'identité effective, non-régression s14/s20), même si un compte inactif existe ; contrôle positif
///    sous Parent (l'action REDEVIENT offerte) — preuve que le gating est le discriminant.
///  - Volet échec transport : sous Parent, un clic « Activer » alors que le canal /activer-compte est
///    injoignable (échec de transport déterministe) surface un message d'échec clair, SANS faux positif —
///    le statut affiché reste « inactif » et aucun accusé « Compte activé » n'apparaît.
/// </summary>
public sealed class FrontWasmConfigActiverCompteGatingEtEchecRuntimeTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']").Single(li => NomLigne(li) == nom);

    /// <summary>Sème un compte INACTIF pour Alice (parent-a) dans le store RÉEL de l'API (via le port
    /// d'écriture réel), pour observer l'action « Activer » sans passer par la création UI (gatée Parent).</summary>
    private static void SemerCompteInactifAlice(ApiDistanteFactory api)
        => api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s24", "alice@foyer.fr", StatutCompte.Inactif, "parent-a");

    [Fact]
    public void Un_Invite_ne_voit_pas_l_action_Activer_alors_qu_un_Parent_oui()
    {
        // Given — un compte inactif d'Alice existe dans le store réel ; écran câblé sous « Invité ».
        using var api = new ApiDistanteFactory();
        SemerCompteInactifAlice(api);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        Assert.False(session.EstParent); // garde-fou : un Invité n'écrit pas

        // Then — sous « Invité », aucune action « Activer » n'est offerte (gating sur l'identité effective).
        Assert.Empty(config.FindAll("[data-testid='bouton-activer-compte']"));

        // Contrôle positif (anti faux-vert) — sous « Parent », l'action REDEVIENT offerte pour le compte inactif.
        session.Role = RoleAuteur.Parent;
        this.SurDispatcher(() => config.Find("[data-testid='onglet-periode-garde']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='onglet-acteurs']").Click());
        config.WaitForAssertion(
            () => Assert.NotNull(LigneDe(config, "Alice").QuerySelector("[data-testid='bouton-activer-compte']")),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_Afficher_un_message_d_echec_clair_sans_faux_positif_When_un_Parent_clique_Activer_alors_que_l_API_est_injoignable()
    {
        // Given — un compte inactif d'Alice dans le store réel ; écran câblé sous Parent, MAIS le canal
        // /activer-compte subit un échec de transport déterministe (les lectures transitent normalement).
        using var api = new ApiDistanteFactory();
        SemerCompteInactifAlice(api);
        Services.AddSingleton(
            GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "activer-compte"));
        Services.AddSingleton(new SessionPlanning()); // Parent par défaut

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForAssertion(
            () => Assert.NotNull(LigneDe(config, "Alice").QuerySelector("[data-testid='bouton-activer-compte']")),
            TimeSpan.FromSeconds(10));

        // When — je clique « Activer » alors que le canal d'activation est injoignable.
        this.SurDispatcher(() => LigneDe(config, "Alice").QuerySelector("[data-testid='bouton-activer-compte']")!.Click());

        // Then — un message d'échec clair s'affiche dans la ligne, le statut affiché reste « inactif »
        // (aucun faux positif), et aucun accusé « Compte activé » n'apparaît.
        var motif = config.WaitForElement("[data-testid='motif-echec-activation']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageInjoignable, motif.TextContent.Trim());
        var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
        Assert.Contains("inactif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(config.FindAll("[data-testid='accuse-activation']"));

        // Observable cardinal — le store réel n'a pas activé le compte (aucune écriture n'a transité).
        var compteStore = api.Services.GetRequiredService<IEnumerationComptes>()
            .EnumererComptes().Single(c => c.ActeurId == "parent-a");
        Assert.Equal(StatutCompte.Inactif, compteStore.Statut);
    }
}
