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
/// Sprint 21 — Sc.8 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis l'onglet « Acteurs » de l'écran
/// de configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel), on <b>affecte un rôle à un acteur via un sélecteur borné
/// au référentiel</b>. Le sélecteur de rôle d'un acteur propose <b>exactement</b> les rôles du référentiel
/// (relus depuis le store, GET /api/foyer/roles) plus une option « sans rôle » — <b>aucun libellé en dur</b>.
/// Affecter « Nounou » fait porter ce rôle à l'acteur (persisté, POST /api/canal/affecter-role, relu depuis
/// le store) ; un acteur auquel aucun rôle n'a été affecté s'affiche « sans rôle » (neutre).
/// </summary>
public sealed class FrontWasmConfigOngletActeursAffecterRoleRuntimeTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    [Fact]
    public void Should_affecter_un_role_a_un_acteur_via_un_selecteur_borne_au_referentiel()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel), identité
        // Parent. On sème DEUX rôles dans le référentiel réel (Nounou, Grand-parent) via le port d'écriture
        // réel — le sélecteur devra proposer exactement ces deux libellés (plus « sans rôle »), jamais un
        // rôle en dur.
        using var api = new ApiDistanteFactory();
        var editeurRoles = api.Services.GetRequiredService<IEditeurReferentielRoles>();
        var idNounou = "role-nounou";
        editeurRoles.Creer(idNounou, "Nounou");
        editeurRoles.Creer("role-grand-parent", "Grand-parent");

        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Then (sélecteur borné) — refonte s32 : le sélecteur de rôle vit dans la MODAL ouverte au crayon.
        // Ouvert sur « Alice » (parent-a), il propose exactement les rôles du référentiel plus « sans rôle ».
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        var options = config.Find("[data-testid='selecteur-role-acteur']")
            .QuerySelectorAll("option")
            .Select(o => o.TextContent.Trim())
            .ToList();
        Assert.Contains("Nounou", options);
        Assert.Contains("Grand-parent", options);
        Assert.Contains("sans rôle", options);
        Assert.Equal(3, options.Count);

        // Then (acteur sans rôle = neutre) — avant toute affectation, la ligne d'Alice affiche « sans rôle ».
        Assert.Equal("sans rôle", ConfigActeursModalHarness.LigneParNom(config, "Alice")
            .QuerySelector("[data-testid='role-acteur-courant']")!.TextContent.Trim());

        // When (affectation) — j'affecte « Nounou » à Alice via le sélecteur de la modal (POST
        // /api/canal/affecter-role, la valeur émise est l'id de rôle du référentiel, jamais un libellé en dur).
        this.SurDispatcher(() => config.Find("[data-testid='selecteur-role-acteur']").Change(idNounou));

        // Then (affectation persistée) — sans rechargement, la ligne d'Alice relue depuis le store affiche
        // « Nounou », tandis qu'un acteur sans rôle (Bruno) reste « sans rôle » (neutre).
        config.WaitForAssertion(
            () =>
            {
                var alice = config.FindAll("[data-testid='acteur-foyer']").Single(li => NomLigne(li) == "Alice");
                Assert.Equal("Nounou", alice.QuerySelector("[data-testid='role-acteur-courant']")!.TextContent.Trim());
                var bruno = config.FindAll("[data-testid='acteur-foyer']").Single(li => NomLigne(li) == "Bruno");
                Assert.Equal("sans rôle", bruno.QuerySelector("[data-testid='role-acteur-courant']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));
    }
}
