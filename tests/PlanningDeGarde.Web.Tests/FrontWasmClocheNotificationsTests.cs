using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 47 — Sc.3 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la CLOCHE de notifications en en-tête du
/// planning réellement câblé (store réel, projection réelle, canal réel). Un Parent connecté qui a des
/// changements le concernant voit une icône cloche + un BADGE compteur de non-lus ; cliquer la cloche déroule
/// un PANNEAU listant les changements récents (chrono, lu/non-lu) ; marquer lu décroît le compteur ; Échap
/// ferme le panneau (port document s33, DOUBLÉ) ; un Invité ne voit PAS la cloche (Parent-gated).
///
/// Anti « vert qui ment » : la notification provient d'une VRAIE écriture (délégation) transitée par le canal
/// requête/réponse, consignée au journal réel, relue par le canal de lecture réel — jamais une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmClocheNotificationsTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026, ISO 27 → parent-b

    /// <summary>Double à la main du port d'écoute Échap (spy) : capte le callback d'attache et rejoue Échap document.</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;
        public int Attachements { get; private set; }

        public ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
        {
            Attachements++;
            _onEchap = onEchap;
            return ValueTask.FromResult<IAsyncDisposable>(new Abonnement(this));
        }

        public Task DeclencherEchapDocument() => _onEchap?.Invoke() ?? Task.CompletedTask;

        private sealed class Abonnement : IAsyncDisposable
        {
            private readonly EspionEchap _espion;
            public Abonnement(EspionEchap espion) => _espion = espion;
            public ValueTask DisposeAsync() { _espion._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    private IRenderedComponent<PlanningPartage> Rendre(ApiDistanteFactory api, SessionPlanning session, IEcouteurEchapModal? echap = null)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        if (echap is not null)
            Services.AddSingleton(echap);

        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        return grille;
    }

    /// <summary>Sème une VRAIE délégation (canal réel) dont <paramref name="versActeurId"/> est le RECEVANT :
    /// 29/06 résout parent-b (fond ISO 27), la déléguer à parent-a consigne au journal un changement concernant
    /// parent-a (recevant). Rempart anti vert-qui-ment : le journal est alimenté par l'écriture réelle.</summary>
    private static async Task SemerDelegationVers(ApiDistanteFactory api, string versActeurId)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        var reponse = await client.PostAsJsonAsync(
            "api/canal/deleguer-recuperation",
            new DeleguerRecuperationRequete(new DateOnly(2026, 6, 29), "Léa", versActeurId));
        reponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Cloche_affiche_badge_non_lus_panneau_et_marquer_lu_decroit_le_compteur()
    {
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        // Given — un changement (délégation) concernant parent-a (le recevant), non lu.
        await SemerDelegationVers(api, "parent-a");

        // Parent connecté = parent-a (concerné par la notification).
        var grille = Rendre(api, SessionComme("parent-a", "Alice"));

        // Then — la cloche affiche un badge = 1 (un changement non lu me concernant).
        grille.WaitForAssertion(
            () => Assert.Equal("1", grille.Find("[data-testid='cloche-badge']").TextContent.Trim()),
            TimeSpan.FromSeconds(10));

        // When — clic sur la cloche → panneau déroulant listant la notification (non-lu, type délégation).
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(
            () =>
            {
                var notifs = grille.Find("[data-testid='cloche-panneau']").QuerySelectorAll("[data-testid='cloche-notif']");
                Assert.Single(notifs);
                Assert.Equal("0", notifs[0].GetAttribute("data-lu"));            // non-lu
                Assert.Equal("delegation", notifs[0].GetAttribute("data-type")); // c'est bien la délégation
            },
            TimeSpan.FromSeconds(10));

        // When — marquer la notification comme lue.
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-marquer-lu']").Click());

        // Then — le compteur passe à 0 (badge retiré) : l'état lu est persisté par utilisateur.
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='cloche-badge']")),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Echap_ferme_le_panneau_de_la_cloche()
    {
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        await SemerDelegationVers(api, "parent-a");

        var espion = new EspionEchap();
        var grille = Rendre(api, SessionComme("parent-a", "Alice"), espion);

        // Ouvre le panneau → l'écouteur Échap document est ATTACHÉ (capture s33).
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='cloche-panneau']"));
                Assert.Equal(1, espion.Attachements);
            },
            TimeSpan.FromSeconds(10));

        // When — Échap document (rejoué via le callback capté).
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — le panneau se ferme.
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='cloche-panneau']")),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Un_invite_ne_voit_pas_la_cloche()
    {
        using var api = new ApiDistanteFactory();
        var session = SessionComme("parent-a", "Alice");
        session.Role = RoleAuteur.Invite; // consultation seule

        var grille = Rendre(api, session);

        // Then — Parent-gated : l'Invité ne voit pas la cloche.
        Assert.Empty(grille.FindAll("[data-testid='cloche-bouton']"));
    }
}
