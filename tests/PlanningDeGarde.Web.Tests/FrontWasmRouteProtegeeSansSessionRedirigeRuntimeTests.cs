using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 25 — Sc.1 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : protection d'accès aux routes.
/// La <b>vraie</b> route protégée (<see cref="PlanningPartage"/>) est câblée à l'API distante réelle
/// (<see cref="ApiDistanteFactory"/>, store réel, DI réelle, horloge réelle) MAIS sans session ouverte
/// (<see cref="SessionPlanning.EstConnecte"/> faux — aucun compte connecté). Rendue, elle redirige vers
/// « /connexion » (via le <see cref="NavigationManager"/> réel) et NE rend AUCUN contenu de la route
/// protégée (pas de flash de grille agenda). Preuve sur câblage réel, jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmRouteProtegeeSansSessionRedirigeRuntimeTests : TestContext
{
    [Fact]
    public void Should_rediriger_vers_connexion_et_ne_rien_rendre_de_la_route_protegee_When_aucune_session_ouverte()
    {
        // Given — la route protégée réellement câblée à l'API distante réelle (store réel), SANS session
        // ouverte : aucun compte n'est connecté (EstConnecte = faux).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning();
        Services.AddSingleton(session);
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(GrilleRuntimeHarness.Lundi_29_06_2026));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.False(session.EstConnecte);

        // When — je navigue directement vers la route protégée (rendu de la page).
        var planning = RenderComponent<PlanningPartage>();

        // Then — je suis redirigé vers « /connexion »…
        planning.WaitForAssertion(
            () => Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(10));

        // … et aucun contenu de la route protégée n'est rendu (pas de flash de grille agenda).
        Assert.Empty(planning.FindAll("[data-testid='grille-agenda']"));
    }
}
