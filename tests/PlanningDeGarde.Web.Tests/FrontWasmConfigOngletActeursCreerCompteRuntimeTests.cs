using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 22 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis l'onglet « Acteurs » de l'écran de
/// configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel, DI réelle, hub SignalR réel), on <b>crée / associe un
/// compte à un acteur</b> (email obligatoire) via le canal d'écriture HTTP réel (POST
/// /api/foyer/comptes), relu depuis le store (GET /api/foyer/comptes). Le compte apparaît associé à
/// l'acteur avec son statut « inactif » affiché, sans rechargement. Un email vide (ou déjà utilisé) est
/// refusé par le handler réel : le formulaire reste ouvert avec un motif clair, aucun compte créé.
/// </summary>
public sealed class FrontWasmConfigOngletActeursCreerCompteRuntimeTests : TestContext
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
    public void Should_creer_un_compte_associe_a_un_acteur_avec_statut_inactif_affiche()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel), identité
        // Parent. Alice (parent-a) est un acteur déclaré du seed, sans compte.
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);

        // Then (avant création) — la ligne d'Alice n'affiche aucun compte associé.
        Assert.Empty(LigneDe(config, "Alice").QuerySelectorAll("[data-testid='compte-acteur']"));

        // When — refonte s32 : la création de compte se fait dans la MODAL ouverte au crayon d'Alice.
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-email-compte']").Change("alice@foyer.fr"));
        this.SurDispatcher(() => config.Find("[data-testid='bouton-creer-compte']").Click());

        // Then — sans rechargement, la ligne d'Alice relue depuis le store affiche son compte associé,
        // avec l'email et le statut « inactif ».
        config.WaitForAssertion(
            () =>
            {
                var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
                Assert.NotNull(compte);
                Assert.Contains("alice@foyer.fr", compte!.TextContent);
                Assert.Contains("inactif", compte.TextContent, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_garder_le_formulaire_ouvert_avec_un_motif_clair_et_ne_creer_aucun_compte_When_l_email_est_vide()
    {
        // Given — écran câblé réel, identité Parent, Alice sans compte.
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);

        // When — refonte s32 : dans la MODAL d'Alice, je soumets un email VIDE (le handler réel refuse
        // « email requis »).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-email-compte']").Change(""));
        this.SurDispatcher(() => config.Find("[data-testid='bouton-creer-compte']").Click());

        // Then — un motif clair est surfacé DANS la modal, le champ email reste présent (formulaire ouvert),
        // et AUCUN compte n'est associé à Alice (le store n'a rien écrit, la ligne relue reste sans compte).
        config.WaitForAssertion(
            () =>
            {
                var motif = config.Find("[data-testid='motif-echec-compte']");
                Assert.False(string.IsNullOrWhiteSpace(motif.TextContent));
                Assert.NotEmpty(config.FindAll("[data-testid='champ-email-compte']"));
                Assert.Empty(LigneDe(config, "Alice").QuerySelectorAll("[data-testid='compte-acteur']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
