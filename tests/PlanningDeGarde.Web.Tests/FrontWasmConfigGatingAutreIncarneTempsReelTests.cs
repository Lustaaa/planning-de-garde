using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME / G3 du Sc.6 (🖥️ IHM, <c>@erreur</c>, driver — durcissement du gating
/// config, D1) : en incarnant un acteur de type <b>« Autre »</b>, l'écran de configuration <b>réellement
/// câblé</b> (<see cref="ConfigurationFoyer"/>, API distante réelle <see cref="ApiDistanteFactory"/>, store
/// réel, contexte rôle réel <see cref="SessionPlanning"/>) ne propose <b>AUCUNE</b> écriture : ni ajout
/// d'acteur, ni édition d'acteur, ni édition du cycle de fond, ni bouton de suppression. Le gating règle 9
/// est lu sur l'identité <b>effective</b> (<see cref="SessionPlanning.EstParent"/> dérivé de l'incarné).
///
/// <para>Contrôle positif (anti faux-vert) : sous l'identité réelle (Parent, aucune incarnation), TOUTES
/// ces écritures restent proposées — sinon l'absence sous incarnation serait un faux vert (formulaires
/// cassés pour tous). Un bUnit forçant l'interactivité ou doublant le rôle ne prouverait pas le gating
/// réel sur l'identité effective.</para>
///
/// <para>NB : le bouton supprimer suit déjà l'identité effective (gaté `@if EstParent` depuis s13) →
/// early-green partiel ; le driver réel est l'extension du garde à ajout / édition / cycle.</para>
/// </summary>
public sealed class FrontWasmConfigGatingAutreIncarneTempsReelTests : TestContext
{
    // Affordances d'écriture (proxys des formulaires gatés) réparties par onglet depuis la refonte s20
    // (Sc.2) : sous l'onglet « Acteurs » — champ-nom (édition), champ-nom-ajout (ajout), bouton-supprimer
    // (suppression) ; sous l'onglet « Période de garde » — champ-nombre-semaines (cycle). Le gating règle 9
    // (identité effective) est préservé DANS chaque onglet (Sc.7, s20).
    private static readonly string[] EcrituresOngletActeurs =
    {
        "[data-testid='champ-nom']",
        "[data-testid='champ-nom-ajout']",
        "[data-testid='bouton-supprimer']",
    };

    private const string EcritureOngletPeriodeGarde = "[data-testid='champ-nombre-semaines']";

    [Fact]
    public void Should_MasquerToutesLesEcrituresDeLaConfiguration_When_OnIncarneUnActeurDeTypeAutre()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store + énumération
        // réels), avec un contexte de rôle réel. On part de l'identité réelle (Parent) : contrôle positif.
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning();
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Contrôle positif — sous l'identité réelle (Parent), TOUTES les écritures config sont proposées
        // (les formulaires ne sont pas cassés pour tous), sur chacun de leurs onglets respectifs.
        foreach (var ecriture in EcrituresOngletActeurs)
            Assert.NotEmpty(config.FindAll(ecriture)); // onglet « Acteurs », actif par défaut
        Assert.NotEmpty(config.FindAll(EcritureOngletPeriodeGarde)); // section « Cycle de fond », visible sur la même page

        // When — le configurateur incarne Nina la nounou (type Autre) ; l'écran est re-rendu.
        session.ActeursIncarnables = new List<IdentiteActeur>
        {
            new("nounou", "Nina la nounou", TypeActeur.Autre),
        };
        session.Incarner("nounou");
        Assert.False(session.EstParent); // garde-fou : l'incarnation d'un Autre retire bien le droit d'écrire
        config.Render();

        // Then — en incarnant un Autre, AUCUNE écriture config n'est proposée sur AUCUN onglet (Sc.7, s20) :
        // le cycle est masqué sous « Période de garde » (onglet actif), et l'ajout/édition/suppression sous
        // « Acteurs ».
        Assert.Empty(config.FindAll(EcritureOngletPeriodeGarde)); // section « Cycle de fond »
        foreach (var ecriture in EcrituresOngletActeurs)
            Assert.Empty(config.FindAll(ecriture));

        // … la liste des acteurs reste visible (consultation seule préservée) : le durcissement masque les
        // ÉCRITURES, pas la lecture.
        Assert.NotEmpty(config.FindAll("[data-testid='acteur-foyer']"));
    }

    /// <summary>
    /// Phase IHM finale (cohérence inter-écrans) — sous incarnation, l'écran de configuration affiche le
    /// <b>bandeau « Vous incarnez X »</b> (en miroir de la grille) et l'<b>affordance de retour</b> permet de
    /// revenir à l'identité réelle <b>depuis cet écran</b> : les écritures config redeviennent alors visibles.
    /// Prouvé sur l'écran réellement câblé (DI réelle, état d'incarnation partagé de session).
    /// </summary>
    [Fact]
    public void Should_AfficherLeBandeauEtRestaurerLesEcritures_When_OnIncarneUnAutrePuisRevientDepuisLaConfiguration()
    {
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning
        {
            ActeursIncarnables = new List<IdentiteActeur> { new("nounou", "Nina la nounou", TypeActeur.Autre) },
        };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Sous identité réelle : aucun bandeau.
        Assert.Empty(config.FindAll("[data-testid='bandeau-incarnation']"));

        // En incarnant Nina (Autre) : le bandeau « Vous incarnez Nina la nounou » s'affiche, écritures masquées.
        session.Incarner("nounou");
        config.Render();
        Assert.Contains("Vous incarnez Nina la nounou",
            config.Find("[data-testid='bandeau-incarnation']").TextContent);
        Assert.Empty(config.FindAll("[data-testid='champ-nom']"));

        // En revenant à l'identité réelle DEPUIS la configuration : bandeau retiré, écritures restaurées sur
        // chaque onglet (édition/ajout sous « Acteurs » actif, cycle sous « Période de garde », Sc.2/Sc.7 s20).
        this.SurDispatcher(() => config.Find("[data-testid='revenir-identite-reelle']").Click());
        Assert.Empty(config.FindAll("[data-testid='bandeau-incarnation']"));
        Assert.NotEmpty(config.FindAll("[data-testid='champ-nom']"));
        Assert.NotEmpty(config.FindAll("[data-testid='champ-nom-ajout']"));
        Assert.NotEmpty(config.FindAll("[data-testid='champ-nombre-semaines']"));
    }
}
