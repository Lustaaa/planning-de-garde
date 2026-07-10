using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis l'onglet « Acteurs » de l'écran
/// de configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel, DI réelle, canal HTTP réel), un compte de statut
/// « inactif » offre une action <b>« Activer »</b> (Parent-gated). Le clic transite par le canal
/// d'écriture HTTP réel (POST /api/canal/activer-compte) ; relu depuis le store (GET /api/foyer/comptes),
/// le compte passe « actif » sans rechargement, un accusé non bloquant « Compte activé » s'affiche, et
/// l'action « Activer » disparaît pour ce compte (déjà Actif). Preuve sur câblage réel, jamais une doublure.
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
    public void Should_activer_un_compte_inactif_afficher_l_accuse_et_retirer_l_action_When_le_parent_clique_Activer()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel), identité
        // Parent. On crée d'abord le compte d'Alice (POST /api/canal/creer-compte réel) : il naît « inactif ».
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);

        // Refonte s32 : la gestion du compte se fait dans la MODAL ouverte au crayon d'Alice.
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-email-compte']").Change("alice@foyer.fr"));
        this.SurDispatcher(() => config.Find("[data-testid='bouton-creer-compte']").Click());

        // ... et on attend que le compte inactif soit affiché (ligne relue) avec son action « Activer »
        // dans la modal.
        config.WaitForAssertion(
            () =>
            {
                var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
                Assert.NotNull(compte);
                Assert.Contains("inactif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
                Assert.NotEmpty(config.FindAll("[data-testid='bouton-activer-compte']"));
            },
            TimeSpan.FromSeconds(10));

        // When — je clique « Activer » dans la modal (POST /api/canal/activer-compte réel).
        this.SurDispatcher(() => config.Find("[data-testid='bouton-activer-compte']").Click());

        // Then — sans rechargement, le compte relu depuis le store passe « actif », un accusé non bloquant
        // « Compte activé » s'affiche, et l'action « Activer » disparaît (déjà Actif).
        config.WaitForAssertion(
            () =>
            {
                var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
                Assert.NotNull(compte);
                Assert.Contains("actif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("inactif", compte.TextContent, StringComparison.OrdinalIgnoreCase);
                Assert.Empty(config.FindAll("[data-testid='bouton-activer-compte']"));
                var accuse = config.Find("[data-testid='accuse-activation']");
                Assert.Contains("activé", accuse.TextContent, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));
    }
}
