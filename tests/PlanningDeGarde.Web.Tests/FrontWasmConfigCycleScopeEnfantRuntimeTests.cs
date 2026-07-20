using System;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 53 — Sc.16 (🖥️ @ihm) — NIVEAU RUNTIME, gate G3 (3e passage : « Éditer le cycle de fond ne prend pas
/// en compte l'enfant »). Sur l'écran de configuration, l'onglet Cycle porte un SÉLECTEUR D'ENFANT ; éditer le
/// cycle EN VUE de l'enfant A l'estampille de A → la grille de A résout SON cycle, celle de B reste sur le sien.
/// La dialog affiche l'enfant courant en LECTURE SEULE. Câblage RÉEL (config + grille sur la MÊME API distante).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigCycleScopeEnfantRuntimeTests : TestContext
{
    private const string LeaId = "Léa";
    private const string TomId = "tom";

    private void CablerServices(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(GrilleRuntimeHarness.SessionConnectee());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(GrilleRuntimeHarness.Lundi_29_06_2026));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
    }

    private void EditerCyclePourEnfant(IRenderedComponent<ConfigurationFoyer> config, string enfantId, string prenom, string responsableId)
    {
        // Onglet Cycle actif + sélection de l'enfant courant du cycle.
        this.SurDispatcher(() => config.Find("[data-testid='onglet-cycles']").Click());
        config.WaitForElement("[data-testid='selecteur-enfant-cycle']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='selecteur-enfant-cycle']").Change(enfantId));

        // Ouvre la modal, affiche l'enfant courant en LECTURE SEULE, édite N=1 index0 → responsable, valide.
        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='cycle-enfant-courant']", TimeSpan.FromSeconds(10));
        Assert.Contains(prenom, config.Find("[data-testid='cycle-enfant-courant']").TextContent);
        config.WaitForElement("[data-testid='champ-nombre-semaines']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nombre-semaines']").Change("1"));
        config.WaitForElement("[data-testid='champ-cycle-index-0']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-0']").Change(responsableId));
        this.SurDispatcher(() => config.Find("#form-cycle").Submit());
        config.WaitForElement("[data-testid='confirmation-cycle']", TimeSpan.FromSeconds(10));
    }

    private static string? Resolu(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).QuerySelector("[data-testid='nom-responsable']")?.TextContent.Trim();

    private void Selectionner(IRenderedComponent<PlanningPartage> grille, string enfantId)
        => this.SurDispatcher(() => grille.Find("[data-testid='selecteur-enfant-carte']").Change(enfantId));

    [Fact]
    public void Editer_le_cycle_en_vue_Lea_change_le_cycle_de_Lea_seul_dialog_enfant_lecture_seule()
    {
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter(TomId, "Tom");
        CablerServices(api);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(() => config.FindAll("[data-testid='acteur-foyer']").Count > 0, TimeSpan.FromSeconds(10));

        // Léa → cycle N=1 Alice ; Tom → cycle N=1 Bruno (deux éditions EN VUE de chaque enfant ; la modal
        // affiche l'enfant courant en lecture seule à chaque édition — vérifié dans EditerCyclePourEnfant).
        EditerCyclePourEnfant(config, LeaId, "Léa", "parent-a");
        EditerCyclePourEnfant(config, TomId, "Tom", "parent-b");

        // La grille câblée à la MÊME API résout le cycle PROPRE de chaque enfant.
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));

        Selectionner(grille, LeaId);
        grille.WaitForAssertion(() => Assert.Equal("Alice", Resolu(grille, "29/06")), TimeSpan.FromSeconds(10));
        Selectionner(grille, TomId);
        grille.WaitForAssertion(() => Assert.Equal("Bruno", Resolu(grille, "29/06")), TimeSpan.FromSeconds(10));
    }
}
