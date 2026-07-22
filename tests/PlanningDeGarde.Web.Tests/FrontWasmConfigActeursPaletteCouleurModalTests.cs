using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : dans la modal d'édition d'un acteur
/// (Parent, écran réellement câblé à l'API distante réelle), la couleur se choisit désormais dans une
/// PALETTE (picker minimal réutilisant le set de couleurs) — plus un menu déroulant. La couleur COURANTE
/// de l'acteur est pré-sélectionnée. Choisir une pastille puis « Enregistrer » persiste la couleur via la
/// commande EXISTANTE editer-acteur (canal HTTP réel) ; en succès la modal se ferme et la table en lecture
/// reflète la nouvelle couleur. HORS scope : aucune gestion de palette custom.
/// </summary>
public sealed class FrontWasmConfigActeursPaletteCouleurModalTests : TestContext
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
    public void La_palette_prereselectionne_la_couleur_courante_et_choisir_une_pastille_puis_Enregistrer_recolorie_l_acteur_dans_la_table()
    {
        // Given — parent-a (Alice) est « bleu » au seed ; écran câblé sous Parent, modal ouverte.
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // Then (modal rendue) — une PALETTE est proposée et la couleur courante (bleu) est pré-sélectionnée.
        Assert.NotNull(config.Find("[data-testid='palette-couleur']"));
        Assert.Contains("selectionnee",
            config.Find("[data-testid='pastille-couleur-bleu']").GetAttribute("class"));
        Assert.DoesNotContain("selectionnee",
            config.Find("[data-testid='pastille-couleur-vert']").GetAttribute("class"));

        // When — je choisis la pastille « vert » puis j'enregistre (POST réel /api/canal/editer-acteur).
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-vert']").Click());
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal se ferme et la table en lecture reflète la nouvelle couleur (vert) sur Alice.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                Assert.Equal("vert", LigneDe(config, "Alice").GetAttribute("data-couleur"));
            },
            TimeSpan.FromSeconds(10));
    }
}
