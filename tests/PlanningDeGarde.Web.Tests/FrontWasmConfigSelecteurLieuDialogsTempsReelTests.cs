using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 27 — S6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, 100 % temps réel SignalR) : le <b>sélecteur de
/// lieu</b> des dialogs « Poser un slot » ET « Définir un transfert » de l'écran de planning reflète le
/// <b>référentiel d'activités configuré</b> (store vivant, GET /api/foyer/activites, s35 — jamais la liste en
/// dur <c>Foyer.Lieux</c>) : quand un parent AJOUTE l'activité « piscine » depuis un second écran (config), le
/// sélecteur du premier écran le propose <b>sans rechargement</b> (propagé par la diffusion SignalR) ; quand
/// il SUPPRIME « nounou », le sélecteur ne le propose plus.
///
/// Deux écrans = deux <see cref="Bunit.TestContext"/> (navigateurs / DI séparés) câblés à la MÊME API distante
/// réelle (<see cref="ApiDistanteFactory"/> unique → store singleton partagé, hub SignalR réel commun). Rempart
/// anti « vert qui ment » : l'état de base est asserté AVANT (le sélecteur propose « nounou », pas « piscine ») ;
/// tant que les dialogs itèrent la liste EN DUR <c>Foyer.Lieux</c> (non reliée au store, non éditable), l'ajout /
/// la suppression en config n'atteint jamais le sélecteur → rouge. Test multi-clients SignalR → catégorie du
/// flake P1 catalogué (re-run isolé si rouge sur la convergence, dette docs/BACKLOG.md).
/// </summary>
public sealed class FrontWasmConfigSelecteurLieuDialogsTempsReelTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    /// <summary>Câble un écran de configuration (contexte bUnit) sur l'API distante réelle : client HTTP réel,
    /// session (Parent) et redirection du hub SignalR vers le TestServer (long polling) pour la diffusion temps réel.</summary>
    private static void ConfigurerEcranConfig(Bunit.TestContext ctx, ApiDistanteFactory api)
    {
        ctx.Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        ctx.Services.AddSingleton(new SessionPlanning());
        ctx.Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
    }

    /// <summary>Libellés de lieu proposés par le sélecteur de lieu de la dialog ouverte (champ-lieu).</summary>
    private static IReadOnlyList<string> LieuxProposes(IRenderedComponent<PlanningPartage> grille)
        => grille.FindAll("[data-testid='champ-lieu'] option")
            .Select(o => o.TextContent.Trim())
            .Where(t => t.Length > 0 && t != "— choisir —")
            .ToList();

    /// <summary>Ouvre (idempotent, robuste aux re-renders async du hub) une dialog du menu clic-case sur la
    /// case du mardi 16/06, via l'action ciblée, et attend que la dialog soit rendue.</summary>
    private void OuvrirDialog(IRenderedComponent<PlanningPartage> grille, string actionTestId, string dialogTestId)
        => grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find($"[data-testid='{actionTestId}']").Click());
                Assert.NotEmpty(grille.FindAll($"[data-testid='{dialogTestId}']"));
            },
            TimeSpan.FromSeconds(10));

    [Fact]
    public async Task Le_selecteur_de_lieu_des_dialogs_du_planning_reflete_sans_rechargement_un_lieu_ajoute_puis_supprime_depuis_l_ecran_de_config()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, seed lieux école/domiciles/nounou).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026); // écran 1 : planning
        using var ecranConfig = new TestContext();
        ConfigurerEcranConfig(ecranConfig, api);
        var config = ecranConfig.RenderComponent<ConfigurationFoyer>();               // écran 2 : config
        config.WaitForState(() => config.FindAll("[data-testid='activite-foyer']").Count > 0, TimeSpan.FromSeconds(10));

        // … écran 1 : le parent ouvre la dialog « Poser un slot » depuis la case du 16/06.
        OuvrirDialog(grille, "action-poser-slot", "dialog-poser-slot");

        // … état de base (anti faux-vert) : le sélecteur propose « nounou » (seed) mais PAS « piscine ».
        Assert.Contains("nounou", LieuxProposes(grille));
        Assert.DoesNotContain("piscine", LieuxProposes(grille));

        // Re-diffusion de fond idempotente : le push SignalR tombe forcément APRÈS l'établissement des
        // connexions long polling des deux écrans, sans dépendre du timing (anti-flake, pattern s20 Sc.6).
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusion = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusion.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusion.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // When — le parent AJOUTE l'activité « piscine » via la MODAL de config (patron s35, canal d'écriture
            // HTTP réel : POST /api/canal/ajouter-activite → store partagé muté + diffusion temps réel déclenchée).
            config.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-activite']").Click());
            config.WaitForElement("[data-testid='dialog-activite']", TimeSpan.FromSeconds(10));
            config.SurDispatcher(() => config.Find("[data-testid='champ-libelle-activite']").Change("piscine"));
            config.SurDispatcher(() => config.Find("#form-activite").Submit());
            config.WaitForAssertion(
                () => Assert.Contains(config.FindAll("[data-testid='activite-foyer']"),
                    li => li.QuerySelector(".role-libelle")!.TextContent.Trim() == "piscine"),
                TimeSpan.FromSeconds(10));

            // Then — sans rechargement, le sélecteur de lieu de la dialog du PREMIER écran propose « piscine »,
            // propagé par la diffusion SignalR (lecture du store vivant, plus la liste en dur Foyer.Lieux).
            grille.WaitForAssertion(
                () => Assert.Contains("piscine", LieuxProposes(grille)),
                TimeSpan.FromSeconds(15));

            // When — le parent SUPPRIME l'activité « nounou » via la MODAL (crayon → « Supprimer cette activité »).
            config.SurDispatcher(() => config.FindAll("[data-testid='activite-foyer']")
                .Single(li => li.QuerySelector(".role-libelle")!.TextContent.Trim() == "nounou")
                .QuerySelector("[data-testid='crayon-activite']")!
                .Click());
            config.WaitForElement("[data-testid='dialog-activite']", TimeSpan.FromSeconds(10));
            config.SurDispatcher(() => config.Find("[data-testid='bouton-supprimer-activite']").Click());

            // Then — sans rechargement, le sélecteur de lieu de la dialog ne propose plus « nounou ».
            grille.WaitForAssertion(
                () => Assert.DoesNotContain("nounou", LieuxProposes(grille)),
                TimeSpan.FromSeconds(15));

            // Et — les DEUX dialogs convergent : « Définir un transfert » lit le MÊME référentiel vivant.
            this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] [data-testid='dialog-annuler']").Click());
            OuvrirDialog(grille, "action-definir-transfert", "dialog-definir-transfert");
            grille.WaitForAssertion(
                () =>
                {
                    Assert.Contains("piscine", LieuxProposes(grille));
                    Assert.DoesNotContain("nounou", LieuxProposes(grille));
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusion.Cancel();
            await pousseur;
        }
    }
}
