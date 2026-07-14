using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 43 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le panneau « À venir » CONVERGE en TEMPS
/// RÉEL sur une écriture pertinente (période affectée sur un jour à venir), SANS rechargement, via la
/// REPROJECTION CLIENT de la grille rafraîchie par la diffusion SignalR de LECTURE SEULE (s20) — AUCUN GET
/// dédié sur push (garde anti-amplification flake s42). La grille et son panneau sont réellement câblés à
/// l'API distante (projection réelle, diffusion SignalR réelle).
///
/// Anti « vert qui ment » : la convergence est prouvée sur l'app réellement câblée (relecture de la grille
/// déclenchée par la diffusion SignalR réelle, panneau reprojeté client), pas une doublure de transport. Le
/// panneau ne s'abonne PAS séparément et n'émet AUCUN GET propre : il se recalcule depuis la grille déjà en
/// main — même canal que la carte du jour (s42).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmAVenirConvergenceTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026
    private static readonly DateTime Mardi_30_06 = new(2026, 6, 30);                     // jour à venir ciblé

    [Fact]
    public async Task Le_panneau_a_venir_converge_sur_une_ecriture_pertinente_sans_rechargement()
    {
        // Given — grille câblée réelle, aucun responsable sur le 30/06 → ligne à-venir « Personne assignée ».
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        Assert.NotNull(Ligne30Juin(grille).QuerySelector("[data-testid='a-venir-qui-neutre']"));

        // When — une écriture pertinente survient (période parent-a le 30/06), puis la diffusion SignalR RÉELLE
        // est poussée en boucle de fond (idempotente : le store est déjà muté), pour qu'un push tombe APRÈS
        // l'établissement de la connexion (anti-flake timing, pattern harnais).
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Mardi_30_06, Mardi_30_06);
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
            // Then — SANS rechargement (même instance rendue), la ligne 30/06 du panneau CONVERGE vers « Alice » —
            // la grille est relue via la diffusion SignalR de lecture seule et le panneau se REPROJETTE client.
            grille.WaitForAssertion(
                () => Assert.Equal(
                    "Alice",
                    Ligne30Juin(grille).QuerySelector("[data-testid='a-venir-responsable']")!.TextContent.Trim()),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }

    private static AngleSharp.Dom.IElement Ligne30Juin(IRenderedComponent<PlanningDeGarde.Web.Components.Pages.PlanningPartage> grille)
        => grille.Find("[data-testid='panneau-a-venir']")
            .QuerySelectorAll("[data-testid='a-venir-jour']")
            .Single(j => j.GetAttribute("data-date") == "2026-06-30");
}
