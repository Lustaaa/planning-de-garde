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
/// Sprint 33 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : dans la modal d'édition d'un acteur
/// (Parent, écran réellement câblé à l'API distante réelle, store réel, DI réelle), un champ « adresse de
/// résidence » <b>pré-rempli avec la valeur courante</b> est éditable. Le modifier puis « Enregistrer »
/// porte l'adresse via la commande d'édition (Sc.1) sur le canal HTTP réel ; en succès la modal se ferme
/// et la table en lecture affiche la nouvelle adresse. Une adresse laissée vide est acceptée sans bloquer
/// l'enregistrement des autres champs (le nom édité est bien appliqué).
/// </summary>
public sealed class FrontWasmConfigActeursAdresseModalTableTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']").Single(li => NomLigne(li) == nom);

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
    public void Le_champ_adresse_est_prerempli_editable_et_l_Enregistrer_porte_l_adresse_que_la_table_affiche()
    {
        // Given — parent-a (Alice) porte déjà une adresse dans le store réel (via le port d'écriture).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().ChangerAdresse("parent-a", "12 rue des Lilas, Lyon");
        var config = RendreConfig(api);

        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // Then (modal rendue) — le champ adresse est pré-rempli avec la valeur COURANTE.
        var champ = config.Find("[data-testid='champ-adresse']");
        Assert.Equal("12 rue des Lilas, Lyon", champ.GetAttribute("value"));

        // When — je modifie l'adresse puis j'enregistre (POST réel /api/canal/editer-acteur porteur de l'adresse).
        this.SurDispatcher(() => config.Find("[data-testid='champ-adresse']").Change("34 avenue Neuve, Paris"));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal se ferme et la table en lecture affiche la NOUVELLE adresse sur la ligne d'Alice.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var adresse = LigneDe(config, "Alice").QuerySelector("[data-testid='acteur-adresse']");
                Assert.NotNull(adresse);
                Assert.Contains("34 avenue Neuve, Paris", adresse!.TextContent);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Une_adresse_laissee_vide_est_acceptee_sans_bloquer_l_enregistrement_des_autres_champs()
    {
        // Given — parent-a porte une adresse ; on va la vider tout en renommant l'acteur.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().ChangerAdresse("parent-a", "12 rue des Lilas, Lyon");
        var config = RendreConfig(api);

        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // When — je vide l'adresse ET je renomme Alice → Alicia, puis j'enregistre.
        this.SurDispatcher(() => config.Find("[data-testid='champ-adresse']").Change(""));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Alicia"));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — l'enregistrement aboutit (adresse vide acceptée) : la modal se ferme, le nom est appliqué
        // (Alicia dans la table) et plus aucune adresse n'est affichée sur sa ligne.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var alicia = LigneDe(config, "Alicia");
                Assert.Empty(alicia.QuerySelectorAll("[data-testid='acteur-adresse']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
