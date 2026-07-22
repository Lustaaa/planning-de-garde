using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 45 — Sc.6 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : après une délégation de PLAGE sur le 1ᵉʳ écran,
/// CHAQUE case de la plage de la GRILLE AGENDA d'un 2ᵉ écran CONVERGE sans rechargement — nouveau responsable
/// (le délégataire) sur chaque jour + TRANSFERTS dérivés bicolores (s31) aux DEUX frontières — par REPROJECTION
/// CLIENT de la grille rafraîchie via la diffusion SignalR de LECTURE SEULE (s20). L'écriture, elle, a transité
/// par le canal requête/réponse. AUCUN GET dédié sur push (anti-amplification flake, garde s38/s44).
///
/// <para>Profil réaliste : un CYCLE DE FOND (parent-a semaines paires ISO, parent-b impaires) pilote le
/// planning. La semaine du lundi 29/06 (ISO 27 impaire) est portée par Bruno (parent-b). Déléguer la plage
/// [30/06 .. 02/07] à Nina (la nounou) sur l'écran 1 fait, sur l'écran 2 : (a) chaque case 30/06, 01/07, 02/07
/// reprojeter le nouveau responsable <b>Nina</b> ; (b) la case d'ENTRÉE (30/06) matérialiser un transfert dérivé
/// Bruno → Nina ; (c) la case de SORTIE (03/07) matérialiser un transfert dérivé Nina → Bruno. Aucune teinte
/// réinventée, aucun GET dédié : les cases se recalculent depuis la MÊME grille relue.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDeleguerPlageConvergenceDeuxEcransTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026 (ISO 27 → Bruno)

    [Fact]
    public async Task Toutes_les_cases_de_la_plage_du_2e_ecran_convergent_sur_le_delegataire_et_les_transferts_derives()
    {
        // Given — API distante réelle, cycle de fond N=2 (parent-a index 0, parent-b index 1). Deux écrans.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var ecran1 = RendreGrillePartagee(api, Aujourdhui);
        var ecran2 = RendreGrillePartagee(api, Aujourdhui);

        // Précondition écran 2 — la semaine (ISO 27) est UNICOLORE Bruno : chaque jour de la plage résout Bruno,
        // aucune pastille de transfert (pas de bascule intra-semaine).
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

        // When — sur l'ÉCRAN 1, un Parent délègue la PLAGE [30/06 .. 02/07] à Nina via le menu clic-case : clic
        // sur la case du 30/06 → menu → « déléguer ce jour » → choisir Nina, porter « jusqu'au » au 02/07, valider
        // (écriture par le canal requête/réponse). Puis la diffusion SignalR RÉELLE est repoussée en boucle de
        // fond (idempotente) pour qu'un push tombe APRÈS l'établissement des connexions (anti-flake timing).
        ecran1.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(ecran1, "30/06").Click());
                this.SurDispatcher(() => ecran1.Find("[data-testid='menu-actions-case'] [data-testid='action-deleguer']").Click());
                Assert.NotEmpty(ecran1.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => ecran1.Find("[data-testid='champ-delegataire']").Change("nounou"));
        this.SurDispatcher(() => ecran1.Find("[data-testid='champ-jusqu-au']").Change("2026-07-02"));
        this.SurDispatcher(() => ecran1.Find("[data-testid='dialog-deleguer'] form").Submit());

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
            //  (a) CHAQUE case de la plage (30/06, 01/07, 02/07) reprojette le nouveau responsable NINA ;
            ecran2.WaitForAssertion(
                () =>
                {
                    foreach (var jjMM in new[] { "30/06", "01/07", "02/07" })
                        Assert.Equal("Nina la nounou",
                            GrilleRuntimeHarness.CaseDuJour(ecran2, jjMM).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                },
                TimeSpan.FromSeconds(15));

            //  (b) la case d'ENTRÉE (30/06) matérialise un transfert dérivé bicolore (Bruno → Nina) ;
            //  (c) la case de SORTIE (03/07) matérialise un transfert dérivé bicolore + résout de nouveau Bruno.
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

    /// <summary>
    /// Rend une grille réelle câblée à l'API distante en PARTAGEANT le provider DI de la TestContext (mêmes
    /// singletons : client HTTP vers l'API réelle, session Parent connectée, horloge figée, options de hub
    /// redirigées vers le TestServer). Les deux grilles observent le MÊME store distant et la MÊME diffusion.
    /// </summary>
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
