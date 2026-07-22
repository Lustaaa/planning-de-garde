using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.Components.Shared;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.1 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : la session survit au rechargement (F5).
/// On reproduit fidèlement un F5 : le client <b>redémarre</b> (le <b>vrai</b> composant racine
/// <see cref="App"/> et son Router sont rendus, câblés à l'<b>API distante réelle</b>
/// <see cref="ApiDistanteFactory"/> — store réel, DI réelle, SignalR réel), la session en mémoire repart
/// <b>vierge</b> (mémoire vidée par le rechargement), MAIS un jeton de session a été <b>persisté au login</b>
/// dans un stockage durable qui survit au F5 (le port <see cref="IPersistanceSession"/> — seul doublé, car un
/// navigateur/localStorage réel n'existe pas en test ; le mécanisme réel est prouvé par la garde d'asset
/// <see cref="AmorceSessionAssetTests"/>). Au démarrage, l'app restaure la session depuis le jeton : on
/// atterrit sur « /planning » <b>connecté</b>, la route protégée rend sa grille (28 cases-jour), et l'on
/// n'est <b>PAS</b> re-redirigé vers « /connexion ». La borne anti-cliquet R30 reste tenue : la session
/// reste en mémoire (aucune régression d'état), seul son amorçage d'identité est rejoué depuis le jeton.
/// </summary>
public sealed class FrontWasmSessionSurvitAuRechargementF5RuntimeTests : TestContext
{
    [Fact]
    public void Should_rester_connecte_sur_planning_apres_F5_When_un_jeton_de_session_est_persiste()
    {
        // Given — l'API distante réelle + un jeton persisté au login (stockage durable qui survit au F5),
        // et une session en mémoire VIERGE (le rechargement a vidé la mémoire : NON connectée).
        using var api = new ApiDistanteFactory();
        var persistance = new FakePersistanceSession(
            new SessionPersistee("configurateur", "Alice", TypeActeur.Parent));
        Services.AddSingleton<IPersistanceSession>(persistance);
        Services.AddSingleton<RestaurateurSession>();
        var session = new SessionPlanning();
        Services.AddSingleton(session);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(GrilleRuntimeHarness.Lundi_29_06_2026));
        Services.AddSingleton<IPreferencesTheme>(new SpyPreferencesTheme());
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("planning"); // F5 alors qu'on était sur /planning
        Assert.False(session.EstConnecte);

        // When — l'app redémarre : le vrai composant racine est rendu (Router résout /planning).
        var app = RenderComponent<App>();

        // Then — on reste sur /planning CONNECTÉ : la route protégée rend sa grille (28 cases-jour projetées
        // depuis le store réel), sans redirection vers /connexion.
        app.WaitForState(
            () => app.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        Assert.NotNull(app.Find("[data-testid='grille-agenda']"));
        Assert.False(nav.Uri.EndsWith("connexion", StringComparison.OrdinalIgnoreCase));

        // And — la session en mémoire est bien ré-ouverte au démarrage (borne R30 : aucune régression d'état,
        // la session reste en mémoire, seul son amorçage d'identité est rejoué depuis le jeton persisté).
        Assert.True(session.EstConnecte);
        Assert.Equal("Alice", session.CompteConnecteNom);
    }

    /// <summary>Double À LA MAIN du seul port <see cref="IPersistanceSession"/> : mime le stockage durable
    /// client (localStorage) qui survit au F5, amorcé avec le jeton qu'un login aurait persisté.</summary>
    private sealed class FakePersistanceSession : IPersistanceSession
    {
        private SessionPersistee? _stocke;

        public FakePersistanceSession(SessionPersistee? amorce = null) => _stocke = amorce;

        public ValueTask PersisterAsync(SessionPersistee jeton)
        {
            _stocke = jeton;
            return ValueTask.CompletedTask;
        }

        public ValueTask<SessionPersistee?> LireAsync() => ValueTask.FromResult(_stocke);

        public ValueTask PurgerAsync()
        {
            _stocke = null;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>Double du port de thème (le layout rend la bascule de thème) : neutre pour ce scénario.</summary>
    private sealed class SpyPreferencesTheme : IPreferencesTheme
    {
        public ValueTask<string> ThemeCourantAsync() => ValueTask.FromResult("clair");

        public ValueTask DefinirAsync(string theme) => ValueTask.CompletedTask;
    }
}
