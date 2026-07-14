using System;
using System.Linq;
using AngleSharp.Html.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 37 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : la modal d'édition
/// d'un enfant propose, par parent lié, un SÉLECTEUR rôle-du-lien (père / mère / parent), pré-réglé sur son
/// rôle courant. Câblée à l'API distante réelle (store réel) : changer le rôle puis « Enregistrer » émet la
/// commande « lier » (rôle inclus, POST /api/canal/lier-enfant-parent) et la modal se ferme. Un rôle-du-lien
/// REFUSÉ par le domaine (deux « père ») laisse la modal OUVERTE, le motif DEDANS, la sélection (parents +
/// rôles) CONSERVÉE, le tableau INCHANGÉ (aucune écriture partielle). La fermeture Échap est prouvée par
/// <c>FrontWasmConfigModalsEchapFermeSansMutationTests</c> (port IEcouteurEchapModal, s33).
/// </summary>
public sealed class FrontWasmConfigEnfantsSelecteurRoleDuLienTests : TestContext
{
    // Seed Testing : Alice (parent-a) → rôle Papa (marqué parent), Bruno (parent-b) → Maman (marqué parent),
    // enfant « Léa ». Les deux acteurs sont donc éligibles au lien sans amorçage supplémentaire.

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

    private static void OuvrirModalLea(TestContext ctx, IRenderedComponent<ConfigurationFoyer> config)
    {
        ctx.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
    }

    private static IHtmlSelectElement RoleSelect(IRenderedComponent<ConfigurationFoyer> config, string acteurId)
        => (IHtmlSelectElement)config.FindAll("[data-testid='role-parent-select']")
            .Single(s => s.GetAttribute("data-acteur-id") == acteurId);

    [Fact]
    public void Le_selecteur_role_du_lien_est_pre_regle_puis_changer_le_role_et_enregistrer_emet_lier_avec_le_role()
    {
        // Given — « Léa » liée à Alice (parent-a) avec le rôle « mère » dans le store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a", RoleDuLien.Mere);
        var config = RendreConfig(api);
        OuvrirModalLea(this, config);

        // Then (pré-réglage) — le sélecteur de rôle-du-lien d'Alice est pré-réglé sur « mère ».
        Assert.Equal("Mere", RoleSelect(config, "parent-a").Value);

        // When — je change le rôle-du-lien d'Alice en « père » puis « Enregistrer ».
        this.SurDispatcher(() => RoleSelect(config, "parent-a").Change("Pere"));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la modal se ferme et le store réel porte Alice avec le rôle-du-lien « père ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                var lea = api.Services.GetRequiredService<IEnumerationEnfants>().EnumererEnfants().Single(e => e.Id == "Léa");
                Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single(p => p.ActeurId == "parent-a").Role);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Un_role_refuse_deux_peres_laisse_la_modal_ouverte_motif_dedans_selection_conservee_table_inchangee()
    {
        // Given — « Léa » liée à Bruno (parent-b) avec le rôle « père » dans le store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-b", RoleDuLien.Pere);
        var config = RendreConfig(api);
        OuvrirModalLea(this, config);

        // When — je coche Alice (parent-a) ET je lui mets le rôle « père » (second père), puis « Enregistrer ».
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(true));
        this.SurDispatcher(() => RoleSelect(config, "parent-a").Change("Pere"));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la modal RESTE OUVERTE, le motif de refus est DEDANS, la sélection est CONSERVÉE (Alice cochée,
        // rôle « père » toujours sélectionné).
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-enfant']");
                Assert.Equal("un père est déjà lié à cet enfant",
                    modal.QuerySelector("[data-testid='motif-echec-enfant']")!.TextContent.Trim());
                Assert.True(config.FindAll("[data-testid='checkbox-parent']")
                    .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").HasAttribute("checked"));
                Assert.Equal("Pere", RoleSelect(config, "parent-a").Value);
            },
            TimeSpan.FromSeconds(10));

        // … et le tableau est INCHANGÉ : aucune écriture partielle (Alice non liée, Bruno toujours seul père).
        var lea = api.Services.GetRequiredService<IEnumerationEnfants>().EnumererEnfants().Single(e => e.Id == "Léa");
        Assert.Single(lea.ParentsLies);
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single(p => p.ActeurId == "parent-b").Role);
        Assert.DoesNotContain(lea.ParentsLies, p => p.ActeurId == "parent-a");
    }
}
