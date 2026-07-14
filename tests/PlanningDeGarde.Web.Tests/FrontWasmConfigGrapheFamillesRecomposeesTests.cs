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
/// Sprint 38 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE, store réel).
/// Familles recomposées visibles PAR CONSTRUCTION : deux enfants liés à des parents-acteurs DIFFÉRENTS
/// apparaissent comme DEUX RACINES distinctes, chacune avec ses propres branches ; un parent-acteur lié à
/// DEUX enfants (parent partagé) apparaît en branche SOUS CHACUN des deux enfants (reflet fidèle des liens
/// réels). AUCUN nouvel invariant n'est imposé (ni « exactement 2 parents », ni complétude du couple —
/// hors scope) : le graphe reste cohérent quel que soit le nombre d'enfants et de parents partagés.
/// </summary>
public sealed class FrontWasmConfigGrapheFamillesRecomposeesTests : TestContext
{
    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='graphe-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    private static AngleSharp.Dom.IElement Racine(IRenderedComponent<ConfigurationFoyer> config, string enfantId)
        => config.FindAll("[data-testid='graphe-enfant-racine']").Single(r => r.GetAttribute("data-enfant-id") == enfantId);

    private static bool BrancheVers(AngleSharp.Dom.IElement racine, string acteurId)
        => racine.QuerySelectorAll("[data-testid='graphe-parent-branche']")
            .Any(b => b.GetAttribute("data-acteur-id") == acteurId);

    [Fact]
    public void Deux_enfants_de_parents_differents_sont_deux_racines_distinctes_avec_leurs_propres_branches()
    {
        // Given — deux enfants (Léa seedée + Tom ajouté), liés à des parents-acteurs DIFFÉRENTS.
        using var api = new ApiDistanteFactory();
        var editeur = api.Services.GetRequiredService<IEditeurEnfants>();
        editeur.Ajouter("Tom", "Tom");
        editeur.LierParent("Léa", "parent-a", RoleDuLien.Mere);   // Léa → Alice
        editeur.LierParent("Tom", "parent-b", RoleDuLien.Pere);   // Tom → Bruno

        var config = RendreConfig(api);

        config.WaitForAssertion(
            () =>
            {
                // Deux racines distinctes.
                var lea = Racine(config, "Léa");
                var tom = Racine(config, "Tom");
                // Chacune porte SES propres branches (pas de fuite croisée).
                Assert.True(BrancheVers(lea, "parent-a"));
                Assert.False(BrancheVers(lea, "parent-b"));
                Assert.True(BrancheVers(tom, "parent-b"));
                Assert.False(BrancheVers(tom, "parent-a"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Un_parent_partage_apparait_en_branche_sous_chacun_des_deux_enfants()
    {
        // Given — un parent-acteur (Alice, parent-a) lié à DEUX enfants distincts (Léa et Tom).
        using var api = new ApiDistanteFactory();
        var editeur = api.Services.GetRequiredService<IEditeurEnfants>();
        editeur.Ajouter("Tom", "Tom");
        editeur.LierParent("Léa", "parent-a", RoleDuLien.Mere);
        editeur.LierParent("Tom", "parent-a", RoleDuLien.Mere);

        var config = RendreConfig(api);

        config.WaitForAssertion(
            () =>
            {
                // Alice apparaît en branche SOUS Léa ET SOUS Tom (reflet fidèle, aucune dédup inter-enfants).
                Assert.True(BrancheVers(Racine(config, "Léa"), "parent-a"));
                Assert.True(BrancheVers(Racine(config, "Tom"), "parent-a"));
            },
            TimeSpan.FromSeconds(10));
    }
}
