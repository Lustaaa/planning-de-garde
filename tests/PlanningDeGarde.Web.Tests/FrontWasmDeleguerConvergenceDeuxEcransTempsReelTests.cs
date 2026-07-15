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
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 44 — Sc.6 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : après une délégation sur le 1ᵉʳ écran, la
/// CARTE « Aujourd'hui » (s42) ET le PANNEAU « À venir » (s43) d'un 2ᵉ écran CONVERGENT sans rechargement —
/// nouveau responsable + TRANSFERT dérivé bicolore (s31) — par REPROJECTION CLIENT de la grille rafraîchie
/// via la diffusion SignalR de LECTURE SEULE (s20). L'écriture, elle, a transité par le canal requête/réponse.
///
/// <para>Profil réaliste : un CYCLE DE FOND (parent-a semaines paires ISO, parent-b impaires) pilote le
/// planning. Aujourd'hui (lundi 29/06, ISO 27 → Bruno) porte déjà un transfert dérivé Alice→Bruno (bascule du
/// cycle au lundi). Déléguer la récupération d'aujourd'hui à Nina (la nounou) sur l'écran 1 fait, sur l'écran
/// 2 : (a) la CARTE reprojeter le transfert dérivé Alice→<b>Nina</b> (nouveau recevant) ; (b) le PANNEAU
/// (30/06) reprojeter un NOUVEAU transfert dérivé <b>Nina→Bruno</b> (la bascule se décale d'un jour). Aucune
/// teinte réinventée, aucun GET dédié sur push : les deux surfaces se recalculent depuis la MÊME grille relue.</para>
///
/// Anti « vert qui ment » : l'écriture part réellement du canal (POST) de l'écran 1, la convergence de l'écran
/// 2 passe par la diffusion SignalR RÉELLE — pas une doublure. Un GET dédié par surface amplifierait le flake
/// TempsReel (garde s42/s43) : ici les surfaces sont de simples reprojections de la grille en main.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDeleguerConvergenceDeuxEcransTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026 (ISO 27 → Bruno)

    [Fact]
    public async Task La_carte_et_le_panneau_du_2e_ecran_convergent_sur_le_delegataire_et_le_transfert_derive()
    {
        // Given — API distante réelle, cycle de fond N=2 (parent-a index 0, parent-b index 1) : le planning
        // est piloté par le cycle (Alice / Bruno selon parité ISO). Deux écrans câblés sur le MÊME foyer.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        var ecran1 = RendreGrillePartagee(api, Aujourdhui);
        var ecran2 = RendreGrillePartagee(api, Aujourdhui);

        // Précondition écran 2 — aujourd'hui (29/06) porte déjà un transfert dérivé Alice → Bruno (bascule du
        // cycle au lundi), et le 30/06 est unicolore Bruno (même semaine ISO, pas de bascule).
        ecran2.WaitForAssertion(
            () => Assert.Equal("Bruno",
                ecran2.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-transfert-recevant']").TextContent.Trim()),
            TimeSpan.FromSeconds(10));
        Assert.NotNull(Ligne(ecran2, "2026-06-30").QuerySelector("[data-testid='a-venir-responsable']"));

        // When — sur l'ÉCRAN 1, un Parent délègue la récupération d'aujourd'hui à Nina (la nounou) via l'ENTRÉE
        // DU MENU CLIC-CASE (surface tranchée au gate G3) : clic sur la case du jour (29/06) → menu → entrée
        // « déléguer ce jour » → mini-dialog (écriture par le canal requête/réponse). Puis la diffusion SignalR
        // RÉELLE est repoussée en boucle de fond (idempotente : le store est déjà muté) pour qu'un push tombe
        // APRÈS l'établissement des connexions (anti-flake timing, pattern harnais s42/s43).
        ecran1.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(ecran1, "29/06").Click());
                this.SurDispatcher(() => ecran1.Find("[data-testid='menu-actions-case'] [data-testid='action-deleguer']").Click());
                Assert.NotEmpty(ecran1.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => ecran1.Find("[data-testid='champ-delegataire']").Change("nounou"));
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
            //  (a) la CARTE du jour reprojette le transfert dérivé Alice → NINA (nouveau recevant délégué) ;
            ecran2.WaitForAssertion(
                () => Assert.Equal("Nina la nounou",
                    ecran2.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-transfert-recevant']").TextContent.Trim()),
                TimeSpan.FromSeconds(15));
            Assert.Equal("Alice",
                ecran2.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-transfert-cedant']").TextContent.Trim());

            //  (b) le PANNEAU à-venir (30/06) reprojette un NOUVEAU transfert dérivé bicolore NINA → BRUNO
            //      (la bascule s'est décalée d'un jour du fait de la surcharge d'aujourd'hui).
            ecran2.WaitForAssertion(
                () =>
                {
                    var transfert = Ligne(ecran2, "2026-06-30").QuerySelector("[data-testid='a-venir-transfert-bicolore']");
                    Assert.NotNull(transfert);
                    Assert.Equal("Nina la nounou",
                        Ligne(ecran2, "2026-06-30").QuerySelector("[data-testid='a-venir-transfert-cedant']")!.TextContent.Trim());
                    Assert.Equal("Bruno",
                        Ligne(ecran2, "2026-06-30").QuerySelector("[data-testid='a-venir-transfert-recevant']")!.TextContent.Trim());
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }

    private static AngleSharp.Dom.IElement Ligne(IRenderedComponent<PlanningPartage> grille, string dateIso)
        => grille.Find("[data-testid='panneau-a-venir']")
            .QuerySelectorAll("[data-testid='a-venir-jour']")
            .Single(j => j.GetAttribute("data-date") == dateIso);

    /// <summary>
    /// Rend une grille réelle câblée à l'API distante en PARTAGEANT le provider DI de la TestContext (mêmes
    /// singletons : client HTTP vers l'API réelle, session Parent connectée, horloge figée, options de hub
    /// redirigées vers le TestServer). Enregistré une seule fois puis appelé pour chaque écran — les deux
    /// grilles observent le MÊME store distant et la MÊME diffusion SignalR (2ᵉ écran = 2ᵉ instance rendue).
    /// </summary>
    private bool _servicesEnregistres;

    private IRenderedComponent<PlanningPartage> RendreGrillePartagee(ApiDistanteFactory api, DateTime aujourdhui)
    {
        // Enregistrement UNE seule fois (résoudre un service initialise le provider bUnit et interdit tout
        // Add ultérieur) : les deux écrans partagent alors les mêmes singletons (même API, même hub).
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
