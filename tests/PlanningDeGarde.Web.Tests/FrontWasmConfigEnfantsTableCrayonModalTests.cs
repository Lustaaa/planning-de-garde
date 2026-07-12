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
/// Sprint 34 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : l'onglet « Enfants »
/// adopte le patron s32/s33 « tableau lecture seule + crayon → modal ». Sous Parent (écran réellement câblé
/// à l'API distante réelle, store réel), chaque enfant est sur UNE ligne en lecture seule (prénom + colonne
/// « Parents liés » résolue en noms), avec une colonne Actions portant un CRAYON et un bouton « Ajouter un
/// enfant ». La surface d'édition INLINE préexistante (form-ajouter-enfant, champ-editer-enfant,
/// bouton-editer-enfant) n'est PLUS rendue (lot atomique de surface, MÊME commit). Le crayon ouvre une modal
/// pré-remplie (prénom + parents liés courants) → « Enregistrer » émet editer-enfant EXISTANTE ; « Ajouter »
/// ouvre la MÊME modal VIDE → « Enregistrer » crée un enfant (id stable neuf).
/// </summary>
public sealed class FrontWasmConfigEnfantsTableCrayonModalTests : TestContext
{
    private static string? Prenom(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParPrenom(IRenderedComponent<ConfigurationFoyer> config, string prenom)
        => config.FindAll("[data-testid='enfant-foyer']").Single(li => Prenom(li) == prenom);

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
    public void La_table_des_enfants_est_en_lecture_seule_avec_colonne_parents_lies_crayon_et_bouton_ajouter_l_inline_est_retire()
    {
        // Given — « Léa » est semée et liée au parent-acteur « parent-a » (Alice) dans le store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a");
        var config = RendreConfig(api);

        // Then — la ligne de « Léa » est en LECTURE, sa colonne « Parents liés » résout « Alice », un crayon
        // et un bouton « Ajouter un enfant » sont présents.
        var ligne = LigneParPrenom(config, "Léa");
        Assert.Equal("Alice", ligne.QuerySelector("[data-testid='enfant-parents-lies']")!.TextContent.Trim());
        Assert.NotNull(ligne.QuerySelector("[data-testid='crayon-enfant']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-enfant']"));

        // … et l'INLINE préexistant n'est plus rendu : ni formulaire d'ajout inline, ni champ/bouton d'édition
        // inline, et la modal est fermée (dialog-enfant absent tant qu'on n'ouvre pas).
        Assert.Empty(config.FindAll("#form-ajouter-enfant"));
        Assert.Empty(config.FindAll("[data-testid='champ-editer-enfant']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-editer-enfant']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
    }

    [Fact]
    public void Le_crayon_ouvre_une_modal_preremplie_qui_edite_le_prenom_et_Ajouter_ouvre_la_meme_modal_vide_qui_cree()
    {
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a");
        var config = RendreConfig(api);

        // When (édition) — clic crayon de « Léa » : la modal s'ouvre PRÉ-REMPLIE (prénom « Léa » + parent lié « Alice »).
        this.SurDispatcher(() => LigneParPrenom(config, "Léa").QuerySelector("[data-testid='crayon-enfant']")!.Click());
        config.WaitForElement("[data-testid='dialog-enfant']", TimeSpan.FromSeconds(10));
        Assert.Equal("Léa", config.Find("[data-testid='champ-prenom-enfant']").GetAttribute("value"));
        Assert.Equal("Alice", config.Find("[data-testid='enfant-parents-lies-modal']").TextContent.Trim());

        // … je renomme puis j'enregistre (POST réel /api/canal/editer-enfant) : modal fermée, table relue, même id.
        this.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Léana"));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                Assert.Contains(config.FindAll("[data-testid='enfant-foyer']"), li => Prenom(li) == "Léana");
                Assert.DoesNotContain(config.FindAll("[data-testid='enfant-foyer']"), li => Prenom(li) == "Léa");
            },
            TimeSpan.FromSeconds(10));

        // When (création) — « Ajouter un enfant » ouvre la MÊME modal VIDE → j'y crée « Tom » (id stable neuf).
        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-enfant']").Click());
        config.WaitForElement("[data-testid='dialog-enfant']", TimeSpan.FromSeconds(10));
        Assert.Equal("", config.Find("[data-testid='champ-prenom-enfant']").GetAttribute("value"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Tom"));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                Assert.Contains(config.FindAll("[data-testid='enfant-foyer']"), li => Prenom(li) == "Tom");
            },
            TimeSpan.FromSeconds(10));
    }
}
