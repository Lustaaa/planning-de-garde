extern alias api;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Bunit;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 40 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE, store réel,
/// endpoint /api/foyer/graphe réel — jamais une doublure de transport). À l'ARRIVÉE sur /configuration, la
/// vue graphe s38 affiche PAR enfant un BADGE de complétude du couple R3 (s40) : « couple complet » (père +
/// mère), « couple incomplet » (0/1 parent, ou 2 sans le couple père+mère), « aucun parent » (racine isolée,
/// état neutre distinct). Le badge est STRICTEMENT en lecture : aucun contrôle d'édition, aucune commande
/// émise depuis le graphe. Profil de données représentatif (enfants à complétude variée, usage réel).
/// </summary>
public sealed class FrontWasmConfigBadgeCompletudeCoupleTests : TestContext
{
    private IRenderedComponent<ConfigurationFoyer> RendreConfig(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<api::ApiProgram> api)
    {
        var client = new System.Net.Http.HttpClient(api.Server.CreateHandler()) { BaseAddress = api.Server.BaseAddress };
        Services.AddSingleton(client);
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='graphe-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void A_l_arrivee_chaque_enfant_porte_un_badge_de_completude_selon_son_couple()
    {
        // Given — profil représentatif : Léa (couple père+mère → complet), Tom (un seul parent → incomplet),
        // Zoé (aucun parent → vide). Les acteurs parent-a / parent-b sont des parents liables (seed réel).
        using var api = new ApiDistanteFactory();
        var enfants = api.Services.GetRequiredService<IEditeurEnfants>();
        enfants.LierParent("Léa", "parent-a", RoleDuLien.Pere);
        enfants.LierParent("Léa", "parent-b", RoleDuLien.Mere);
        enfants.Ajouter("enfant-tom", "Tom");
        enfants.LierParent("enfant-tom", "parent-a", RoleDuLien.Pere);
        enfants.Ajouter("enfant-zoe", "Zoé");

        // When — j'arrive sur la Config foyer (la vue graphe et ses badges sont rendus à l'arrivée).
        var config = RendreConfig(api);

        // Then — un badge par enfant, libellé selon son statut R3.
        config.WaitForAssertion(
            () =>
            {
                Assert.Equal("couple complet", BadgeDe(config, "Léa").TextContent.Trim());
                Assert.Equal("couple incomplet", BadgeDe(config, "enfant-tom").TextContent.Trim());
                Assert.Equal("aucun parent", BadgeDe(config, "enfant-zoe").TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // Et la section graphe reste STRICTEMENT en lecture : aucun contrôle d'édition, aucune commande émise.
        var section = config.Find("[data-testid='graphe-foyer']");
        Assert.Empty(section.QuerySelectorAll("button, input, select, textarea, a"));
    }

    private static AngleSharp.Dom.IElement BadgeDe(IRenderedComponent<ConfigurationFoyer> config, string enfantId)
    {
        var racine = config.FindAll("[data-testid='graphe-enfant-racine']")
            .Single(r => r.GetAttribute("data-enfant-id") == enfantId);
        return racine.QuerySelector("[data-testid='graphe-enfant-badge']")!;
    }
}
