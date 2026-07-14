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
/// Sprint 41 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le toggle actif / admin de la modal
/// Acteurs devient <b>bi-directionnel</b> (fin du verrou ON s33). Écran réellement câblé à l'API distante
/// réelle (store réel, DI réelle, canal HTTP réel). Basculer le toggle admin OFF émet la VRAIE commande
/// de dé-désignation (Sc.1) via le canal HTTP — PAS un no-op silencieux (anti-vert-qui-ment) : en succès
/// la modal se ferme, le tableau relu reflète OFF sans rechargement, et l'effet SURVIT (round-trip store
/// réel). De même, basculer le toggle actif OFF émet la commande de désactivation (Sc.3). Le verrou ON
/// s33 est LEVÉ pour les deux toggles (le sens OFF est désormais actionnable).
/// </summary>
public sealed class FrontWasmConfigActeursToggleOffBidirectionnelRuntimeTests : TestContext
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
    public void Basculer_le_toggle_admin_ON_vers_OFF_puis_Enregistrer_de_designe_l_admin_via_le_canal_et_le_tableau_reflete_OFF()
    {
        // Given — parent-a (Alice) ET parent-b (Bruno) sont admins dans le store réel (deux admins : la
        // dé-désignation d'Alice n'est PAS bloquée par la borne « dernier admin », Sc.2).
        using var api = new ApiDistanteFactory();
        var editeurAdmins = api.Services.GetRequiredService<IEditeurAdminsFoyer>();
        editeurAdmins.DesignerAdmin("parent-a");
        editeurAdmins.DesignerAdmin("parent-b");
        var config = RendreConfig(api);
        Assert.NotEmpty(LigneDe(config, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']")); // baseline : admin

        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // Then (modal rendue) — le toggle admin est ON mais ACTIONNABLE (verrou ON s33 LEVÉ).
        var toggleAdmin = config.Find("[data-testid='toggle-admin']");
        Assert.False(toggleAdmin.HasAttribute("disabled")); // sens OFF actionnable

        // When — je bascule le toggle admin ON→OFF puis j'enregistre (POST réel /api/canal/de-designer-admin).
        this.SurDispatcher(() => config.Find("[data-testid='toggle-admin']").Change(false));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal se ferme, le tableau relu ne marque PLUS Alice « admin » (l'effet a atteint le store).
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var alice = LigneDe(config, "Alice");
                Assert.Equal("parent-a", alice.GetAttribute("data-acteur-id"));
                Assert.Empty(alice.QuerySelectorAll("[data-testid='acteur-admin-marqueur']"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Basculer_le_toggle_actif_ON_vers_OFF_puis_Enregistrer_desactive_le_compte_via_le_canal_et_le_tableau_reflete_inactif()
    {
        // Given — parent-a (Alice) porte un compte ACTIF dans le store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s41", "alice@foyer.fr", StatutCompte.Actif, "parent-a");
        var config = RendreConfig(api);
        Assert.Contains("actif", LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']")!.TextContent,
            StringComparison.OrdinalIgnoreCase);

        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // Then (modal rendue) — le toggle actif est ON mais ACTIONNABLE (verrou ON s33 LEVÉ).
        var toggleActif = config.Find("[data-testid='toggle-actif']");
        Assert.False(toggleActif.HasAttribute("disabled")); // sens OFF actionnable

        // When — je bascule le toggle actif ON→OFF puis j'enregistre (POST réel /api/canal/desactiver-compte).
        this.SurDispatcher(() => config.Find("[data-testid='toggle-actif']").Change(false));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal se ferme, la ligne relue montre le compte « inactif » (l'effet a atteint le store).
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var compte = LigneDe(config, "Alice").QuerySelector("[data-testid='compte-acteur']");
                Assert.NotNull(compte);
                Assert.Contains("inactif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));
    }
}
