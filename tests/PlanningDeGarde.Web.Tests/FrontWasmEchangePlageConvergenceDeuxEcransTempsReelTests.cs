using System;
using System.Collections.Generic;
using System.Linq;
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
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 52 — Sc.10 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : quand le recevant ACCEPTE une proposition
/// d'échange de PLAGE (1ᵉʳ écran = POST accord), le 2ᵉ écran voit TOUTES les cases de la plage <c>[J1..J3]</c>
/// CONVERGER (nouveau responsable + transferts dérivés s31 aux DEUX frontières) SANS rechargement, par
/// REPROJECTION CLIENT de la grille rafraîchie via la diffusion SignalR de LECTURE SEULE (s20/s47). L'écriture,
/// elle, a transité par le canal requête/réponse. AUCUN GET DÉDIÉ sur push (anti-amplification flake, garde s38).
///
/// <para>Profil réaliste : un CYCLE DE FOND (parent-a paires ISO, parent-b impaires) pilote le planning. La semaine
/// du 29/06 (ISO 27 impaire) est portée par Bruno (parent-b). Une proposition pending sur la plage [30/06 .. 02/07]
/// vers Alice (parent-a) est acceptée : chaque case 30/06, 01/07, 02/07 reprojette Alice ; l'entrée (30/06) et la
/// sortie (03/07) matérialisent un transfert dérivé bicolore.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmEchangePlageConvergenceDeuxEcransTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026 (ISO 27 → Bruno)
    private static readonly DateOnly J1 = new(2026, 6, 30);
    private static readonly DateOnly J3 = new(2026, 7, 2);

    [Fact]
    public async Task Accord_du_recevant_fait_converger_toutes_les_cases_de_la_plage_du_2e_ecran_sans_GET_sur_push()
    {
        // Given — API distante réelle, cycle de fond N=2 (parent-a index 0, parent-b index 1). Deux écrans.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        // Une proposition pending sur la PLAGE [30/06 .. 02/07] vers Alice (parent-a), écrite par le canal réel.
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/proposer-echange",
            new ProposerEchangeRequete(J1, "Léa", "parent-a", J3))).EnsureSuccessStatusCode();
        var propositionId = api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots()
            .Single(p => p.VersActeurId == "parent-a").Id;

        var ecran2 = RendreGrillePartagee(api, Aujourdhui);

        // Précondition écran 2 — la semaine (ISO 27) est UNICOLORE Bruno : chaque jour de la plage résout Bruno,
        // aucune pastille de transfert (la proposition PENDING n'a rien écrit).
        ecran2.WaitForAssertion(
            () =>
            {
                foreach (var jjMM in new[] { "30/06", "01/07", "02/07" })
                {
                    Assert.Equal("Bruno",
                        GrilleRuntimeHarness.CaseDuJour(ecran2, jjMM).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Null(GrilleRuntimeHarness.CaseDuJour(ecran2, jjMM).QuerySelector("[data-testid='case-transfert-bicolore']"));
                }
            },
            TimeSpan.FromSeconds(10));

        // When — le RECEVANT ACCEPTE (1ᵉʳ écran = POST accord, canal requête/réponse). Puis la diffusion SignalR
        // RÉELLE (MiseAJour) est repoussée en boucle de fond (idempotente) pour tomber APRÈS l'établissement des
        // connexions (anti-flake timing) — c'est le SEUL signal de convergence : le 2ᵉ écran reprojette la grille
        // relue (canal de LECTURE s20), AUCUN GET dédié n'est déclenché par le push.
        (await client.PostAsJsonAsync("api/canal/accepter-proposition", new RepondrePropositionRequete(propositionId))).EnsureSuccessStatusCode();

        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseurDeDiffusion = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — SANS rechargement, l'ÉCRAN 2 CONVERGE (reprojection client depuis la grille relue) :
            //  (a) CHAQUE case de la plage (30/06, 01/07, 02/07) reprojette le nouveau responsable Alice ;
            ecran2.WaitForAssertion(
                () =>
                {
                    foreach (var jjMM in new[] { "30/06", "01/07", "02/07" })
                        Assert.Equal("Alice",
                            GrilleRuntimeHarness.CaseDuJour(ecran2, jjMM).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                },
                TimeSpan.FromSeconds(15));

            //  (b) la case d'ENTRÉE (30/06) et (c) la case de SORTIE (03/07) matérialisent un transfert dérivé
            //      bicolore ; la sortie résout de nouveau Bruno (fond).
            ecran2.WaitForAssertion(
                () =>
                {
                    Assert.NotNull(GrilleRuntimeHarness.CaseDuJour(ecran2, "30/06").QuerySelector("[data-testid='case-transfert-bicolore']"));
                    Assert.NotNull(GrilleRuntimeHarness.CaseDuJour(ecran2, "03/07").QuerySelector("[data-testid='case-transfert-bicolore']"));
                    Assert.Equal("Bruno",
                        GrilleRuntimeHarness.CaseDuJour(ecran2, "03/07").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }

    private bool _servicesEnregistres;

    private IRenderedComponent<PlanningPartage> RendreGrillePartagee(ApiDistanteFactory api, DateTime aujourdhui)
    {
        if (!_servicesEnregistres)
        {
            _servicesEnregistres = true;
            Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
            Services.AddSingleton(GrilleRuntimeHarness.SessionConnectee());
            Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(aujourdhui));
            Services.AddSingleton(new OptionsConnexionHub
            {
                Configurer = options =>
                {
                    options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                },
            });
        }

        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='jour-case']").Count == 28,
            TimeSpan.FromSeconds(10));
        return grille;
    }
}
