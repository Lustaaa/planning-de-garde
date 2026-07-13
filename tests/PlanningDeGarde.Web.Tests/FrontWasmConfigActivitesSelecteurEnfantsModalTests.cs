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
/// Sprint 35 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : la modal d'édition
/// d'une activité propose un SÉLECTEUR des enfants (référentiel enfants s30), les enfants déjà liés
/// pré-cochés. Câblé à l'API distante réelle (store réel) : lier un enfant puis « Enregistrer » émet la
/// commande « lier » (POST /api/canal/lier-enfant-activite, Sc.3) — la colonne « Enfants liés » de la table
/// reflète le nouvel enfant ; délier puis « Enregistrer » émet « délier » (POST /api/canal/delier-enfant-activite)
/// — l'enfant disparaît. Lien N-M : le sélecteur ne borne PAS la cardinalité (0..N). HORS scope : liste de
/// slots par activité, lien adresse acteur↔lieu.
/// </summary>
public sealed class FrontWasmConfigActivitesSelecteurEnfantsModalTests : TestContext
{
    private static string? Libelle(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParLibelle(IRenderedComponent<ConfigurationFoyer> config, string libelle)
        => config.FindAll("[data-testid='activite-foyer']").Single(li => Libelle(li) == libelle);

    private static string EnfantsLies(IRenderedComponent<ConfigurationFoyer> config, string libelle)
        => LigneParLibelle(config, libelle).QuerySelector("[data-testid='activite-enfants-lies']")!.TextContent.Trim();

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='activite-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Le_selecteur_propose_les_enfants_lier_puis_delier_reflete_la_colonne_enfants_lies()
    {
        // Given — le référentiel enfants porte « Léa » (seed) et « Tom » ; l'activité « école » sans enfant lié.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("tom-id", "Tom");
        var config = RendreConfig(api);
        Assert.Equal("—", EnfantsLies(config, "école")); // aucun enfant lié au départ

        // When — j'ouvre la modal de « école » : le sélecteur propose exactement les enfants du référentiel
        // (Léa, Tom), aucun pré-coché.
        this.SurDispatcher(() => LigneParLibelle(config, "école").QuerySelector("[data-testid='crayon-activite']")!.Click());
        config.WaitForElement("[data-testid='selecteur-enfants-activite']", TimeSpan.FromSeconds(10));
        var candidats = config.FindAll("[data-testid='checkbox-enfant-activite']")
            .Select(c => c.GetAttribute("data-enfant-id")).ToList();
        Assert.Contains("Léa", candidats);
        Assert.Contains("tom-id", candidats);
        Assert.Equal(2, candidats.Count);

        // … je coche « Léa » puis « Enregistrer » → POST réel lier-enfant-activite.
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-enfant-activite']")
            .Single(c => c.GetAttribute("data-enfant-id") == "Léa").Change(true));
        this.SurDispatcher(() => config.Find("#form-activite").Submit());

        // Then — la modal se ferme et la colonne « Enfants liés » de « école » reflète « Léa ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));
                Assert.Equal("Léa", EnfantsLies(config, "école"));
            },
            TimeSpan.FromSeconds(10));

        // Preuve store réel : le lien est bien persisté côté domaine (pas seulement à l'écran).
        Assert.Contains("Léa",
            api.Services.GetRequiredService<IEnumerationActivites>().EnumererActivites().Single(a => a.Id == "école").EnfantsLies);

        // When — je rouvre, « Léa » est PRÉ-COCHÉE ; je la décoche puis « Enregistrer » → POST réel delier-enfant-activite.
        this.SurDispatcher(() => LigneParLibelle(config, "école").QuerySelector("[data-testid='crayon-activite']")!.Click());
        config.WaitForElement("[data-testid='selecteur-enfants-activite']", TimeSpan.FromSeconds(10));
        Assert.True(config.FindAll("[data-testid='checkbox-enfant-activite']")
            .Single(c => c.GetAttribute("data-enfant-id") == "Léa").HasAttribute("checked"));
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-enfant-activite']")
            .Single(c => c.GetAttribute("data-enfant-id") == "Léa").Change(false));
        this.SurDispatcher(() => config.Find("#form-activite").Submit());

        // Then — la colonne « Enfants liés » redevient vide (« — ») ; le domaine ne porte plus le lien.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));
                Assert.Equal("—", EnfantsLies(config, "école"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IEnumerationActivites>()
            .EnumererActivites().Single(a => a.Id == "école").EnfantsLies);
    }
}
