using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.Components.Shared;
using PlanningDeGarde.Web.Components.Shared.Layout;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.3 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le logout purge le persisté, si bien qu'un
/// F5 après logout ramène sur « /connexion ». Deux actes sur câblage réel :
/// <list type="bullet">
///   <item><b>logout purge</b> — le <b>vrai</b> menu utilisateur (<see cref="MenuUtilisateur"/>, session
///     connectée, API distante réelle) : « Se déconnecter » détruit la session (logout s23) ET purge le jeton
///     persisté via le port <see cref="IPersistanceSession"/> (espion — l'effet localStorage réel est couvert
///     par la garde d'asset <see cref="AmorceSessionAssetTests"/>) ;</item>
///   <item><b>F5 après logout</b> — le <b>vrai</b> composant racine <see cref="App"/> (Router + API distante
///     réelle) redémarre sur « /planning » avec le stockage <b>purgé</b> et une session en mémoire vierge :
///     rien à restaurer → redirection vers « /connexion », aucune session fantôme (le logout reste effectif
///     au rechargement).</item>
/// </list>
/// Borne R30 + logout s23 tenus : la session reste en mémoire, le logout en reste le seul maître.
/// </summary>
public sealed class FrontWasmLogoutPurgeSessionPersisteeRuntimeTests : TestContext
{
    [Fact]
    public void Should_purger_le_jeton_persiste_When_je_me_deconnecte_depuis_le_menu_utilisateur()
    {
        // Given — une session connectée avec un jeton persisté (comme après un login), et le vrai menu
        // utilisateur câblé à cette session + au port de persistance (espion).
        using var api = new ApiDistanteFactory();
        var persistance = new SpyPersistanceSession(
            new SessionPersistee("configurateur", "Alice", TypeActeur.Parent));
        Services.AddSingleton<IPersistanceSession>(persistance);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = GrilleRuntimeHarness.SessionConnectee();
        Services.AddSingleton(session);
        var menu = RenderComponent<MenuUtilisateur>();
        Assert.True(session.EstConnecte);

        // When — je me déconnecte (logout s23).
        this.SurDispatcher(() => menu.Find("[data-testid='menu-se-deconnecter']").Click());

        // Then — la session est détruite ET le jeton persisté est purgé (le stockage durable est vidé :
        // une relecture ultérieure ne rend plus rien).
        menu.WaitForAssertion(
            () =>
            {
                Assert.False(session.EstConnecte);
                Assert.True(persistance.NombrePurges >= 1);
            },
            TimeSpan.FromSeconds(10));
        Assert.Null(persistance.LireAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_rediriger_vers_connexion_When_F5_apres_logout_le_persiste_est_vide()
    {
        // Given — un F5 APRÈS logout : le stockage durable a été purgé (aucun jeton), la session en mémoire
        // repart vierge. L'app redémarre sur « /planning ».
        using var api = new ApiDistanteFactory();
        Services.AddSingleton<IPersistanceSession>(new SpyPersistanceSession()); // stockage vide (purgé)
        Services.AddSingleton<RestaurateurSession>();
        var session = new SessionPlanning(); // vierge : NON connectée
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
        nav.NavigateTo("planning");

        // When — l'app redémarre (rendu du vrai composant racine).
        var app = RenderComponent<App>();

        // Then — rien à restaurer (persisté vidé) → redirection vers /connexion, aucune session restaurée
        // (le logout reste effectif au rechargement).
        app.WaitForAssertion(
            () => Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(10));
        Assert.False(session.EstConnecte);
    }

    /// <summary>Double du port de thème (le layout rend la bascule de thème) : neutre pour ce scénario.</summary>
    private sealed class SpyPreferencesTheme : IPreferencesTheme
    {
        public System.Threading.Tasks.ValueTask<string> ThemeCourantAsync()
            => System.Threading.Tasks.ValueTask.FromResult("clair");

        public System.Threading.Tasks.ValueTask DefinirAsync(string theme)
            => System.Threading.Tasks.ValueTask.CompletedTask;
    }
}
