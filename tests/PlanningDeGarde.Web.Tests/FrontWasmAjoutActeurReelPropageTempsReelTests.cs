using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 19 — Sc.7 (🖥️ @ihm, dernier) — Acceptation de NIVEAU RUNTIME, 100 % temps réel SignalR :
/// l'ajout d'un acteur RÉEL depuis un écran propage au <b>second écran</b> — grille, légende ET
/// sélecteurs des dialogs — <b>sans rechargement</b>, et <b>aucun acteur fictif</b> (« Parent A /
/// Parent B ») n'apparaît avant ni après la propagation.
///
/// <b>Deux écrans</b> (deux <see cref="Bunit.TestContext"/> = deux navigateurs / DI séparées) câblés à la
/// <b>MÊME API distante réelle</b> (<see cref="ApiDistanteFactory"/> unique → store singleton partagé,
/// projection réelle, hub SignalR réel commun). Écran 1 ajoute « Carla » via le <b>canal d'écriture
/// HTTP réel</b> (<c>POST /api/canal/ajouter-acteur</c>, règle 27) puis lui affecte le 15/07 ; la
/// diffusion temps réel (notificateur réel) fait réagir l'écran 2 <b>sans second render manuel</b>.
///
/// Anti « vert qui ment » : baseline (15/07 sans nom, aucun fantôme) asserté sur les DEUX écrans avant
/// l'ajout ; la convergence vers « Carla » est donc réellement observée. Compose les acquis (store
/// partagé Sc.5 + diffusion temps réel + résolution sur l'identifiant stable) : aucun code de production
/// neuf attendu (GREEN minimal de cohérence). Test multi-clients SignalR → catégorie du flake P2
/// catalogué (re-run ciblé en cas de rouge isolé sur la convergence, dette docs/BACKLOG.md).
/// </summary>
public sealed class FrontWasmAjoutActeurReelPropageTempsReelTests : TestContext
{
    private sealed record ActeurVue(string Id, string Nom, string Couleur);

    private static readonly DateTime Lundi_13_07_2026 = new(2026, 7, 13);

    [Fact]
    public async Task Should_Propager_l_acteur_reel_Carla_a_la_grille_la_legende_et_le_selecteur_du_second_ecran_sans_rechargement_ni_fantome_When_il_est_ajoute_puis_affecte_depuis_le_premier_ecran()
    {
        // Given — UNE seule API distante réelle (store singleton partagé). Deux écrans/grilles distincts.
        using var api = new ApiDistanteFactory();
        var grille1 = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_13_07_2026);
        using var ecran2 = new TestContext();
        var grille2 = GrilleRuntimeHarness.RendreGrille(ecran2, api, Lundi_13_07_2026);

        // … baseline asserté sur les DEUX écrans : le 15/07 n'a aucun responsable et AUCUN fantôme nulle part.
        foreach (var g in new[] { grille1, grille2 })
        {
            Assert.Null(GrilleRuntimeHarness.CaseDuJour(g, "15/07").QuerySelector("[data-testid='nom-responsable']"));
            Assert.DoesNotContain("Parent A", g.Markup);
            Assert.DoesNotContain("Parent B", g.Markup);
        }

        // When — l'écran 1 ajoute « Carla » (acteur réel) via le canal d'écriture HTTP réel, puis lui
        // affecte le mercredi 15/07 (sur son identifiant stable neuf, relu du store partagé).
        using var client = GrilleRuntimeHarness.ClientVers(api);
        var ajout = await client.PostAsJsonAsync("api/canal/ajouter-acteur", new { Nom = "Carla", Couleur = "rose" });
        Assert.True(ajout.IsSuccessStatusCode);

        var declares = await client.GetFromJsonAsync<List<ActeurVue>>("api/foyer/acteurs") ?? new();
        var carlaId = declares.Single(a => a.Nom == "Carla").Id;

        var affectation = await client.PostAsJsonAsync("api/canal/affecter-periode", new
        {
            ResponsableId = carlaId,
            Debut = new DateTime(2026, 7, 15),
            Fin = new DateTime(2026, 7, 15),
        });
        Assert.True(affectation.IsSuccessStatusCode);

        // … re-diffusion de fond idempotente (store déjà muté) pour que le push SignalR tombe forcément
        // APRÈS l'établissement des deux connexions long polling, sans dépendre du timing.
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
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
            // Then — sans rechargement, les DEUX écrans voient « Carla » (rose) au 15/07 ET en légende,
            // et AUCUN fantôme n'apparaît.
            foreach (var g in new[] { grille1, grille2 })
            {
                g.WaitForAssertion(
                    () =>
                    {
                        var caseCarla = GrilleRuntimeHarness.CaseDuJour(g, "15/07");
                        Assert.Equal("Carla", caseCarla.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                        Assert.Equal("rose", caseCarla.GetAttribute("data-couleur"));

                        var legende = g.FindAll("[data-testid='legende-entree']")
                            .Select(e => e.QuerySelector(".legende-nom")!.TextContent.Trim())
                            .ToList();
                        Assert.Contains("Carla", legende);

                        Assert.DoesNotContain("Parent A", g.Markup);
                        Assert.DoesNotContain("Parent B", g.Markup);
                    },
                    TimeSpan.FromSeconds(15));
            }
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }

        // … et le SÉLECTEUR du second écran propose « Carla » (lu du store partagé), sans fantôme : on
        // ouvre la dialog d'affectation depuis une case de l'écran 2.
        grille2.WaitForAssertion(
            () =>
            {
                grille2.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille2, "14/07").Click());
                grille2.SurDispatcher(() => grille2.Find("[data-testid='action-affecter-periode']").Click());
                Assert.NotEmpty(grille2.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));

        grille2.WaitForAssertion(
            () =>
            {
                var options = grille2.FindAll("[data-testid='champ-responsable'] option").ToList();
                Assert.Contains(options, o => (o.GetAttribute("value") ?? "") == carlaId);
                Assert.Contains(options, o => o.TextContent.Trim() == "Carla");
                Assert.DoesNotContain(options, o => o.TextContent.Trim() is "Parent A" or "Parent B");
            },
            TimeSpan.FromSeconds(10));
    }
}
