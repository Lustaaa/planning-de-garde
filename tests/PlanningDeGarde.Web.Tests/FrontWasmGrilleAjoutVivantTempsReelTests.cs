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
/// Acceptation de NIVEAU RUNTIME du Sc.4 (🖥️ IHM, <c>@limite</c>) — 100 % runtime SignalR, 0 backend.
/// La grille réellement câblée est rendue (Alice seule responsable le 29/06). Un <b>autre acteur</b>
/// affecte Bruno le jeudi 02/07 via le <b>canal d'écriture HTTP réel</b> de l'API distante, puis la
/// diffusion est émise sur le <b>hub SignalR réel</b> (par le notificateur réel <c>INotificateurPlanning</c>
/// → <c>SignalRNotificateurPlanning</c>, exactement comme en production). Le front, connecté au hub réel
/// (redirigé vers le TestServer), <b>réagit sans rechargement</b> : la case du 02/07 affiche « Bruno »
/// et la légende gagne une seconde entrée.
///
/// Anti « vert qui ment » : aucune re-render manuelle, aucun second <c>RenderComponent</c> — c'est la
/// même instance rendue qui se réactualise à l'arrivée de l'évènement diffusé. Si le câblage hub→reload
/// était mort, la case du 02/07 resterait sans nom → rouge. Un bUnit à doublure ne prouverait pas ce
/// chemin temps réel réel (négociation + transport + re-projection HTTP).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmGrilleAjoutVivantTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Faire_apparaitre_Bruno_le_02_07_2026_et_une_seconde_entree_de_legende_sans_rechargement_When_une_periode_affectee_a_Bruno_est_diffusee_sur_le_canal_de_lecture()
    {
        // Given — la grille réellement câblée affiche Alice seule responsable le 29/06 (store réel).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 6, 29), new DateTime(2026, 6, 29));

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // … l'état initial : Alice nommée, légende = 1 entrée, et le 02/07 n'a encore aucun nom.
        Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Null(GrilleRuntimeHarness.CaseDuJour(grille, "02/07").QuerySelector("[data-testid='nom-responsable']"));

        // When — un autre acteur affecte Bruno (parent-b) le jeudi 02/07 via le canal d'écriture réel …
        using var clientAutreActeur = GrilleRuntimeHarness.ClientVers(api);
        var reponse = await clientAutreActeur.PostAsJsonAsync("api/canal/affecter-periode", new
        {
            ResponsableId = "parent-b",
            Debut = new DateTime(2026, 7, 2),
            Fin = new DateTime(2026, 7, 2),
        });
        Assert.True(reponse.IsSuccessStatusCode);

        // … et la diffusion est émise sur le hub réel par le notificateur réel (comme en production).
        // La connexion SignalR du front (long polling vers le TestServer) s'établit de façon asynchrone :
        // on ré-émet la diffusion en boucle de fond (idempotente) — découplée des re-renders — pour
        // qu'un push tombe forcément APRÈS l'établissement de la connexion, sans dépendre de son timing.
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
            // Then — sans rechargement de page (même instance rendue, aucun second render), la case du
            // 02/07 affiche « Bruno » et la légende passe à deux entrées.
            grille.WaitForAssertion(
                () =>
                {
                    var caseBruno = GrilleRuntimeHarness.CaseDuJour(grille, "02/07");
                    Assert.Equal("Bruno", caseBruno.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Equal("orange", caseBruno.GetAttribute("data-couleur"));

                    var noms = grille.FindAll("[data-testid='legende-entree']")
                        .Select(e => e.QuerySelector(".legende-nom")!.TextContent.Trim())
                        .ToList();
                    Assert.Equal(2, noms.Count);
                    Assert.Contains("Alice", noms);
                    Assert.Contains("Bruno", noms);
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
