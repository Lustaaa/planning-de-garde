using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 25 — Sc.2 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : accès rétabli sous session.
/// La <b>vraie</b> route protégée (<see cref="PlanningPartage"/>) est câblée à l'API distante réelle
/// (<see cref="ApiDistanteFactory"/>, store réel, DI réelle, horloge réelle) AVEC une session ouverte
/// (compte Actif connecté, <see cref="SessionPlanning.EstConnecte"/> vrai). Rendue, elle s'affiche
/// normalement (la grille agenda est projetée depuis le store réel — 28 cases-jour) et ne redirige PAS
/// vers « /connexion ». Une re-projection ultérieure (navigation entre routes / semaine) ne redéclenche
/// aucune redirection (la session reste ouverte). Preuve sur câblage réel, jamais une doublure.
/// </summary>
public sealed class FrontWasmRouteProtegeeAvecSessionAccesRetabliRuntimeTests : TestContext
{
    [Fact]
    public void Should_afficher_la_route_protegee_sans_redirection_When_une_session_est_ouverte()
    {
        // Given — la route protégée réellement câblée à l'API distante réelle (store réel), AVEC une session
        // ouverte (compte Actif connecté, EstConnecte = vrai).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = GrilleRuntimeHarness.SessionConnectee();
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
        Assert.True(session.EstConnecte);

        // When — je navigue vers la route protégée (rendu de la page).
        var planning = RenderComponent<PlanningPartage>();

        // Then — la route s'affiche normalement : la grille agenda est projetée (28 cases-jour, 4 semaines
        // glissantes) depuis le store réel, sans redirection vers /connexion.
        planning.WaitForState(
            () => planning.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        Assert.NotNull(planning.Find("[data-testid='grille-agenda']"));
        Assert.False(nav.Uri.EndsWith("connexion", StringComparison.OrdinalIgnoreCase));

        // And — une re-projection (navigation d'une semaine) ne redéclenche aucune redirection : la session
        // reste ouverte, la grille reste affichée.
        this.SurDispatcher(() => planning.Find("[data-testid='nav-semaine-suivante']").Click());
        planning.WaitForState(
            () => planning.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        Assert.NotNull(planning.Find("[data-testid='grille-agenda']"));
        Assert.False(nav.Uri.EndsWith("connexion", StringComparison.OrdinalIgnoreCase));
    }
}
