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
/// Sprint 25 — Sc.16 (🖥️ @ihm @preuve-doublure — acceptation de NIVEAU RUNTIME) : boutons OAuth sur /connexion.
/// La <b>vraie</b> page <see cref="Connexion"/> (landing s24) est câblée à l'API distante réelle
/// (<see cref="ApiDistanteFactory"/>, store réel, DI réelle, hub SignalR réel redirigé sur le TestServer)
/// — le MÊME câblage runtime que les scénarios Sc.1→Sc.4. Rendue, elle présente, À CÔTÉ du login local,
/// des boutons « Se connecter avec Google / Microsoft / Apple ». Un clic DÉCLENCHE le flux OAuth :
/// observable au runtime comme une navigation vers l'endpoint de démarrage OAuth du provider
/// (l'authorize réel du provider — redirection secrets/callbacks — est vérifié MANUELLEMENT au G3 ;
/// ici le câblage court-circuite la sortie réelle vers Google/Microsoft/Apple). Preuve sur câblage réel,
/// jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmConnexionBoutonsOAuthRuntimeTests : TestContext
{
    private void CablerConnexionReelle(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning()); // page de connexion : pas de session ouverte
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(GrilleRuntimeHarness.Lundi_29_06_2026));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
    }

    [Fact]
    public void Should_presenter_les_trois_boutons_OAuth_a_cote_du_login_local_When_la_page_connexion_s_affiche()
    {
        // Given — la vraie page /connexion réellement câblée à l'API distante réelle.
        using var api = new ApiDistanteFactory();
        CablerConnexionReelle(api);

        // When — la page de connexion s'affiche.
        var connexion = RenderComponent<Connexion>();

        // Then — le login local (email + bouton) est présent…
        Assert.NotNull(connexion.Find("[data-testid='champ-email-connexion']"));
        Assert.NotNull(connexion.Find("[data-testid='bouton-se-connecter']"));

        // … ET, à côté, les trois boutons providers OAuth.
        Assert.NotNull(connexion.Find("[data-testid='bouton-oauth-google']"));
        Assert.NotNull(connexion.Find("[data-testid='bouton-oauth-microsoft']"));
        Assert.NotNull(connexion.Find("[data-testid='bouton-oauth-apple']"));
    }

    [Fact]
    public void Should_declencher_le_flux_OAuth_du_provider_When_on_clique_le_bouton()
    {
        // Given — la vraie page /connexion réellement câblée.
        using var api = new ApiDistanteFactory();
        CablerConnexionReelle(api);
        var nav = Services.GetRequiredService<NavigationManager>();
        var connexion = RenderComponent<Connexion>();

        // When — je clique « Se connecter avec Google ».
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-oauth-google']").Click());

        // Then — le flux OAuth du provider est déclenché : navigation vers l'endpoint de démarrage OAuth
        // « google » (l'authorize réel du provider est vérifié manuellement au G3 — ici court-circuité).
        connexion.WaitForAssertion(
            () =>
            {
                Assert.Contains("oauth", nav.Uri, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("google", nav.Uri, StringComparison.OrdinalIgnoreCase);
            },
            TimeSpan.FromSeconds(10));
    }
}
