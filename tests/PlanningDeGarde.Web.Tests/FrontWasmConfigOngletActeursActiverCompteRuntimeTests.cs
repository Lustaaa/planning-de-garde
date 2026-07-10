using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.5, MIGRÉ s33 Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la modal d'un
/// acteur de l'écran réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel, DI réelle, canal HTTP réel), un compte de statut
/// « inactif » offre désormais un <b>TOGGLE « actif »</b> (plus un bouton d'action immédiat — swap de
/// surface s33 Sc.4, sens ON). Basculer le toggle OFF→ON puis « Enregistrer » transite par le canal
/// d'écriture HTTP réel (POST /api/canal/activer-compte) ; relu depuis le store (GET /api/foyer/comptes),
/// le compte passe « actif » sans rechargement, la modal se ferme, un accusé « Compte activé » s'affiche.
/// Preuve sur câblage réel, jamais une doublure.
/// </summary>
public sealed class FrontWasmConfigOngletActeursActiverCompteRuntimeTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']").Single(li => NomLigne(li) == nom);

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Should_activer_un_compte_inactif_afficher_l_accuse_et_fermer_la_modal_When_le_parent_bascule_le_toggle_actif_et_enregistre()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel), identité
        // Parent. On crée d'abord le compte d'Alice (POST /api/canal/creer-compte réel) : il naît « inactif ».
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);

        // Refonte s32 : la gestion du compte se fait dans la MODAL ouverte au crayon d'Alice.
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-email-compte']").Change("alice@foyer.fr"));
        this.SurDispatcher(() => config.Find("[data-testid='bouton-creer-compte']").Click());

        // ... et on attend que le compte inactif soit affiché (ligne relue) et que le TOGGLE « actif » de la
        // modal soit désormais ACTIONNABLE (compte inactif → non verrouillé). Swap de surface s33 Sc.4.
        config.WaitForAssertion(
            () =>
            {
                var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
                Assert.NotNull(compte);
                Assert.Contains("inactif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
                var toggle = config.Find("[data-testid='toggle-actif']");
                Assert.False(toggle.HasAttribute("disabled")); // compte inactif → toggle actionnable
            },
            TimeSpan.FromSeconds(10));

        // When — je bascule le toggle « actif » OFF→ON puis j'enregistre (POST /api/canal/activer-compte réel).
        this.SurDispatcher(() => config.Find("[data-testid='toggle-actif']").Change(true));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — sans rechargement, la modal se ferme, le compte relu depuis le store passe « actif » (dans la
        // table), et un accusé non bloquant « Compte activé » s'affiche.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
                Assert.NotNull(compte);
                Assert.Contains("actif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("inactif", compte.TextContent, StringComparison.OrdinalIgnoreCase);
                var accuse = config.Find("[data-testid='accuse-activation']");
                Assert.Contains("activé", accuse.TextContent, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));
    }
}
