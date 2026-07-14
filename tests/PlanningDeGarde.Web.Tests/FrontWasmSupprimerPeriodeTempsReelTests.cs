using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.10 (🖥️ IHM, <c>@limite</c> — temps réel). La suppression d'une
/// période <b>aboutie</b> via le canal d'écriture déclenche la <b>diffusion SignalR (lecture seule)</b>
/// qui rafraîchit un <b>second écran</b> affichant le même planning partagé, <b>sans rechargement</b>.
/// Deux écrans câblés à la <b>MÊME API distante réelle</b> (<see cref="ApiDistanteFactory"/> unique → store
/// singleton partagé, hub SignalR réel commun) : l'écran 2 est la <b>grille</b>
/// (<see cref="Web.Components.Pages.PlanningPartage"/>), l'écran 1 émet la suppression sur le canal réel.
///
/// Montage : « Nina la nounou » garde le mardi 16/06/2026 (<b>surcharge</b>) ; le <b>cycle de fond</b>
/// (N=1, index 0 → parent-a) attribue « Alice » à toute la fenêtre. La surcharge prime → la case du 16/06
/// porte « Nina la nounou ». Quand l'écran 1 supprime la période (<c>POST /api/canal/supprimer-periode</c>
/// → diffusion temps réel <b>sur succès</b>), l'écran 2 voit, sans rechargement : la case du 16/06
/// <b>retomber sur Parent A</b> (« Alice », repli surcharge &gt; fond) et la <b>légende dédoublonnée</b>
/// ne <b>plus</b> faire apparaître Nina.
///
/// Convention anti-flake *TempsReel* : la <b>pompe de diffusion</b> ré-émet la suppression (idempotente :
/// no-op qui réussit ET re-déclenche la diffusion par l'endpoint) en boucle jusqu'à ce qu'un push tombe
/// APRÈS l'établissement de la connexion long polling — JAMAIS un appel direct au notificateur (sinon le
/// vert ne prouverait pas que l'endpoint diffuse). Sans la diffusion par l'endpoint, aucune re-projection
/// n'atteint l'écran 2 → rouge. Anti « vert qui ment » : le baseline « Nina la nounou » est asserté avant ;
/// un bUnit à doublure ne prouverait ni le store partagé, ni le client SignalR, ni la re-projection runtime.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSupprimerPeriodeTempsReelTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public async Task Should_Faire_retomber_la_case_du_16_06_sur_Alice_et_retirer_Nina_de_la_legende_du_second_ecran_sans_rechargement_When_l_ecran_1_supprime_la_periode()
    {
        // Given — UNE seule API distante réelle (store singleton partagé). Nina garde le 16/06 (surcharge) ;
        // le cycle de fond (N=1, index 0 → parent-a) attribue « Alice » à toute la fenêtre.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);
        GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(1, new Dictionary<int, string> { [0] = "parent-a" }));

        // Écran 2 (le « second écran ») = la grille réellement câblée à l'API, dans un TestContext distinct
        // (navigateur / DI séparés, client SignalR propre).
        using var ecran2 = new TestContext();
        var grille2 = GrilleRuntimeHarness.RendreGrille(ecran2, api, Mardi_16_06_2026);

        // … baseline : la surcharge prime → la case du 16/06 porte « Nina la nounou », et la légende
        // dédoublonnée le fait apparaître (aux côtés d'Alice, fond présent sur la fenêtre).
        Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille2, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Contains(grille2.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");

        // When — écran 1 = le canal d'écriture réel (le POST exact que la dialog émet, prouvée en Sc.6) :
        // on supprime la période de Nina par son identifiant stable, relu du store partagé.
        var client1 = GrilleRuntimeHarness.ClientVers(api);
        var idPeriode = api.Services.GetRequiredService<IPeriodeRepository>()
            .AllSnapshots().Single(p => p.ResponsableId == "nounou").Id;

        // … pompe de diffusion : ré-émet la suppression (idempotente) jusqu'à ce que le push SignalR tombe
        // APRÈS l'établissement de la connexion long polling du second écran. La diffusion vient de
        // l'ENDPOINT (sur succès), jamais d'un appel direct au notificateur.
        using var diffusionContinue = new CancellationTokenSource();
        var pousseurDeDiffusion = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                await client1.PostAsJsonAsync("api/canal/supprimer-periode", new { PeriodeId = idPeriode });
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — sans rechargement, le second écran voit la case du 16/06 retomber sur « Alice » (repli
            // surcharge > fond) et la légende dédoublonnée ne fait plus apparaître Nina.
            grille2.WaitForAssertion(
                () =>
                {
                    Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille2, "16/06")
                        .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.DoesNotContain(grille2.FindAll("[data-testid='legende-entree']"),
                        e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }
}
