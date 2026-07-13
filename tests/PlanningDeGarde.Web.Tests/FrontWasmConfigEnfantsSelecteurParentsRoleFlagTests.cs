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
/// Sprint 36 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE, jamais une
/// doublure) : le sélecteur des parents de la modal Enfants (<c>ActeursParents()</c>) énumère désormais les
/// acteurs dont le rôle affecté est marqué « est rôle parent » (option B1, s36 — le FLAG du rôle, jamais le
/// libellé ni le TypeActeur). Sur le SEED démo : Alice (→ Papa marqué parent) & Bruno (→ Maman marqué parent)
/// APPARAISSENT ; Nina (→ Nounou non marqué), grand-père (→ Grand-parent non marqué) et Marie-Hélène (sans
/// rôle) N'APPARAISSENT PAS. Lier un parent éligible puis « Enregistrer » émet la commande « lier » (POST
/// /api/canal/lier-enfant-parent) via le canal HTTP réel ; la colonne « Parents liés » suit. L'IHM suit
/// EXACTEMENT la règle back (Sc.4) — aucun critère d'éligibilité divergent.
/// </summary>
public sealed class FrontWasmConfigEnfantsSelecteurParentsRoleFlagTests : TestContext
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

    [Fact]
    public void Le_selecteur_ne_propose_que_les_acteurs_a_role_marque_parent_du_seed_et_lier_reflete_la_colonne()
    {
        // Given — SEED démo tel quel (aucun montage sur-mesure) : Alice → Papa & Bruno → Maman (rôles marqués
        // parent) ; Nina → Nounou, grand-père → Grand-parent (non marqués) ; Marie-Hélène sans rôle.
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);
        Assert.Equal("—", ParentsLies(config));

        // When — j'ouvre la modal de « Léa » : le sélecteur énumère les acteurs à rôle marqué parent.
        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
        var candidats = config.FindAll("[data-testid='checkbox-parent']")
            .Select(c => c.GetAttribute("data-acteur-id")).ToList();

        // Then — Alice & Bruno (rôle marqué parent) proposés ; Nina, grand-père (rôle non marqué) et
        // Marie-Hélène (sans rôle) absents.
        Assert.Contains("parent-a", candidats);   // Alice → Papa (marqué parent)
        Assert.Contains("parent-b", candidats);   // Bruno → Maman (marqué parent)
        Assert.DoesNotContain("nounou", candidats);     // Nina → Nounou (non marqué)
        Assert.DoesNotContain("grand-pere", candidats); // grand-père → Grand-parent (non marqué)
        Assert.DoesNotContain("parent-c", candidats);   // Marie-Hélène → sans rôle
        Assert.Equal(2, candidats.Count);

        // … je coche Alice puis « Enregistrer » → POST réel lier-enfant-parent.
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(true));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la modal se ferme et la colonne « Parents liés » reflète « Alice » ; le domaine porte le lien.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                Assert.Equal("Alice", ParentsLies(config));
            },
            TimeSpan.FromSeconds(10));
        Assert.Contains(
            api.Services.GetRequiredService<IEnumerationEnfants>().EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies,
            p => p.ActeurId == "parent-a");
    }
}
