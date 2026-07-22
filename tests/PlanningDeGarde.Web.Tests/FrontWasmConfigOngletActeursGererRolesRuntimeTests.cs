using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 21 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis l'onglet « Acteurs » (actif par
/// défaut) de l'écran de configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante
/// réelle <see cref="ApiDistanteFactory"/>, store réel), on <b>gère le référentiel de rôles du foyer</b> :
/// créer « Nounou », le renommer, créer « Grand-parent », puis supprimer un rôle. Le référentiel commence
/// vide (store réel neuf). Les écritures transitent par le <b>canal HTTP réel</b>
/// (POST /api/foyer/roles, /renommer-role, /supprimer-role) et la liste des rôles est relue depuis le
/// store durable (GET /api/foyer/roles) — jamais une doublure de transport. Observé sur les observables
/// propres de l'écran : la liste des rôles reflète créations, renommage et suppression sans rechargement.
/// </summary>
public sealed class FrontWasmConfigOngletActeursGererRolesRuntimeTests : TestContext
{
    private static string? LibelleLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".role-libelle")?.TextContent.Trim();

    [Fact]
    public void Should_creer_renommer_et_supprimer_un_role_du_referentiel_depuis_l_onglet_Acteurs()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel neuf),
        // identité Parent. L'onglet « Acteurs » est actif par défaut ; le référentiel de rôles y est géré.
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        // Fusion des sections (hors-sprint) : plus d'onglets — la gestion des rôles est sur la page unique.

        // When (création) — refonte s33 Sc.8 : j'ouvre la modal d'ajout, saisis « Nounou » et enregistre
        // (canal d'écriture HTTP réel : POST /api/foyer/roles).
        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Nounou"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        // Then (création) — sans rechargement, la liste relue des rôles (GET /api/foyer/roles) contient « Nounou ».
        config.WaitForAssertion(
            () => Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Nounou"),
            TimeSpan.FromSeconds(10));

        // When (renommage) — j'ouvre la modal au crayon de « Nounou », renomme en « Assistante maternelle » et
        // enregistre (POST /api/canal/renommer-role, clé = identifiant stable du rôle, jamais le libellé).
        var idNounou = config.FindAll("[data-testid='role-foyer']").Single(li => LibelleLigne(li) == "Nounou")
            .GetAttribute("data-role-id");
        this.SurDispatcher(() =>
            config.FindAll("[data-testid='role-foyer']").Single(li => li.GetAttribute("data-role-id") == idNounou)
                .QuerySelector("[data-testid='crayon-role']")!.Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Assistante maternelle"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        // Then (renommage) — la liste relue reflète le nouveau libellé, sur le MÊME identifiant stable.
        config.WaitForAssertion(
            () => Assert.Contains(
                config.FindAll("[data-testid='role-foyer']"),
                li => LibelleLigne(li) == "Assistante maternelle" && li.GetAttribute("data-role-id") == idNounou),
            TimeSpan.FromSeconds(10));

        // When (2e création) — via la modal d'ajout, je crée « Grand-parent ».
        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Grand-parent"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());

        // Then (2e création) — la liste relue contient les deux rôles.
        config.WaitForAssertion(
            () => Assert.Contains(config.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Grand-parent"),
            TimeSpan.FromSeconds(10));

        // When (suppression) — j'ouvre la modal au crayon de « Grand-parent » et clique « Supprimer ce rôle »
        // (POST /api/canal/supprimer-role).
        this.SurDispatcher(() =>
            config.FindAll("[data-testid='role-foyer']").Single(li => LibelleLigne(li) == "Grand-parent")
                .QuerySelector("[data-testid='crayon-role']")!.Click());
        this.SurDispatcher(() => config.Find("[data-testid='bouton-supprimer-role']").Click());

        // Then (suppression) — sans rechargement, « Grand-parent » quitte la liste relue ; « Assistante
        // maternelle » reste. Les écritures ont bien abouti sur le store réel (relu via l'API distante).
        config.WaitForAssertion(
            () =>
            {
                var libelles = config.FindAll("[data-testid='role-foyer']").Select(LibelleLigne).ToList();
                Assert.DoesNotContain("Grand-parent", libelles);
                Assert.Contains("Assistante maternelle", libelles);
            },
            TimeSpan.FromSeconds(10));
    }
}
