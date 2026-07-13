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
/// Sprint 35 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : l'onglet « Lieux » (s27)
/// est BASCULÉ (SWAP atomique) en onglet « Activités » au patron s32/s34 « tableau lecture seule + crayon →
/// modal ». Sous Parent (écran réellement câblé à l'API distante réelle, store réel), chaque activité est sur
/// UNE ligne de tableau en lecture seule (libellé + adresse + colonne « Enfants liés » résolue en prénoms),
/// avec une colonne Actions portant un CRAYON et un bouton « Ajouter une activité ». La surface d'édition
/// INLINE préexistante (form-ajouter-lieu, champ-libelle-lieu, liste-lieux, bouton-supprimer-lieu) n'est PLUS
/// rendue (lot atomique de surface, MÊME commit). Le crayon ouvre une modal pré-remplie (libellé + adresse +
/// enfants liés courants) → « Enregistrer » émet editer-activite EXISTANTE ; « Ajouter » ouvre la MÊME modal
/// VIDE → « Enregistrer » crée une activité (id stable neuf).
/// </summary>
public sealed class FrontWasmConfigActivitesTableCrayonModalTests : TestContext
{
    private static string? Libelle(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParLibelle(IRenderedComponent<ConfigurationFoyer> config, string libelle)
        => config.FindAll("[data-testid='activite-foyer']").Single(li => Libelle(li) == libelle);

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
    public void La_table_des_activites_est_en_lecture_seule_avec_adresse_enfants_lies_crayon_et_bouton_ajouter_l_inline_est_retire()
    {
        // Given — l'activité « école » est semée avec une adresse et liée à l'enfant « Léa » dans le store réel.
        using var api = new ApiDistanteFactory();
        var referentiel = api.Services.GetRequiredService<IEditeurActivites>();
        referentiel.ChangerAdresse("école", "12 rue des Écoliers");
        referentiel.LierEnfant("école", "Léa");
        var config = RendreConfig(api);

        // Then — la ligne « école » est en LECTURE : adresse rendue, colonne « Enfants liés » résout « Léa »,
        // un crayon et un bouton « Ajouter une activité » sont présents.
        var ligne = LigneParLibelle(config, "école");
        Assert.Equal("12 rue des Écoliers", ligne.QuerySelector("[data-testid='activite-adresse']")!.TextContent.Trim());
        Assert.Equal("Léa", ligne.QuerySelector("[data-testid='activite-enfants-lies']")!.TextContent.Trim());
        Assert.NotNull(ligne.QuerySelector("[data-testid='crayon-activite']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-activite']"));

        // … et l'INLINE « Lieux » préexistant n'est plus rendu : ni liste, ni formulaire/champ d'ajout inline,
        // ni bouton de suppression inline, et la modal est fermée (dialog-activite absent tant qu'on n'ouvre pas).
        Assert.Empty(config.FindAll("[data-testid='liste-lieux']"));
        Assert.Empty(config.FindAll("[data-testid='champ-libelle-lieu']"));
        Assert.Empty(config.FindAll("#form-ajouter-lieu"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer-lieu']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));
    }

    [Fact]
    public void Le_crayon_ouvre_une_modal_preremplie_qui_edite_libelle_et_adresse_et_Ajouter_ouvre_la_meme_modal_vide_qui_cree()
    {
        using var api = new ApiDistanteFactory();
        var referentiel = api.Services.GetRequiredService<IEditeurActivites>();
        referentiel.ChangerAdresse("nounou", "3 impasse du Bosquet");
        referentiel.LierEnfant("nounou", "Léa");
        var config = RendreConfig(api);

        // When (édition) — clic crayon de « nounou » : la modal s'ouvre PRÉ-REMPLIE (libellé + adresse + enfant lié).
        this.SurDispatcher(() => LigneParLibelle(config, "nounou").QuerySelector("[data-testid='crayon-activite']")!.Click());
        config.WaitForElement("[data-testid='dialog-activite']", TimeSpan.FromSeconds(10));
        Assert.Equal("nounou", config.Find("[data-testid='champ-libelle-activite']").GetAttribute("value"));
        Assert.Equal("3 impasse du Bosquet", config.Find("[data-testid='champ-adresse-activite']").GetAttribute("value"));
        Assert.Equal("Léa", config.Find("[data-testid='activite-enfants-lies-modal']").TextContent.Trim());

        // … je renomme + change l'adresse puis j'enregistre (POST réel /api/canal/editer-activite) : modal
        // fermée, table relue, même id stable (« nounou »), adresse mise à jour.
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-activite']").Change("nounou du quartier"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-adresse-activite']").Change("5 rue Neuve"));
        this.SurDispatcher(() => config.Find("#form-activite").Submit());
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));
                Assert.Contains(config.FindAll("[data-testid='activite-foyer']"), li => Libelle(li) == "nounou du quartier");
                Assert.DoesNotContain(config.FindAll("[data-testid='activite-foyer']"), li => Libelle(li) == "nounou");
            },
            TimeSpan.FromSeconds(10));
        // … l'adresse est bien persistée en store (id stable inchangé « nounou »).
        Assert.Equal("5 rue Neuve", api.Services.GetRequiredService<IEnumerationActivites>()
            .EnumererActivites().Single(a => a.Id == "nounou").Adresse);

        // When (création) — « Ajouter une activité » ouvre la MÊME modal VIDE → j'y crée « piscine » (id neuf).
        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-activite']").Click());
        config.WaitForElement("[data-testid='dialog-activite']", TimeSpan.FromSeconds(10));
        Assert.Equal("", config.Find("[data-testid='champ-libelle-activite']").GetAttribute("value"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-activite']").Change("piscine"));
        this.SurDispatcher(() => config.Find("#form-activite").Submit());
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));
                Assert.Contains(config.FindAll("[data-testid='activite-foyer']"), li => Libelle(li) == "piscine");
            },
            TimeSpan.FromSeconds(10));
    }
}
