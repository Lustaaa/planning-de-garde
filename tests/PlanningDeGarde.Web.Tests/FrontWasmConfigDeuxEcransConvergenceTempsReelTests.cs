using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ IHM, <c>@limite</c>) — VOLET RUNTIME : <b>deux écrans</b>
/// (deux parents) affichent la <b>même grille partagée du foyer</b> où la case du 15/07/2026 est
/// affectée à parent-b (« Bruno »). Les deux grilles sont rendues dans <b>deux TestContext distincts</b>
/// (deux navigateurs / DI séparées) mais câblées à la <b>MÊME API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/> unique → store singleton <c>ConfigurationFoyerEnMemoire</c> partagé,
/// projection <c>GrilleAgendaQuery</c> inchangée, hub SignalR réel commun).
///
/// Le premier écran renomme parent-b en « Bruno M. » puis, juste après, le second le renomme en
/// « Bruno Martin » — chacun via le <b>canal d'écriture HTTP réel</b> (<c>POST /api/canal/editer-acteur</c>,
/// règle 27). La <b>dernière écriture gagne</b> (le store écrase, pas de version ni de rejet, décision CP) :
/// les <b>DEUX</b> grilles convergent vers « Bruno Martin » dans la case du 15/07 ET en légende, propagé par
/// la <b>diffusion temps réel</b>, sans rechargement.
///
/// Anti « vert qui ment » : le baseline « Bruno » est asserté sur les DEUX grilles avant les renommages, pour
/// que la convergence vers « Bruno Martin » soit réellement observée (pas un faux-vert). Un bUnit à doublure
/// ne prouverait ni la mémoire partagée du store singleton, ni les deux clients SignalR, ni le chemin
/// d'écriture HTTP réel. Compose le chemin existant (renommage Sc.1 + store partagé) : aucun code de
/// production neuf attendu (GREEN minimal).
/// </summary>
public sealed class FrontWasmConfigDeuxEcransConvergenceTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Faire_converger_les_deux_grilles_vers_Bruno_Martin_dans_la_case_du_15_07_2026_et_en_legende_sans_rechargement_ni_rejet_When_un_premier_ecran_renomme_parent_b_en_Bruno_M_puis_un_second_le_renomme_en_Bruno_Martin()
    {
        // Given — UNE seule API distante réelle (store singleton partagé) ; la case du mercredi 15/07/2026
        // est affectée à parent-b (« Bruno »). Deux écrans/grilles distincts l'observent.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 7, 15), new DateTime(2026, 7, 15));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);

        // Écran 1 = ce TestContext ; écran 2 = un second TestContext (navigateur / DI séparés), câblé à la
        // MÊME api → même store partagé côté serveur, deux clients SignalR distincts.
        var grille1 = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);
        using var ecran2 = new TestContext();
        var grille2 = GrilleRuntimeHarness.RendreGrille(ecran2, api, lundi_13_07_2026);

        // … baseline asserté sur les DEUX grilles : la case du 15/07 et la légende portent « Bruno ».
        AssertNomDans(grille1, "Bruno");
        AssertNomDans(grille2, "Bruno");

        // When — le premier écran renomme parent-b en « Bruno M. » et enregistre (canal d'écriture HTTP réel)…
        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon (plus de sélecteur d'acteur inline).
        var config1 = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.OuvrirEdition(this, config1, "parent-b");
        this.SurDispatcher(() => config1.Find("[data-testid='champ-nom']").Change("Bruno M."));
        this.SurDispatcher(() => config1.Find("#form-edition").Submit());

        // … puis, juste après, le second écran le renomme en « Bruno Martin » et enregistre (même store).
        var config2 = ecran2.RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.OuvrirEdition(ecran2, config2, "parent-b");
        config2.SurDispatcher(() => config2.Find("[data-testid='champ-nom']").Change("Bruno Martin"));
        config2.SurDispatcher(() => config2.Find("#form-edition").Submit());

        // … re-diffusion de fond idempotente (le store est déjà muté à « Bruno Martin ») pour que le push
        // SignalR tombe forcément APRÈS l'établissement des deux connexions long polling, sans dépendre du
        // timing. Diffuse sur les DEUX clients (notificateur partagé de l'unique api).
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
            // Then — la dernière écriture gagne : sans rechargement, les DEUX grilles convergent vers
            // « Bruno Martin » dans la case du 15/07 ET en légende, propagé par la diffusion temps réel ;
            // aucune édition rejetée (pas de conflit, pas de version).
            grille1.WaitForAssertion(() => AssertNomDans(grille1, "Bruno Martin"), TimeSpan.FromSeconds(15));
            grille2.WaitForAssertion(() => AssertNomDans(grille2, "Bruno Martin"), TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }

    /// <summary>Asserte que la case du 15/07 ET l'entrée de légende d'une grille portent <paramref name="nom"/>.</summary>
    private static void AssertNomDans(IRenderedComponent<PlanningPartage> grille, string nom)
    {
        Assert.Equal(nom, GrilleRuntimeHarness.CaseDuJour(grille, "15/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal(nom, entree.QuerySelector(".legende-nom")!.TextContent.Trim());
    }
}
