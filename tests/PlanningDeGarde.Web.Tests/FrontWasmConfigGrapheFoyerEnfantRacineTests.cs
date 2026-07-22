extern alias api;
using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Bunit;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 38 — Sc.3 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE, store réel,
/// endpoint /api/foyer/graphe réel — jamais une doublure de transport). À l'ARRIVÉE sur /configuration, une
/// vue LECTURE SEULE affiche le foyer comme un GRAPHE avec chaque ENFANT en RACINE ; sous chaque enfant, ses
/// parents liés en branches « nom (rôle-du-lien) » (« Alice (mère) »). La vue est STRICTEMENT en lecture :
/// aucun contrôle d'édition dans la section graphe. Store SANS enfant (Mongo 1er lancement) → MESSAGE NEUTRE
/// (« Aucun enfant, ajoutez-en. »), zéro nœud fantôme.
/// </summary>
public sealed class FrontWasmConfigGrapheFoyerEnfantRacineTests : TestContext
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
    public void A_l_arrivee_chaque_enfant_est_une_racine_avec_ses_parents_en_branches_nom_et_role_du_lien()
    {
        // Given — « Léa » liée à Alice (parent-a) avec le rôle « mère » dans le store réel (état représentatif).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a", RoleDuLien.Mere);

        // When — j'arrive sur la Config foyer (la vue graphe est rendue à l'arrivée).
        var config = RendreConfig(api);

        // Then — Léa en RACINE, avec Alice en branche « Alice (mère) ».
        config.WaitForAssertion(
            () =>
            {
                var lea = config.FindAll("[data-testid='graphe-enfant-racine']")
                    .Single(r => r.GetAttribute("data-enfant-id") == "Léa");
                Assert.Equal("Léa", lea.QuerySelector("[data-testid='graphe-enfant-nom']")!.TextContent.Trim());
                var branche = lea.QuerySelectorAll("[data-testid='graphe-parent-branche']")
                    .Single(b => b.GetAttribute("data-acteur-id") == "parent-a");
                Assert.Equal("Alice (mère)", branche.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // Et la vue graphe est STRICTEMENT en lecture : aucun contrôle d'édition dans la section.
        var section = config.Find("[data-testid='graphe-foyer']");
        Assert.Empty(section.QuerySelectorAll("button, input, select, textarea, a"));
        Assert.Empty(section.QuerySelectorAll("[data-testid='crayon-enfant']"));
    }

    [Fact]
    public void Store_sans_enfant_affiche_un_message_neutre_zero_noeud_fantome()
    {
        // Given — un foyer SANS aucun enfant (Mongo 1er lancement) : store d'enfants vide sur l'hôte RÉEL.
        using var api = new ApiSansEnfantsFactory();
        var config = RendreConfig(api);

        // Then — message neutre affiché, aucun nœud (racine) fantôme.
        var section = config.Find("[data-testid='graphe-foyer']");
        Assert.Contains("Aucun enfant", section.QuerySelector("[data-testid='graphe-vide']")!.TextContent);
        Assert.Empty(config.FindAll("[data-testid='graphe-enfant-racine']"));
    }

    /// <summary>Hôte d'API RÉEL (env « Testing »), mais dont le store d'enfants est <b>vide</b> — profil de
    /// données « Mongo 1er lancement » (asymétrie seed s15). Seule la donnée diffère : endpoint /api/foyer/graphe,
    /// DI, razor et canal restent réels. On remplace le port de lecture des enfants par un store vide.</summary>
    private sealed class ApiSansEnfantsFactory : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<api::ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing"); // store réel, câblage inchangé — seule la donnée enfants diffère
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IEnumerationEnfants>(new EnfantsVides()));
        }

        private sealed class EnfantsVides : IEnumerationEnfants
        {
            public System.Collections.Generic.IReadOnlyCollection<PlanningDeGarde.Application.Enfants.Ports.EnfantFoyer> EnumererEnfants()
                => System.Array.Empty<PlanningDeGarde.Application.Enfants.Ports.EnfantFoyer>();
        }
    }
}
