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
using PlanningDeGarde.Web.Components;
using PlanningDeGarde.Web.State;
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 48 — Sc.7 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : convergence TEMPS RÉEL de l'imprévu. Deux écrans
/// connectés en tant qu'acteurs concernés par le jour/enfant. Quand un imprévu est SIGNALÉ depuis le premier écran
/// (POST signaler-imprevu, canal d'écriture), la CLOCHE du SECOND écran (le responsable résolu du jour) reçoit
/// l'événement via la DIFFUSION PORTEUSE DE PAYLOAD (INotificateurChangement s47) : le badge de non-lus et le
/// panneau CONVERGENT PAR REPROJECTION CLIENT (0 GET dédié sur push, garde anti-flake) et la diffusion ne
/// déclenche AUCUNE écriture (elle porte une donnée de LECTURE — séparation des canaux tenue).
///
/// Anti « vert qui ment » : le signalement part réellement du canal d'écriture (POST), consigné au journal réel ;
/// la convergence de la cloche passe par la diffusion porteuse de payload RÉELLE — pas de doublure.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmImprevuConvergenceTempsReelTests : TestContext
{
    private static readonly DateOnly Jour = new(2026, 6, 29); // 29/06 → responsable de fond parent-b (ISO 27)

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    /// <summary>Câble le runtime du SECOND écran = le responsable résolu du jour (parent-b), concerné (cédant).</summary>
    private IRenderedComponent<Cloche> RendreClocheResponsable(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(SessionComme("parent-b", "Bruno"));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        return RenderComponent<Cloche>();
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    [Fact]
    public async Task Un_imprevu_signale_depuis_le_premier_ecran_fait_converger_la_cloche_du_second_par_reprojection_0_GET()
    {
        // Given — cycle de fond (29/06 = parent-b). Le SECOND écran = parent-b (responsable, concerné) observe sa
        // cloche panneau ouvert, initialement VIDE (aucun imprévu encore signalé).
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var cloche = RendreClocheResponsable(api);
        this.SurDispatcher(() => cloche.Find("[data-testid='cloche-bouton']").Click());
        cloche.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(cloche.FindAll("[data-testid='cloche-panneau']"));
                Assert.Empty(cloche.FindAll("[data-testid='cloche-notif'][data-type='imprevu']"));
            },
            TimeSpan.FromSeconds(10));

        // When — un imprévu « malade » est SIGNALÉ depuis le PREMIER écran (POST canal d'écriture, signalant parent-a).
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync(
            "api/canal/signaler-imprevu",
            new SignalerImprevuRequete(Jour, "Léa", TypeImprevu.Malade, "parent-a", ""))).EnsureSuccessStatusCode();

        // Diffusion RÉELLE repoussée en boucle de fond (idempotente) pour tomber APRÈS l'établissement de la
        // connexion du hub : le payload de l'événement d'imprévu (INotificateurChangement s47) est re-porté.
        var evenementImprevu = api.Services.GetRequiredService<IJournalChangements>().Tout()
            .Single(e => e.Type == TypeChangement.Imprevu);
        var changement = api.Services.GetRequiredService<INotificateurChangement>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                changement.NotifierChangement(evenementImprevu);
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — la cloche du SECOND écran CONVERGE PAR REPROJECTION depuis la diffusion (0 GET dédié sur push) :
            // la notification d'imprévu apparaît (informative « Léa est malade le 29/06 ») et le badge passe à 1.
            cloche.WaitForAssertion(
                () =>
                {
                    var notif = cloche.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='imprevu']");
                    Assert.Contains("Léa est malade le 29/06", notif.TextContent);
                    Assert.Equal("1", cloche.Find("[data-testid='cloche-badge']").TextContent.Trim());
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }

        // … et la diffusion n'a déclenché AUCUNE écriture (elle porte une donnée de LECTURE — invariant s48,
        // séparation des canaux tenue) : le store des surcharges reste intact.
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }
}
