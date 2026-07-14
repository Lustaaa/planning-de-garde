using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.10 (🖥️ IHM, <c>@limite</c> — temps réel). La suppression d'un slot
/// <b>aboutie</b> via le canal d'écriture déclenche la <b>diffusion SignalR (lecture seule)</b> qui
/// rafraîchit un <b>second écran</b> affichant le même planning partagé, <b>sans rechargement</b>. Deux
/// écrans câblés à la <b>MÊME API distante réelle</b> (<see cref="ApiDistanteFactory"/> unique → store
/// singleton partagé, hub SignalR réel commun) : l'écran 2 est la <b>grille</b>
/// (<see cref="Web.Components.Pages.PlanningPartage"/>), l'écran 1 émet la suppression sur le canal réel.
///
/// Montage : un slot « École » 08h30-16h30 pour Léa le mardi 16/06/2026 est rendu dans la case du 16/06.
/// Quand l'écran 1 supprime le slot (<c>POST /api/canal/supprimer-slot</c> → diffusion temps réel <b>sur
/// succès</b>), l'écran 2 voit, sans rechargement, la case du 16/06 <b>ne plus afficher</b> le slot École.
///
/// Convention anti-flake *TempsReel* : la <b>pompe de diffusion</b> ré-émet la suppression (idempotente :
/// no-op qui réussit ET re-déclenche la diffusion par l'endpoint) en boucle jusqu'à ce qu'un push tombe
/// APRÈS l'établissement de la connexion long polling — JAMAIS un appel direct au notificateur (sinon le
/// vert ne prouverait pas que l'endpoint diffuse). Sans la diffusion par l'endpoint, aucune re-projection
/// n'atteint l'écran 2 → rouge. Anti « vert qui ment » : le baseline (slot École rendu) est asserté avant ;
/// un bUnit à doublure ne prouverait ni le store partagé, ni le client SignalR, ni la re-projection runtime.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSupprimerSlotTempsReelTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    private static bool CaseContientSlot(AngleSharp.Dom.IElement caseJour, string libelle)
        => caseJour.QuerySelectorAll("[data-testid='slot-case']")
            .Any(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Trim() == libelle);

    [Fact]
    public async Task Should_Retirer_le_slot_Ecole_de_la_case_du_16_06_du_second_ecran_sans_rechargement_When_l_ecran_1_supprime_le_slot()
    {
        // Given — UNE seule API distante réelle (store singleton partagé). Un slot « École » 08h30-16h30
        // pour Léa le mardi 16/06/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerSlot(api, "lea", "École", new DateTime(2026, 6, 16, 8, 30, 0), new DateTime(2026, 6, 16, 16, 30, 0));

        // Écran 2 (le « second écran ») = la grille réellement câblée à l'API, dans un TestContext distinct
        // (navigateur / DI séparés, client SignalR propre).
        using var ecran2 = new TestContext();
        var grille2 = GrilleRuntimeHarness.RendreGrille(ecran2, api, Mardi_16_06_2026);

        // … baseline : la case du 16/06 rend le slot « École ».
        Assert.True(CaseContientSlot(GrilleRuntimeHarness.CaseDuJour(grille2, "16/06"), "École"),
            "la case du 16/06 doit rendre le slot École au départ.");

        // When — écran 1 = le canal d'écriture réel (le POST exact que la dialog émet, prouvée en Sc.6) :
        // on supprime le slot par son identifiant stable, relu du store partagé.
        var client1 = GrilleRuntimeHarness.ClientVers(api);
        var idSlot = api.Services.GetRequiredService<ISlotRepository>()
            .AllSnapshots().Single(s => s.LieuId == "École").Id;

        // … pompe de diffusion : ré-émet la suppression (idempotente) jusqu'à ce que le push SignalR tombe
        // APRÈS l'établissement de la connexion long polling du second écran. La diffusion vient de
        // l'ENDPOINT (sur succès), jamais d'un appel direct au notificateur.
        using var diffusionContinue = new CancellationTokenSource();
        var pousseurDeDiffusion = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                await client1.PostAsJsonAsync("api/canal/supprimer-slot", new { SlotId = idSlot });
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — sans rechargement, le second écran voit la case du 16/06 ne plus rendre le slot École
            // (re-projection déclenchée par la diffusion temps réel de l'endpoint).
            grille2.WaitForAssertion(
                () => Assert.False(CaseContientSlot(GrilleRuntimeHarness.CaseDuJour(grille2, "16/06"), "École"),
                    "la case du 16/06 ne doit plus rendre le slot École après la diffusion."),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }

        // … et le second écran reflète l'état du store relu : le slot École en est absent.
        using var scope = api.Services.CreateScope();
        var slotsDuJour = scope.ServiceProvider.GetRequiredService<SlotsDuJourQuery>();
        Assert.DoesNotContain(slotsDuJour.Lister(new DateOnly(2026, 6, 16)), s => s.LieuId == "École");
    }
}
