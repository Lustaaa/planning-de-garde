using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : dans la modal d'édition d'un acteur
/// (Parent, écran réellement câblé à l'API distante réelle, store réel, DI réelle), l'état « admin » et
/// l'état « actif » sont matérialisés en <b>TOGGLES</b> pré-réglés sur l'état courant (et non plus en
/// boutons d'action immédiats). SENS UNIQUE (décision SM s33) : seul OFF→ON est promis (les commandes
/// inverses n'existent pas), un toggle déjà ON est VERROUILLÉ (disabled). Basculer le toggle admin de OFF
/// vers ON puis « Enregistrer » émet la commande EXISTANTE designer-admin via le canal HTTP réel ; en
/// succès la modal se ferme et le tableau relu reflète le nouvel état (badge admin), l'identifiant stable
/// restant inchangé. Le toggle « actif » n'est actionnable que si l'acteur porte un compte.
/// </summary>
public sealed class FrontWasmConfigActeursToggleAdminActifModalTests : TestContext
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
    public void Basculer_le_toggle_admin_de_OFF_vers_ON_puis_Enregistrer_ferme_la_modal_et_le_tableau_reflete_l_admin_sur_le_meme_identifiant()
    {
        // Given — écran câblé (Parent), modal d'édition ouverte sur parent-a (Alice, Parent, non admin).
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);
        Assert.Empty(LigneDe(config, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']")); // baseline : pas admin

        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // Then (modal rendue) — l'admin est un TOGGLE (plus un bouton d'action immédiat), pré-réglé OFF.
        var toggleAdmin = config.Find("[data-testid='toggle-admin']");
        Assert.False(toggleAdmin.HasAttribute("checked") || toggleAdmin.HasAttribute("disabled")); // OFF, actionnable
        Assert.Empty(config.FindAll("[data-testid='bouton-designer-admin']")); // l'ancien bouton n'existe plus

        // When — je bascule le toggle admin OFF→ON puis j'enregistre (POST réel /api/canal/designer-admin).
        this.SurDispatcher(() => config.Find("[data-testid='toggle-admin']").Change(true));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal se ferme, le tableau relu marque Alice « admin », sur le MÊME id stable parent-a.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var alice = LigneDe(config, "Alice");
                Assert.Equal("parent-a", alice.GetAttribute("data-acteur-id"));
                Assert.NotEmpty(alice.QuerySelectorAll("[data-testid='acteur-admin-marqueur']"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Le_toggle_admin_deja_ON_est_actionnable_s41_et_le_toggle_actif_reste_desactive_sans_compte()
    {
        // Given — parent-a est DÉJÀ admin dans le store réel (désignation directe via le port), et ne porte
        // aucun compte. Depuis s41 (verrou ON s33 LEVÉ), la modal ouverte rend le toggle admin ON mais
        // ACTIONNABLE (le sens OFF émet la vraie commande de dé-désignation). Le toggle actif reste DÉSACTIVÉ
        // (pas de compte à activer/désactiver), avec un motif dedans.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurAdminsFoyer>().DesignerAdmin("parent-a");
        var config = RendreConfig(api);

        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        var toggleAdmin = config.Find("[data-testid='toggle-admin']");
        Assert.False(toggleAdmin.HasAttribute("disabled")); // s41 : le sens OFF est actionnable (verrou levé)

        var toggleActif = config.Find("[data-testid='toggle-actif']");
        Assert.True(toggleActif.HasAttribute("disabled")); // pas de compte → non actionnable
        Assert.NotNull(config.Find("[data-testid='toggle-actif-motif']"));
    }
}
