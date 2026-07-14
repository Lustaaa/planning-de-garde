using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 42 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la carte « Aujourd'hui » est LECTURE non
/// gatée (l'<b>Invité VOIT</b> la carte, aucune action d'écriture atteignable) ET converge en TEMPS RÉEL
/// sur une écriture pertinente, SANS rechargement, via le canal SignalR de LECTURE SEULE (s20). La grille
/// et sa carte sont réellement câblées à l'API distante (projection réelle, diffusion SignalR réelle).
///
/// Anti « vert qui ment » : la convergence est prouvée sur l'app réellement câblée (relecture de la grille
/// déclenchée par la diffusion SignalR réelle, carte reprojetée client) — pas une doublure de transport.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmCarteAujourdhuiGatingConvergenceTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;

    [Fact]
    public void L_invite_voit_la_carte_en_lecture_seule_sans_action_d_ecriture()
    {
        // Given — un responsable résolu le jour courant (parent-a « Alice »), grille câblée réelle.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Aujourdhui, Aujourdhui);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — l'identité effective bascule en Invité (consultation seule) et la vue est re-rendue.
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // Then — l'Invité VOIT la carte (lecture non gatée) avec le « qui » résolu, et AUCUN contrôle
        // d'écriture n'y est atteignable (aucun bouton dans la carte).
        var carte = grille.Find("[data-testid='carte-aujourdhui']");
        Assert.Equal("Alice", carte.QuerySelector("[data-testid='carte-qui']")!.TextContent.Trim());
        Assert.Empty(carte.QuerySelectorAll("button"));
    }

    [Fact]
    public async Task La_carte_converge_sur_une_ecriture_pertinente_sans_rechargement()
    {
        // Given — grille câblée réelle, aucun responsable le jour courant → carte « Personne assignée ».
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        Assert.Equal(
            "Personne assignée",
            grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-qui']").TextContent.Trim());

        // When — une écriture pertinente survient (période affectée à parent-a le jour courant), puis la
        // diffusion SignalR RÉELLE est poussée en boucle de fond (idempotente : le store est déjà muté),
        // pour qu'un push tombe APRÈS l'établissement de la connexion (anti-flake timing, pattern harnais).
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Aujourdhui, Aujourdhui);
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
            // Then — SANS rechargement (même instance rendue), la carte CONVERGE vers « Alice » — la
            // relecture est déclenchée par la diffusion SignalR de lecture seule, jamais un reload de page.
            grille.WaitForAssertion(
                () => Assert.Equal(
                    "Alice",
                    grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-qui']").TextContent.Trim()),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }
}
