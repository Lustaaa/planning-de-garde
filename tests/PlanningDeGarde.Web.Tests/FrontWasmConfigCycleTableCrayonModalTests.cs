using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.10 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : l'onglet « Cycle » rend VISIBLES, dans un
/// TABLEAU en lecture seule, toutes les affectations déclarées du cycle de fond (query Sc.3, une ligne par
/// semaine {index → responsable}) — y compris celles auparavant invisibles (retour PO gate s32). Une colonne
/// Actions porte UN crayon « Éditer le cycle » ; l'éditeur inline préexistant (#form-cycle : N + selects par
/// semaine) n'est PLUS rendu hors modal. Le crayon ouvre une MODAL hébergeant cet éditeur EXISTANT tel quel,
/// pré-rempli sur le cycle courant ; « Définir le cycle » (commande existante, atomique N + toutes les
/// semaines) persiste, la modal se ferme, le tableau est relu. Écran réellement câblé, jamais une doublure.
/// </summary>
public sealed class FrontWasmConfigCycleTableCrayonModalTests : TestContext
{
    private static string? ResponsableLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".cycle-responsable")?.TextContent.Trim();

    private static string? LibelleLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneIndex(IRenderedComponent<ConfigurationFoyer> config, int index)
        => config.FindAll("[data-testid='cycle-foyer']").Single(li => li.GetAttribute("data-cycle-index") == index.ToString());

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api, SessionPlanning? session = null)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session ?? new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => api.Services.GetRequiredService<IReferentielCycleDeFond>()
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }), GrilleRuntimeHarness.EnfantParDefaut);

    [Fact]
    public void Le_tableau_rend_visibles_les_affectations_declarees_avec_un_crayon_editer_l_inline_est_retire()
    {
        // Given — un cycle de fond N=2 est déclaré (parent-a semaine 0, parent-b semaine 1) ; écran Parent.
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var config = RendreConfig(api);

        // Then — le tableau lecture seule liste les DEUX affectations déclarées (Alice / Bruno résolus).
        config.WaitForAssertion(
            () =>
            {
                Assert.Equal(2, config.FindAll("[data-testid='cycle-foyer']").Count);
                Assert.Equal("Alice", ResponsableLigne(LigneIndex(config, 0)));
                Assert.Equal("Bruno", ResponsableLigne(LigneIndex(config, 1)));
                // Finition PO s33 : libellés par parité (cycle ISO 2 semaines) au lieu de « Semaine d'index k ».
                Assert.Equal("Semaine paire", LibelleLigne(LigneIndex(config, 0)));
                Assert.Equal("Semaine impaire", LibelleLigne(LigneIndex(config, 1)));
            },
            TimeSpan.FromSeconds(10));

        // … un crayon « Éditer le cycle » est présent ; l'inline #form-cycle n'est PLUS rendu (modal fermée).
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-cycle']"));
        Assert.Empty(config.FindAll("[data-testid='champ-nombre-semaines']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-cycle']"));
    }

    [Fact]
    public void Le_crayon_ouvre_la_modal_preremplie_et_Definir_le_cycle_persiste_puis_relit_le_tableau()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var config = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='cycle-foyer']").Count == 2, TimeSpan.FromSeconds(10));

        // When — je clique « Éditer le cycle » : la modal s'ouvre PRÉ-REMPLIE (N=2, semaine 0 = Alice, 1 = Bruno).
        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='dialog-cycle']", TimeSpan.FromSeconds(10));
        Assert.Equal("2", config.Find("[data-testid='champ-nombre-semaines']").GetAttribute("value"));
        Assert.Equal("parent-a", config.Find("[data-testid='champ-cycle-index-0']").GetAttribute("value"));
        Assert.Equal("parent-b", config.Find("[data-testid='champ-cycle-index-1']").GetAttribute("value"));

        // … je réaffecte la semaine 1 à parent-a puis « Définir le cycle » (POST réel /api/canal/definir-cycle).
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-a"));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());

        // Then — la modal se ferme et le tableau relu reflète la semaine 1 = Alice (parent-a).
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-cycle']"));
                Assert.Equal("Alice", ResponsableLigne(LigneIndex(config, 1)));
            },
            TimeSpan.FromSeconds(10));
    }
}
