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
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.State;
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 51 — Sc.7 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME, TEMPS RÉEL : la cloche d'un 2ᵉ écran (le recevant
/// parent-a) CONVERGE sur une proposition GREFFÉE SUR UN IMPRÉVU (composition s51) écrite depuis un AUTRE écran —
/// badge de non-lus + notif ACTIONNABLE apparaissent — par REPROJECTION CLIENT depuis la DIFFUSION PORTEUSE DE
/// PAYLOAD (INotificateurChangement / PropositionEchangeSnapshot s47), <b>0 GET dédié sur push</b>. La diffusion
/// est une donnée de LECTURE : elle ne déclenche AUCUNE écriture (store des surcharges intact pendant le push).
/// Puis ACCEPTER depuis ce 2ᵉ écran COMPOSE la délégation s44 → la case du jour converge sur le recevant.
///
/// <para><b>Preuve stricte du « 0 GET sur push »</b> : le <c>GET /api/notifications/…</c> de cet écran est COUPÉ
/// (échec de transport déterministe) — la cloche démarre à 0 (chargement initial impossible) et CONVERGE pourtant à
/// 1 quand la proposition greffée est diffusée, prouvant que la convergence vient EXCLUSIVEMENT de la diffusion.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmProposerEchangeSuiteImprevuConvergenceTempsReelTests : TestContext
{
    private static readonly DateOnly Jour = new(2026, 6, 29); // 29/06 → responsable de fond parent-b

    /// <summary>Coupe UNIQUEMENT le GET /api/notifications (preuve 0-GET) ; tout le reste relaie vers l'API réelle.</summary>
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

    /// <summary>Sème un imprévu (signalé par le responsable résolu parent-b) PUIS une proposition GREFFÉE dessus
    /// (composition s51) vers parent-a, par le canal réel d'un AUTRE écran. Retourne l'id de la proposition.</summary>
    private static async Task<string> SemerImprevuPuisPropositionGreffeeVersParentA(ApiDistanteFactory api)
    {
        var autreEcran = GrilleRuntimeHarness.ClientVers(api);
        (await autreEcran.PostAsJsonAsync("api/imprevus",
            new SignalerImprevuRequete(Jour, "Léa", TypeImprevu.Malade, "parent-b", ""))).EnsureSuccessStatusCode();
        var imprevuId = api.Services.GetRequiredService<IJournalChangements>().Tout()
            .Single(e => e.Type == TypeChangement.Imprevu).Id;
        (await autreEcran.PostAsJsonAsync("api/propositions/suite-imprevu",
            new ProposerEchangeSuiteImprevuRequete(imprevuId, "parent-a"))).EnsureSuccessStatusCode();
        return api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots()
            .Single(p => p.VersActeurId == "parent-a").Id;
    }

    [Fact]
    public async Task La_cloche_du_recevant_converge_sur_la_proposition_greffee_par_diffusion_0_GET_et_accepter_compose_la_delegation()
    {
        // Given — API réelle, cycle de fond. L'écran du recevant parent-a a son GET notifications COUPÉ (preuve 0-GET) :
        // sa cloche démarre à 0 (chargement initial impossible).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var clientCoupe = new HttpClient(new NotificationsGetCoupe(api.Server.CreateHandler())) { BaseAddress = api.Server.BaseAddress };
        Services.AddSingleton(clientCoupe);
        Services.AddSingleton(SessionComme("parent-a", "Alice"));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });

        var cloche = RenderComponent<Cloche>();
        Assert.Empty(cloche.FindAll("[data-testid='cloche-badge']")); // N = 0 (GET coupé)

        // When — depuis un AUTRE écran, une proposition GREFFÉE sur un imprévu est écrite vers parent-a (composition s51).
        var propositionId = await SemerImprevuPuisPropositionGreffeeVersParentA(api);

        // La diffusion RÉELLE de la proposition (pending) est repoussée en boucle de fond (idempotente : dédup par id)
        // pour qu'un push tombe APRÈS l'établissement de la connexion du hub (anti-flake timing, pattern s42/s43).
        var propositionPending = api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots().Single(p => p.Id == propositionId);
        var notificateur = api.Services.GetRequiredService<INotificateurChangement>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierProposition(propositionPending);
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — SANS aucun GET (celui de la cloche est coupé), la cloche CONVERGE : badge 0 → 1 et la proposition
            // greffée apparaît ACTIONNABLE en tête du panneau — reprojection du payload diffusé (0 GET sur push).
            cloche.WaitForAssertion(
                () => Assert.Equal("1", cloche.Find("[data-testid='cloche-badge']").TextContent.Trim()),
                TimeSpan.FromSeconds(15));
            this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
            cloche.WaitForAssertion(
                () =>
                {
                    var notif = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']");
                    Assert.NotNull(notif.QuerySelector("[data-testid='cloche-accepter']"));
                    Assert.NotNull(notif.QuerySelector("[data-testid='cloche-refuser']"));
                },
                TimeSpan.FromSeconds(15));

            // La diffusion est une donnée de LECTURE : AUCUNE écriture n'a eu lieu pendant le push (store intact).
            Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }

        // When — ACCEPTER depuis ce 2ᵉ écran (mini-dialog de confirmation → Confirmer, canal d'écriture).
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-accepter']").Click());
        cloche.WaitForAssertion(() => Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-confirmer']").Click());

        // Then — ACCEPTER COMPOSE la délégation s44 : la case du jour converge sur le recevant (surcharge + projection Alice).
        cloche.WaitForAssertion(
            () =>
            {
                Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                    p => p.Id == propositionId && p.Statut == StatutProposition.Acceptee);
                Assert.Single(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
            },
            TimeSpan.FromSeconds(15));
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        Assert.Equal("Alice", projection.Projeter(Jour).Jours.Single(j => j.Date == Jour).NomResponsable);
    }
}
