using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 25 — Sc.3 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : la page « /connexion » reste LIBREMENT
/// accessible (elle n'est PAS une route protégée — aucune garde d'admission, donc aucune boucle de
/// redirection), et la landing « / » (<see cref="Home"/>) redirige vers « /connexion » (préservation
/// s24). La page de connexion est câblée à l'API distante réelle (<see cref="ApiDistanteFactory"/>,
/// store réel, DI réelle, <see cref="NavigationManager"/> réel), rendue SANS session ouverte.
/// Preuve sur câblage réel, jamais une doublure.
/// </summary>
public sealed class FrontWasmConnexionLibrementAccessibleRuntimeTests : TestContext
{
    [Fact]
    public void Should_afficher_la_page_de_connexion_sans_rediriger_When_rendue_sans_session()
    {
        // Given — la page de connexion réellement câblée à l'API distante réelle, SANS session ouverte
        // (aucun compte connecté). La position de départ du NavigationManager réel est « / ».
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        var session = new SessionPlanning();
        Services.AddSingleton(session);
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.False(session.EstConnecte);

        // When — je navigue vers « /connexion » (rendu de la page).
        var connexion = RenderComponent<Connexion>();

        // Then — la page de connexion s'affiche (le formulaire de login est présent)…
        Assert.NotNull(connexion.Find("[data-testid='bouton-se-connecter']"));
        Assert.NotNull(connexion.Find("[data-testid='champ-email-connexion']"));

        // … et AUCUNE redirection n'est déclenchée (pas de boucle) : l'URI ne bascule pas vers le planning
        // ni ne quitte la page — la page de connexion n'est pas protégée.
        Assert.DoesNotContain("planning", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_rediriger_la_landing_vers_connexion_When_l_app_est_ouverte_sans_session()
    {
        // Given — un utilisateur non connecté ouvre l'app (route « / » = Home).
        Services.AddSingleton(new SessionPlanning());
        var nav = Services.GetRequiredService<NavigationManager>();

        // When — la landing est rendue.
        RenderComponent<Home>();

        // Then — « / » redirige vers « /connexion » (landing s24 préservée), sans boucle.
        Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
