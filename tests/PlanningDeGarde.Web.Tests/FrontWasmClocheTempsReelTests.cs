using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
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
/// Sprint 47 — Sc.4 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la CLOCHE d'un utilisateur destinataire CONVERGE
/// en TEMPS RÉEL quand un changement le concernant est écrit depuis un AUTRE écran — une nouvelle notification
/// apparaît + le compteur passe de N à N+1 — par REPROJECTION CLIENT depuis la DIFFUSION PORTEUSE DE PAYLOAD
/// (INotificateurChangement / EvenementChangementSnapshot), <b>0 GET sur push</b>.
///
/// <para><b>Preuve stricte du « 0 GET sur push »</b> : le client de cet écran a son <c>GET /api/notifications/…</c>
/// COUPÉ (échec de transport déterministe). Le chargement initial de la cloche échoue donc (compteur = 0), et
/// pourtant la cloche CONVERGE à 1 quand le changement est diffusé — CE QUI PROUVE que la convergence provient
/// EXCLUSIVEMENT de la diffusion (reprojection du payload), jamais d'un GET de rechargement (garde-fou anti-flake).</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmClocheTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;

    /// <summary>Handler qui relaie tout vers l'API distante RÉELLE SAUF un <c>GET /api/notifications/…</c>, coupé
    /// par un échec de transport déterministe — reproduit « aucun GET de la cloche ne peut aboutir » pour prouver
    /// que la convergence vient de la diffusion, pas d'un rechargement.</summary>
    private sealed class NotificationsGetCoupe : DelegatingHandler
    {
        public NotificationsGetCoupe(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get && (request.RequestUri?.AbsolutePath.Contains("/api/notifications", StringComparison.Ordinal) ?? false))
                throw new HttpRequestException("GET notifications coupé (preuve déterministe : 0 GET sur push)");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    [Fact]
    public async Task La_cloche_converge_de_N_a_N_plus_1_par_reprojection_depuis_la_diffusion_sans_aucun_GET_sur_push()
    {
        // Given — API distante réelle, cycle de fond. L'écran du destinataire parent-a a son GET notifications
        // COUPÉ (preuve 0-GET) : sa cloche démarre donc à 0 (chargement initial impossible).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var clientCoupe = new HttpClient(new NotificationsGetCoupe(api.Server.CreateHandler())) { BaseAddress = api.Server.BaseAddress };
        Services.AddSingleton(clientCoupe);
        Services.AddSingleton(SessionComme("parent-a", "Alice"));
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });

        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        // Compteur initial N = 0 (aucun badge) : le GET de chargement de la cloche est coupé.
        Assert.Empty(grille.FindAll("[data-testid='cloche-badge']"));

        // When — depuis un AUTRE écran (client réel non coupé), un changement concernant parent-a est écrit :
        // délégation de 29/06 (résout parent-b) à parent-a → journal consigné + DIFFUSÉ (payload).
        var autreEcran = GrilleRuntimeHarness.ClientVers(api);
        (await autreEcran.PostAsJsonAsync(
            "api/canal/deleguer-recuperation",
            new DeleguerRecuperationRequete(new DateOnly(2026, 6, 29), "Léa", "parent-a"))).EnsureSuccessStatusCode();

        // La diffusion RÉELLE de l'événement est repoussée en boucle de fond (idempotente côté client : dédup par
        // id) pour qu'un push tombe APRÈS l'établissement de la connexion du hub (anti-flake timing, pattern s42/s43).
        var evenement = api.Services.GetRequiredService<IJournalChangements>().Tout().Single(e => e.RecevantId == "parent-a");
        var notificateur = api.Services.GetRequiredService<INotificateurChangement>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierChangement(evenement);
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — SANS aucun GET (celui de la cloche est coupé), la cloche CONVERGE : compteur 0 → 1 et une
            // nouvelle notification (délégation) apparaît en tête du panneau — reprojection du payload diffusé.
            grille.WaitForAssertion(
                () => Assert.Equal("1", grille.Find("[data-testid='cloche-badge']").TextContent.Trim()),
                TimeSpan.FromSeconds(15));

            this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
            grille.WaitForAssertion(
                () =>
                {
                    var notifs = grille.Find("[data-testid='cloche-panneau']").QuerySelectorAll("[data-testid='cloche-notif']");
                    Assert.Single(notifs);
                    Assert.Equal("delegation", notifs[0].GetAttribute("data-type"));
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
