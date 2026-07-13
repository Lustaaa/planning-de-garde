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
/// Sprint 34 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : la modal d'édition
/// d'un enfant propose un SÉLECTEUR des parents (acteurs portant le rôle « Parent », depuis le référentiel
/// acteurs + rôles), les parents déjà liés pré-cochés. Câblé à l'API distante réelle (store réel) : lier
/// un parent puis « Enregistrer » émet la commande « lier » (POST /api/canal/lier-enfant-parent) — la
/// colonne « Parents liés » de la table reflète le nouveau parent ; délier puis « Enregistrer » émet
/// « délier » (POST /api/canal/delier-enfant-parent) — le parent disparaît. Un acteur NON-Parent n'est
/// pas proposé. HORS scope : familles recomposées / exactement-2 / vue graphe (le sélecteur borne à 2).
/// </summary>
public sealed class FrontWasmConfigEnfantsSelecteurParentsModalTests : TestContext
{
    private static string? Prenom(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static string ParentsLies(IRenderedComponent<ConfigurationFoyer> config)
        => config.FindAll("[data-testid='enfant-foyer']").Single(li => Prenom(li) == "Léa")
            .QuerySelector("[data-testid='enfant-parents-lies']")!.TextContent.Trim();

    /// <summary>Sème le rôle « Parent » et l'affecte à parent-a (Alice) & parent-b (Bruno) ; parent-b garde
    /// aussi le rôle mais Bruno servira de non-candidat quand on lui retire le rôle. Ici : Alice & Bruno Parents.</summary>
    private static void SemerParents(ApiDistanteFactory api)
    {
        var roles = api.Services.GetRequiredService<IEditeurReferentielRoles>();
        roles.Creer("role-parent", "Parent");
        var config = api.Services.GetRequiredService<IEditeurConfigurationFoyer>();
        config.AffecterRole("parent-a", "role-parent");
        config.AffecterRole("parent-b", "role-parent");
    }

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='enfant-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Le_selecteur_ne_propose_que_les_acteurs_Parent_lier_puis_delier_reflete_la_colonne_parents_lies()
    {
        // Given — Alice (parent-a) & Bruno (parent-b) portent le rôle « Parent » ; « Léa » sans parent lié.
        using var api = new ApiDistanteFactory();
        SemerParents(api);
        var config = RendreConfig(api);
        Assert.Equal("—", ParentsLies(config)); // aucun parent lié au départ

        // When — j'ouvre la modal de « Léa » : le sélecteur propose exactement les acteurs Parent (Alice, Bruno),
        // et AUCUN non-Parent (Léa n'est pas un acteur ; les seuls acteurs Parents sont Alice/Bruno).
        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
        var candidats = config.FindAll("[data-testid='checkbox-parent']")
            .Select(c => c.GetAttribute("data-acteur-id")).ToList();
        Assert.Contains("parent-a", candidats);
        Assert.Contains("parent-b", candidats);
        Assert.Equal(2, candidats.Count); // que les Parents, pas d'autre acteur

        // … je coche Alice (parent-a) puis « Enregistrer » → POST réel lier-enfant-parent.
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(true));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la modal se ferme et la colonne « Parents liés » de « Léa » reflète « Alice ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                Assert.Equal("Alice (parent)", ParentsLies(config));
            },
            TimeSpan.FromSeconds(10));

        // Preuve store réel : le lien est bien persisté côté domaine (pas seulement à l'écran).
        Assert.Contains(
            api.Services.GetRequiredService<IEnumerationEnfants>().EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies,
            p => p.ActeurId == "parent-a");

        // When — je rouvre, Alice est PRÉ-COCHÉE ; je la décoche puis « Enregistrer » → POST réel delier-enfant-parent.
        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
        Assert.True(config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").HasAttribute("checked"));
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(false));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la colonne « Parents liés » redevient vide (« — ») ; le domaine ne porte plus le lien.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                Assert.Equal("—", ParentsLies(config));
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IEnumerationEnfants>()
            .EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies);
    }
}
