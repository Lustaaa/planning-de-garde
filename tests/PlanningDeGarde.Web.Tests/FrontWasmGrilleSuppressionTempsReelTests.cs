using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.9 (🖥️ IHM, <c>@limite</c>) — CARACTÉRISATION : la suppression d'un
/// acteur se <b>propage en temps réel</b> (SignalR lecture seule) à un <b>second écran</b> affichant le
/// planning partagé, <b>sans rechargement</b>. Deux écrans câblés à la <b>MÊME API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/> unique → store singleton partagé, hub SignalR réel commun) : l'écran 1
/// est l'<b>écran de configuration</b> (Parent), l'écran 2 la <b>grille</b> (<see cref="PlanningPartage"/>).
///
/// Montage : grand-père (acteur tiers énuméré, rôle « Nounou » du scénario) garde le mardi 16/06/2026
/// (<b>surcharge</b>) ; le <b>cycle de fond</b> attribue parent-a (« Alice ») à toute la fenêtre. La surcharge
/// prime → la case du 16/06 porte « grand-père ». Quand le Parent supprime grand-père depuis l'écran de
/// configuration (<c>POST /api/canal/supprimer-acteur</c> → diffusion temps réel sur succès), le second écran
/// voit, sans rechargement : la case du 16/06 <b>retomber sur Parent A</b> (« Alice » — repli surcharge
/// orpheline → fond, filtre d'existence <c>Resolvable</c>) et la <b>légende dédoublonnée</b> ne <b>plus</b>
/// faire apparaître grand-père.
///
/// Caractérisation (early green ATTENDU) : compose des acquis — diffusion sur succès (Sc.1, handler notifie
/// <see cref="INotificateurPlanning"/>), repli surcharge orpheline → fond (Sc.2, backend), légende
/// dédoublonnée (s07), bouton + émission canal (Sc.6). Reste l'observation runtime de la propagation.
///
/// Convention anti-flake *TempsReel* : on attend l'<b>établissement DÉTERMINISTE</b> de la connexion via une
/// <b>pompe de diffusion idempotente</b> (le store est déjà muté, NotifierMiseAJour re-poussé en boucle jusqu'à
/// ce qu'un push tombe APRÈS l'établissement de la connexion long polling), jamais un délai fixe ; assertion
/// finale sous <see cref="BunitRenderedComponentExtensions.WaitForAssertion"/>. Isolation : un TestContext /
/// store / hub propres au test. Anti « vert qui ment » : le baseline « grand-père » est asserté avant ; un
/// bUnit à doublure ne prouverait ni le store partagé, ni le second client SignalR, ni la re-projection runtime.
/// </summary>
public sealed class FrontWasmGrilleSuppressionTempsReelTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public async Task Should_Faire_retomber_la_case_du_16_06_sur_Parent_A_et_retirer_grand_pere_de_la_legende_du_second_ecran_sans_rechargement_When_un_parent_supprime_grand_pere_depuis_l_ecran_de_configuration()
    {
        // Given — UNE seule API distante réelle (store singleton partagé). grand-père garde le 16/06
        // (surcharge) ; le cycle de fond (N=1, index 0 → parent-a) attribue « Alice » à toute la fenêtre.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "grand-pere", Mardi_16_06_2026, Mardi_16_06_2026);
        api.Services.GetRequiredService<IReferentielCycleDeFond>()
            .DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = "parent-a" }));

        // Écran 2 (le « second écran ») = la grille réellement câblée à l'API, dans un TestContext distinct
        // (navigateur / DI séparés, client SignalR propre).
        using var ecran2 = new TestContext();
        var grille2 = GrilleRuntimeHarness.RendreGrille(ecran2, api, Mardi_16_06_2026);

        // … baseline sur le second écran : la surcharge prime → la case du 16/06 porte « grand-père », et la
        // légende dédoublonnée le fait apparaître (aux côtés d'Alice, fond présent sur la fenêtre).
        Assert.Equal("grand-père", GrilleRuntimeHarness.CaseDuJour(grille2, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Contains(
            grille2.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "grand-père");

        // When — écran 1 = l'écran de configuration (Parent) câblé à la MÊME API : on supprime grand-père
        // (émission via le canal d'écriture HTTP réel → diffusion temps réel sur succès).
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.GetAttribute("data-acteur-id") == "grand-pere")
            .QuerySelector("[data-testid='bouton-supprimer']")!.Click());

        // … re-diffusion de fond idempotente (le store est déjà muté) pour que le push SignalR tombe forcément
        // APRÈS l'établissement de la connexion long polling du second écran, sans dépendre du timing.
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
            // Then — sans rechargement, le second écran voit la case du 16/06 retomber sur Parent A (« Alice »,
            // repli surcharge orpheline → fond) et la légende dédoublonnée ne fait plus apparaître grand-père.
            grille2.WaitForAssertion(
                () =>
                {
                    Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille2, "16/06")
                        .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.DoesNotContain(
                        grille2.FindAll("[data-testid='legende-entree']"),
                        e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "grand-père");
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
