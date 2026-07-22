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
/// Sprint 53 — Sc.9 (🖥️ @ihm) — NIVEAU RUNTIME : ISOLATION temps réel PAR ENFANT. Deux écrans Parent partagent
/// le MÊME foyer et la MÊME diffusion SignalR. Le 1ᵉʳ affiche "Tom" ; le 2ᵉ délègue la récupération d'un jour
/// de "Léa". Quand la diffusion arrive au 1ᵉʳ écran, sa vue AFFICHÉE (Tom) reste INCHANGÉE (la délégation de Léa
/// ne fuit pas dans la grille de Tom) ; en basculant sur "Léa", la case déléguée apparaît déjà à jour.
///
/// <para>Convergence par le canal de LECTURE SEULE s20 EXISTANT (la grille reprojette la fenêtre relue de
/// l'enfant affiché) — AUCUN GET DÉDIÉ n'est ajouté sur push (garde [[flake-signalr-blast-radius]] : ce sprint
/// n'introduit AUCUN nouveau client SignalR, il réutilise celui de la grille en le filtrant par enfant).</para>
///
/// Anti « vert qui ment » : l'écriture part réellement du canal (POST) de l'écran 2, la convergence de l'écran 1
/// passe par la diffusion SignalR RÉELLE et la projection isolée par enfant réelle — jamais une doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmDeleguerMultiEnfantsIsolationTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026 (ISO 27 → Bruno)
    private const string LeaId = "Léa";
    private const string TomId = "tom";
    // 01/07/2026 : semaine ISO 27 → index 1 → fond parent-b (Bruno) pour les DEUX enfants (cycle partagé).
    private static readonly DateTime Mercredi_01_07_2026 = new(2026, 7, 1);

    private bool _servicesEnregistres;

    private IRenderedComponent<PlanningPartage> RendreEcran(ApiDistanteFactory api)
    {
        if (!_servicesEnregistres)
        {
            _servicesEnregistres = true;
            Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
            Services.AddSingleton(GrilleRuntimeHarness.SessionConnectee());
            Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));
            Services.AddSingleton(new OptionsConnexionHub
            {
                Configurer = options =>
                {
                    options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                },
            });
        }

        var ecran = RenderComponent<PlanningPartage>();
        ecran.WaitForState(() => ecran.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        return ecran;
    }

    private void Selectionner(IRenderedComponent<PlanningPartage> ecran, string enfantId)
        => this.SurDispatcher(() => ecran.Find("[data-testid='selecteur-enfant-carte']").Change(enfantId));

    private static string? NomCase(IRenderedComponent<PlanningPartage> ecran, string jjMM)
        => GrilleRuntimeHarness.CaseDuJour(ecran, jjMM).QuerySelector("[data-testid='nom-responsable']")?.TextContent.Trim();

    [Fact]
    public async Task La_delegation_de_Lea_converge_sur_Lea_sans_toucher_la_vue_Tom_affichee_sur_le_1er_ecran()
    {
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter(TomId, "Tom");
        var _cyc = new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }); GrilleRuntimeHarness.SemerCycle(api, _cyc); GrilleRuntimeHarness.SemerCycle(api, _cyc, TomId);

        var ecran1 = RendreEcran(api);
        var ecran2 = RendreEcran(api);

        // 1er écran affiche TOM ; le 01/07 y résout le fond Bruno (Tom n'a aucune surcharge).
        Selectionner(ecran1, TomId);
        ecran1.WaitForAssertion(() => Assert.Equal("Bruno", NomCase(ecran1, "01/07")), TimeSpan.FromSeconds(10));

        // 2e écran sélectionne LÉA et délègue le 01/07 à Nina (nounou) via le menu clic-case (canal d'écriture).
        Selectionner(ecran2, LeaId);
        ecran2.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(ecran2, "01/07").Click());
                this.SurDispatcher(() => ecran2.Find("[data-testid='menu-actions-case'] [data-testid='action-deleguer']").Click());
                Assert.NotEmpty(ecran2.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => ecran2.Find("[data-testid='champ-delegataire']").Change("nounou"));
        this.SurDispatcher(() => ecran2.Find("[data-testid='dialog-deleguer'] form").Submit());

        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusion = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusion.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusion.Token); } catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — le 2e écran (Léa) converge sur Nina (délégataire) — la délégation a bien pris.
            ecran2.WaitForAssertion(() => Assert.Equal("Nina la nounou", NomCase(ecran2, "01/07")), TimeSpan.FromSeconds(15));

            // Then — ISOLATION : le 1er écran affiche TOUJOURS Tom, sa case 01/07 RESTE Bruno (la délégation de
            // Léa ne fuit PAS dans la grille de Tom), même après plusieurs diffusions.
            for (var i = 0; i < 5; i++)
            {
                ecran1.WaitForAssertion(() => Assert.Equal("Bruno", NomCase(ecran1, "01/07")), TimeSpan.FromSeconds(3));
                Assert.NotEqual("Nina la nounou", NomCase(ecran1, "01/07"));
                Thread.Sleep(150);
            }

            // Then — en basculant le 1er écran sur LÉA, la case déléguée apparaît à jour (Nina).
            Selectionner(ecran1, LeaId);
            ecran1.WaitForAssertion(() => Assert.Equal("Nina la nounou", NomCase(ecran1, "01/07")), TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusion.Cancel();
            await pousseur;
        }
    }
}
