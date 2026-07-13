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
/// Sprint 36 — Sc.5 (🖥️ @ihm — PREUVE RUNTIME sur le SEED DÉMO, câblée à l'API distante RÉELLE, jamais une
/// doublure ni un seed sur-mesure adjacent) : sur le profil de données réel du foyer (Alice = parent-a et
/// Bruno = parent-b, tous deux <see cref="TypeActeur.Parent"/>), AUCUN rôle du référentiel nommé « Parent »
/// n'a besoin d'être créé pour rendre un parent liable (option A, s36). Le sélecteur de la modal Enfants les
/// propose DIRECTEMENT ; lier Alice PUIS Bruno reflète la colonne « Parents liés » avec les DEUX noms — la
/// distinction Papa/Maman se fait PAR LE NOM (aucun champ père/mère distinct, hors scope). La borne 0..2
/// (s34) reste tenue : un 3ᵉ parent est refusé (invariant inchangé).
/// </summary>
public sealed class FrontWasmConfigEnfantsSeedDemoParentsLiablesTypeActeurTests : TestContext
{
    private static string? Prenom(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static string ParentsLies(IRenderedComponent<ConfigurationFoyer> config)
        => config.FindAll("[data-testid='enfant-foyer']").Single(li => Prenom(li) == "Léa")
            .QuerySelector("[data-testid='enfant-parents-lies']")!.TextContent.Trim();

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

    private void OuvrirModalLea(IRenderedComponent<ConfigurationFoyer> config)
    {
        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
    }

    private static void Cocher(IRenderedComponent<ConfigurationFoyer> config, TestContext ctx, string acteurId)
        => ctx.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == acteurId).Change(true));

    [Fact]
    public void Seed_demo_Alice_et_Bruno_liables_directement_sans_role_Parent_distingues_par_le_nom()
    {
        // Given — SEED DÉMO tel quel : Alice (parent-a) & Bruno (parent-b) sont TypeActeur.Parent, AUCUN rôle
        // « Parent » créé dans le référentiel. « Léa » n'a aucun parent lié.
        using var api = new ApiDistanteFactory();
        Assert.DoesNotContain(
            api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles(),
            r => r.Libelle == "Parent"); // aucun rôle nommé « Parent » n'a été créé au préalable
        var config = RendreConfig(api);
        Assert.Equal("—", ParentsLies(config));

        // When — j'ouvre la modal de « Léa » : le sélecteur énumère les acteurs de type Parent du seed.
        OuvrirModalLea(config);
        var candidats = config.FindAll("[data-testid='checkbox-parent']")
            .Select(c => c.GetAttribute("data-acteur-id")).ToList();

        // Then — Alice ET Bruno sont proposés DIRECTEMENT (liables), sans rôle ; les non-Parents absents.
        Assert.Contains("parent-a", candidats); // Alice — Parent par nature, aucun rôle
        Assert.Contains("parent-b", candidats); // Bruno — Parent par nature, aucun rôle
        Assert.DoesNotContain("parent-c", candidats);  // Marie-Hélène — Admin
        Assert.DoesNotContain("grand-pere", candidats); // grand-père — Autre
        Assert.DoesNotContain("nounou", candidats);     // Nina — Autre
        Assert.Equal(2, candidats.Count);

        // … je lie Alice PUIS Bruno, puis « Enregistrer » → deux POST réels lier-enfant-parent.
        Cocher(config, this, "parent-a");
        Cocher(config, this, "parent-b");
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la modal se ferme et la colonne « Parents liés » distingue « Alice » ET « Bruno » PAR LE NOM.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                var parents = ParentsLies(config);
                Assert.Contains("Alice", parents);
                Assert.Contains("Bruno", parents);
            },
            TimeSpan.FromSeconds(10));

        // Preuve store réel : les DEUX liens sont persistés côté domaine (l'IHM suit exactement la règle back).
        var lies = api.Services.GetRequiredService<IEnumerationEnfants>()
            .EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies;
        Assert.Contains("parent-a", lies);
        Assert.Contains("parent-b", lies);
    }

    [Fact]
    public void Borne_0_2_parents_tenue_un_3e_parent_est_refuse_invariant_inchange()
    {
        // Given — un 3ᵉ acteur Parent-par-nature (ajouté = TypeActeur.Parent par défaut, borne anti-cliquet
        // règle 30) rejoint le seed démo : le foyer compte désormais 3 candidats Parent (Alice, Bruno, David).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().Ajouter("parent-d", "David", "rouge");
        var config = RendreConfig(api);
        OuvrirModalLea(config);
        Assert.Equal(3, config.FindAll("[data-testid='checkbox-parent']").Count); // Alice, Bruno, David tous Parent

        // When — je lie Alice PUIS Bruno (la borne « 2 max » est alors atteinte).
        Cocher(config, this, "parent-a");
        Cocher(config, this, "parent-b");

        // Then — le 3ᵉ parent (David) est REFUSÉ : sa case est désactivée à l'écran (borne 0..2 reflétée).
        config.WaitForAssertion(
            () => Assert.True(config.FindAll("[data-testid='checkbox-parent']")
                .Single(c => c.GetAttribute("data-acteur-id") == "parent-d").HasAttribute("disabled")),
            TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — après enregistrement, l'enfant porte EXACTEMENT 2 parents : David n'est PAS lié (borne tenue).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-enfant']")),
            TimeSpan.FromSeconds(10));
        var lies = api.Services.GetRequiredService<IEnumerationEnfants>()
            .EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies;
        Assert.Equal(2, lies.Count);
        Assert.DoesNotContain("parent-d", lies);
    }
}
