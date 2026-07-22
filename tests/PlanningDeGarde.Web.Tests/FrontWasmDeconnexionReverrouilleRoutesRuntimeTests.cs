using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Shared.Layout;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 25 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : la déconnexion re-verrouille
/// immédiatement les routes protégées. Un compte connecté consulte la route protégée
/// (<see cref="PlanningPartage"/>, câblée à l'API distante réelle <see cref="ApiDistanteFactory"/>,
/// store réel, DI réelle) : la grille s'affiche. Je me déconnecte via le <see cref="MenuUtilisateur"/>
/// (s24) câblé à la MÊME session scoped : la session est détruite (EstConnecte = faux, aucune identité
/// résiduelle — repli sur l'identité réelle). Un accès ultérieur à la route protégée (re-rendu) redirige
/// de nouveau vers « /connexion » et ne rend aucun contenu. Preuve sur câblage réel, jamais une doublure.
/// </summary>
public sealed class FrontWasmDeconnexionReverrouilleRoutesRuntimeTests : TestContext
{
    [Fact]
    public void Should_reverrouiller_la_route_protegee_When_je_me_deconnecte_depuis_le_menu_utilisateur()
    {
        // Given — la route protégée et le menu utilisateur câblés à la MÊME session scoped (connectée) +
        // API distante réelle (store réel). La grille s'affiche (28 cases-jour projetées).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
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

        var planning = RenderComponent<PlanningPartage>();
        var menu = RenderComponent<MenuUtilisateur>();
        planning.WaitForState(
            () => planning.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        Assert.True(session.EstConnecte);

        // When — je me déconnecte depuis le menu utilisateur (logout s23, MenuUtilisateur s24).
        this.SurDispatcher(() => menu.Find("[data-testid='menu-se-deconnecter']").Click());

        // Then — la session est détruite : EstConnecte = faux, aucune identité résiduelle (repli sur
        // l'identité réelle, pas d'incarnation orpheline).
        Assert.False(session.EstConnecte);
        Assert.False(session.IncarnationActive);

        // And — un accès ULTÉRIEUR à la route protégée redirige de nouveau vers « /connexion » et ne rend
        // aucun contenu (re-verrouillage immédiat). On re-rend la route protégée avec la même session détruite.
        var reacces = RenderComponent<PlanningPartage>();
        reacces.WaitForAssertion(
            () => Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(10));
        Assert.Empty(reacces.FindAll("[data-testid='grille-agenda']"));
    }
}
