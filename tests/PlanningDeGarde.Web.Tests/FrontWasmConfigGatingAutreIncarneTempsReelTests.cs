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
    // data-testid des affordances d'écriture (proxys des formulaires gatés) : champ-nom (édition d'acteur),
    // champ-nom-ajout (ajout), champ-nombre-semaines (cycle), bouton-supprimer (suppression).
    private static readonly string[] EcrituresConfig =
    {
        "[data-testid='champ-nom']",
        "[data-testid='champ-nom-ajout']",
        "[data-testid='champ-nombre-semaines']",
        "[data-testid='bouton-supprimer']",
    };

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
        // (les formulaires ne sont pas cassés pour tous).
        foreach (var ecriture in EcrituresConfig)
            Assert.NotEmpty(config.FindAll(ecriture));

        // When — le configurateur incarne Nina la nounou (type Autre) ; l'écran est re-rendu.
        session.ActeursIncarnables = new List<IdentiteActeur>
        {
            new("nounou", "Nina la nounou", TypeActeur.Autre),
        };
        session.Incarner("nounou");
        Assert.False(session.EstParent); // garde-fou : l'incarnation d'un Autre retire bien le droit d'écrire
        config.Render();

        // Then — en incarnant un Autre, AUCUNE écriture config n'est proposée (ajout, édition, cycle, supprimer).
        foreach (var ecriture in EcrituresConfig)
            Assert.Empty(config.FindAll(ecriture));

        // … la liste des acteurs reste visible (consultation seule préservée) : le durcissement masque les
        // ÉCRITURES, pas la lecture.
        Assert.NotEmpty(config.FindAll("[data-testid='acteur-foyer']"));
    }
}
