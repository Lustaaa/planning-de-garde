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
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@nominal</c>) — depuis l'<b>écran de
/// configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>, DI réelle, API distante
/// réelle <see cref="ApiDistanteFactory"/>, store réel, diffusion SignalR réelle), un parent clique
/// le <b>bouton supprimer</b> d'un acteur tiers du foyer (« grand-père », rôle « Nounou » du scénario,
/// énuméré depuis le store ET présent dans la légende d'une période qu'il couvre). Sans rechargement :
/// <list type="bullet">
///   <item>grand-père <b>quitte la liste relue</b> des acteurs du foyer (ré-énumération du store réel) ;</item>
///   <item>un accusé <b>« Acteur supprimé »</b> s'affiche à part, sans bloquer (registre avertissement, D5) ;</item>
///   <item>la <b>légende dédoublonnée</b> du planning ne fait <b>plus apparaître</b> grand-père, via la
///         diffusion temps réel (filtre d'existence <c>Resolvable</c> côté projection — Sc.2/Sc.4).</item>
/// </list>
///
/// Chemin réel exercé : le bouton émet la commande via le <b>canal d'écriture HTTP réel</b>
/// (<c>POST /api/canal/supprimer-acteur</c>, règle 27 — aucune vue n'écrit le domaine en direct) → le
/// handler retire l'acteur du store ET déclenche la diffusion → l'écran ré-énumère le store (liste sans
/// grand-père) et la grille re-projette (légende sans grand-père), sans recharger la page.
///
/// Anti « vert qui ment » : si le bouton supprimer n'existait pas, si son <c>@onclick</c> était mort
/// (render mode), si l'endpoint de suppression manquait, ou si la légende n'appliquait pas le filtre
/// d'existence, grand-père resterait listé / légendé → rouge. Un bUnit à doublure ne prouverait ni la DI
/// réelle, ni le chemin HTTP d'écriture, ni la re-projection runtime de la légende.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigSupprimerActeurTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Retirer_grand_pere_de_la_liste_et_de_la_legende_avec_un_accuse_Acteur_supprime_sans_recharger_la_page_When_un_parent_clique_le_bouton_supprimer_depuis_l_ecran_de_configuration()
    {
        // Given — grand-père (acteur tiers du foyer, type « Nounou ») garde Léa le 30/06 : la grille
        // réellement câblée à l'API distante affiche une entrée de légende « grand-père ».
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "grand-pere", new DateTime(2026, 6, 30), new DateTime(2026, 6, 30));

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // … état initial : la légende fait apparaître « grand-père ».
        Assert.Contains(
            grille.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "grand-père");

        // … l'écran de configuration réellement câblé énumère ses acteurs DEPUIS LE STORE (GET HTTP
        // réel asynchrone) : grand-père figure dans la liste relue.
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        Assert.Contains(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "grand-père");

        // When — je clique le bouton supprimer de la ligne de grand-père (émission via le canal
        // d'écriture HTTP réel de l'API distante : POST /api/canal/supprimer-acteur).
        // Refonte s32 : la suppression se fait dans la MODAL ouverte au crayon de grand-père.
        ConfigActeursModalHarness.OuvrirEdition(this, config, "grand-pere");
        this.SurDispatcher(() => config.Find("[data-testid='bouton-supprimer']").Click());

        // Then (1) — sans rechargement (même instance rendue), grand-père quitte la liste relue.
        config.WaitForAssertion(
            () => Assert.DoesNotContain(
                config.FindAll("[data-testid='acteur-foyer']"),
                li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "grand-père"),
            TimeSpan.FromSeconds(10));

        // Then (2) — un accusé « Acteur supprimé » s'affiche à part, sans bloquer (registre avertissement).
        Assert.Contains("Acteur supprimé", config.Find("[data-testid='accuse-suppression']").TextContent);

        // Then (3) — diffusion temps réel : la légende du planning ne fait plus apparaître grand-père,
        // sans rechargement. On ré-émet la diffusion en boucle de fond (idempotente — le store est déjà
        // muté) pour qu'un push tombe forcément APRÈS l'établissement de la connexion SignalR (long
        // polling), sans dépendre du timing (convention anti-flake *TempsReel*).
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
            grille.WaitForAssertion(
                () => Assert.DoesNotContain(
                    grille.FindAll("[data-testid='legende-entree']"),
                    e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "grand-père"),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }
}
