using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.8 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : l'onglet « Rôles » adopte le patron
/// s32 « tableau lecture seule + crayon → modal ». Sous Parent (écran réellement câblé à l'API distante
/// réelle, store réel), chaque rôle est sur UNE ligne en lecture seule avec une colonne Actions portant un
/// CRAYON, et un bouton « Ajouter un rôle » figure au bas. Toute surface d'édition INLINE préexistante
/// (champ de renommage, bouton renommer, formulaire de création en tête) n'est PLUS rendue (lot atomique).
/// Le crayon ouvre une modal pré-remplie → « Enregistrer » émet renommer-role EXISTANTE (Sc.2) ; « Ajouter »
/// ouvre la MÊME modal vide → « Enregistrer » crée un rôle (id stable neuf). Jamais une doublure.
/// </summary>
public sealed class FrontWasmConfigRolesTableCrayonModalTests : TestContext
{
    private static string? LibelleLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParLibelle(IRenderedComponent<ConfigurationFoyer> config, string libelle)
        => config.FindAll("[data-testid='role-foyer']").Single(li => LibelleLigne(li) == libelle);

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
    public void La_table_des_roles_est_en_lecture_seule_avec_crayon_et_bouton_ajouter_l_inline_est_retire()
    {
        // Given — un rôle « Nounou » est semé dans le store réel ; écran câblé sous Parent.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        var config = RendreConfig(api);

        // Then — la ligne du rôle est en LECTURE, avec un crayon ; un bouton « Ajouter un rôle » est présent.
        config.WaitForAssertion(
            () => Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Nounou"),
            TimeSpan.FromSeconds(10));
        var ligne = LigneParLibelle(config, "Nounou");
        Assert.NotNull(ligne.QuerySelector("[data-testid='crayon-role']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-role']"));

        // … et l'INLINE préexistant n'est plus rendu (modal fermée) : ni champ de renommage, ni bouton
        // renommer, ni formulaire de création en tête (champ-libelle-role vit désormais dans la modal).
        Assert.Empty(config.FindAll("[data-testid='champ-renommer-role']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-renommer-role']"));
        Assert.Empty(config.FindAll("[data-testid='champ-libelle-role']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-role']"));
    }

    [Fact]
    public void Le_crayon_ouvre_une_modal_preremplie_qui_renomme_et_Ajouter_ouvre_la_meme_modal_vide_qui_cree()
    {
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api); // le rôle « Nounou » est présent au référentiel (seed B2, s36)
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count > 0, TimeSpan.FromSeconds(10));

        // When (édition) — je clique le crayon du rôle : la modal s'ouvre PRÉ-REMPLIE avec « Nounou ».
        this.SurDispatcher(() => LigneParLibelle(config, "Nounou").QuerySelector("[data-testid='crayon-role']")!.Click());
        config.WaitForElement("[data-testid='dialog-role']", TimeSpan.FromSeconds(10));
        Assert.Equal("Nounou", config.Find("[data-testid='champ-libelle-role']").GetAttribute("value"));

        // … je renomme puis j'enregistre (POST réel /api/canal/renommer-role) : modal fermée, table relue.
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Assistante maternelle"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-role']"));
                Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Assistante maternelle");
                Assert.DoesNotContain(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Nounou");
            },
            TimeSpan.FromSeconds(10));

        // When (création) — « Ajouter un rôle » ouvre la MÊME modal VIDE → j'y crée « Baby-sitter » (libellé
        // neuf, non déjà semé).
        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        config.WaitForElement("[data-testid='dialog-role']", TimeSpan.FromSeconds(10));
        Assert.Equal("", config.Find("[data-testid='champ-libelle-role']").GetAttribute("value"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Baby-sitter"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-role']"));
                Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Baby-sitter");
            },
            TimeSpan.FromSeconds(10));
    }
}
