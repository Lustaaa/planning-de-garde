using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.4 (🖥️ IHM, <c>@limite</c>) — <b>collision de couleur assumée</b>
/// (règle 17 : la lisibilité repose sur le <b>nom + légende</b>, pas la couleur seule). La grille réelle
/// affiche parent-a (Alice, bleu) le 14/07 et parent-b (Bruno, orange) le 15/07. Depuis l'écran de
/// configuration réellement câblé (<see cref="ConfigurationFoyer"/>), parent-b est recolorié en <b>bleu</b>
/// — la même couleur qu'Alice. <b>Sans rechargement</b>, les cases du 14/07 et du 15/07 sont toutes deux
/// bleues mais <b>restent distinguables par leur nom</b> (« Alice » vs « Bruno »), et la légende liste
/// <b>deux</b> entrées bleues nommées distinctement — la dédup de légende se fait par <b>identifiant
/// stable</b> (s07), jamais par couleur, donc deux ids de même teinte donnent deux entrées.
///
/// Compose le chemin existant (recoloriage Sc.2 + dédup-par-id s07) : aucun code de production neuf. Le
/// baseline orange de parent-b est asserté avant le recoloriage pour que la transition orange→bleu soit
/// réellement observée (pas un faux-vert). Pas un bUnit à doublure (il ne prouve ni le rendu réel, ni la
/// chaîne d'édition réelle, ni la diffusion temps réel).
/// </summary>
public sealed class FrontWasmConfigCollisionCouleurTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Rendre_les_cases_du_14_07_et_du_15_07_2026_toutes_deux_bleues_distinguables_par_Alice_et_Bruno_avec_deux_entrees_de_legende_bleues_nommees_distinctement_When_parent_b_est_recolorie_vers_la_couleur_de_parent_a()
    {
        // Given — la grille réelle affiche, à la semaine du lundi 13/07/2026, parent-a (Alice, bleu) le
        // mardi 14/07 et parent-b (Bruno, orange) le mercredi 15/07.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 14), new DateTime(2026, 7, 14));
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 7, 15), new DateTime(2026, 7, 15));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : 14/07 = Alice bleu, 15/07 = Bruno orange, légende = deux couleurs distinctes.
        var case14Initiale = GrilleRuntimeHarness.CaseDuJour(grille, "14/07");
        Assert.Equal("Alice", case14Initiale.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", case14Initiale.GetAttribute("data-couleur"));
        var case15Initiale = GrilleRuntimeHarness.CaseDuJour(grille, "15/07");
        Assert.Equal("Bruno", case15Initiale.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", case15Initiale.GetAttribute("data-couleur"));
        Assert.Equal(2, grille.FindAll("[data-testid='legende-entree']").Count);

        // When — depuis l'écran de configuration réellement câblé, je recolorie parent-b en « bleu » (la
        // même couleur qu'Alice) et j'enregistre (émission via le canal d'écriture HTTP réel).
        var config = RenderComponent<ConfigurationFoyer>();
        config.Find("select.form-select").Change("parent-b");
        config.Find("[data-testid='champ-couleur']").Change("bleu");
        config.Find("form").Submit();

        // … re-diffusion de fond idempotente (le store est déjà muté) pour que le push SignalR tombe
        // après l'établissement de la connexion long polling vers le TestServer, sans dépendre du timing.
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
            // Then — sans rechargement, les cases du 14/07 et du 15/07 sont toutes deux bleues mais
            // distinguables par le nom (Alice vs Bruno), et la légende liste deux entrées bleues nommées
            // distinctement (collision assumée — dédup par id stable, pas par couleur).
            grille.WaitForAssertion(
                () =>
                {
                    var case14 = GrilleRuntimeHarness.CaseDuJour(grille, "14/07");
                    Assert.Equal("bleu", case14.GetAttribute("data-couleur"));
                    Assert.Equal("Alice", case14.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

                    var case15 = GrilleRuntimeHarness.CaseDuJour(grille, "15/07");
                    Assert.Equal("bleu", case15.GetAttribute("data-couleur"));
                    Assert.Equal("Bruno", case15.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

                    // Légende : deux entrées, toutes deux bleues, nommées distinctement « Alice » / « Bruno ».
                    var entrees = grille.FindAll("[data-testid='legende-entree']");
                    Assert.Equal(2, entrees.Count);
                    Assert.All(entrees, e => Assert.Equal("bleu", e.GetAttribute("data-couleur")));
                    var noms = entrees.Select(e => e.QuerySelector(".legende-nom")!.TextContent.Trim()).ToList();
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
