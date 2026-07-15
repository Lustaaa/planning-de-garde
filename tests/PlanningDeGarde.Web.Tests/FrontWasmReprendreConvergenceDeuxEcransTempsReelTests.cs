using System;
using System.Collections.Generic;
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

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 46 — Sc.6 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : après une REPRISE sur le 1ᵉʳ écran, la CASE
/// DU JOUR d'un 2ᵉ écran CONVERGE sans rechargement dédié — elle retombe sur le FOND (Alice) et le transfert
/// dérivé bicolore (s31) DISPARAÎT — par REPROJECTION CLIENT de la grille rafraîchie via la diffusion SignalR
/// de LECTURE SEULE (s20). L'écriture, elle, a transité par le canal requête/réponse (POST annuler-delegation).
///
/// <para>0 GET DÉDIÉ sur push : la reprise RÉUTILISE le MÊME bus de convergence que toute écriture (le hub
/// rejoue la grille en main) — aucun aller-retour spécifique à la reprise n'est ajouté (garde anti-amplification
/// flake TempsReel). La case se recalcule depuis la MÊME grille relue.</para>
///
/// Anti « vert qui ment » : l'écriture part réellement du canal (POST) de l'écran 1, la convergence de l'écran
/// 2 passe par la diffusion SignalR RÉELLE — pas une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmReprendreConvergenceDeuxEcransTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 PAIRE → fond parent-a (Alice)

    private static CycleDeFond CycleAliceBruno()
        => new(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" });

    [Fact]
    public async Task La_case_du_2e_ecran_converge_sur_le_fond_et_le_transfert_derive_disparait()
    {
        // Given — API distante réelle, cycle de fond N=2 (Alice paire ISO), et une DÉLÉGATION ACTIVE sur le
        // 08/07 (surcharge Bruno). Deux écrans câblés sur le MÊME foyer, même semaine.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, CycleAliceBruno());
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b",
            Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue));

        var ecran1 = RendreGrillePartagee(api, Aujourdhui);
        var ecran2 = RendreGrillePartagee(api, Aujourdhui);

        // Précondition écran 2 — la CASE du 08/07 résout Bruno (surcharge > fond) + pastille bicolore
        // (transfert dérivé Alice → Bruno du cycle).
        ecran2.WaitForAssertion(
            () =>
            {
                Assert.Equal("Bruno",
                    GrilleRuntimeHarness.CaseDuJour(ecran2, "08/07").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                Assert.NotNull(GrilleRuntimeHarness.CaseDuJour(ecran2, "08/07").QuerySelector("[data-testid='case-transfert-bicolore']"));
            },
            TimeSpan.FromSeconds(10));

        // When — sur l'ÉCRAN 1, un Parent reprend le 08/07 via le menu clic-case (entrée conditionnelle) →
        // mini-dialog de confirmation → « Reprendre ce jour » (écriture par le canal requête/réponse).
        ecran1.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(ecran1, "08/07").Click());
                this.SurDispatcher(() => ecran1.Find("[data-testid='menu-actions-case'] [data-testid='action-reprendre']").Click());
                Assert.NotEmpty(ecran1.FindAll("[data-testid='dialog-reprendre']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => ecran1.Find("[data-testid='dialog-reprendre-confirmer']").Click());

        // La diffusion SignalR RÉELLE est repoussée en boucle de fond (idempotente : le store est déjà muté)
        // pour qu'un push tombe APRÈS l'établissement des connexions (anti-flake timing, pattern harnais s42/s43).
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
            // Then — SANS rechargement dédié, l'ÉCRAN 2 CONVERGE (reprojection client depuis la grille relue) :
            // la CASE du 08/07 retombe sur le FOND (Alice) ET la pastille bicolore du transfert dérivé DISPARAÎT.
            ecran2.WaitForAssertion(
                () =>
                {
                    Assert.Equal("Alice",
                        GrilleRuntimeHarness.CaseDuJour(ecran2, "08/07").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Null(GrilleRuntimeHarness.CaseDuJour(ecran2, "08/07").QuerySelector("[data-testid='case-transfert-bicolore']"));
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
