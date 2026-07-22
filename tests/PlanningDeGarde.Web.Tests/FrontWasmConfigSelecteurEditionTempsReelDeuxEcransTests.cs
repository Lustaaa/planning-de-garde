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
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, 100 % temps réel SignalR) : le
/// <b>sélecteur d'édition</b> de l'écran de configuration (onglet « Acteurs ») est <b>cohérent</b> avec la
/// source unifiée (il propose exactement les acteurs déclarés du store, id stable — comme les dialogs et
/// la grille) <b>et</b> le temps réel est préservé : quand un acteur est ajouté depuis un <b>second
/// écran</b> (store partagé), le sélecteur d'édition du <b>premier écran</b> reflète le changement
/// <b>sans rechargement</b>, propagé par la <b>diffusion SignalR</b>.
///
/// Deux écrans = deux <see cref="Bunit.TestContext"/> (navigateurs / DI séparés) câblés à la MÊME API
/// distante réelle (<see cref="ApiDistanteFactory"/> unique → store singleton partagé, hub SignalR réel
/// commun). Rempart anti « vert qui ment » : la cohérence de base est assertée AVANT l'ajout ; tant que
/// l'écran de configuration n'écoute PAS le hub (aucune ré-énumération à la diffusion), le sélecteur du
/// premier écran ignore « Carla » → rouge. Test multi-clients SignalR → catégorie du flake P2 catalogué
/// (re-run ciblé en cas de rouge isolé sur la convergence, dette docs/BACKLOG.md).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigSelecteurEditionTempsReelDeuxEcransTests : TestContext
{
    /// <summary>Câble un écran de configuration (contexte bUnit) sur l'API distante réelle : client HTTP
    /// réel, session (Parent), et redirection du hub SignalR vers le TestServer (long polling) pour observer
    /// la diffusion temps réel au runtime.</summary>
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

    /// <summary>Identifiants stables (triés) énumérés par la table de l'écran de configuration (refonte s32 :
    /// la table de lecture remplace le sélecteur d'édition inline ; le crayon de chaque ligne porte l'id).</summary>
    private static IReadOnlyList<string> IdsSelecteurEdition(IRenderedComponent<ConfigurationFoyer> config)
        => config.FindAll("[data-testid='acteur-foyer']")
            .Select(li => li.GetAttribute("data-acteur-id") ?? "")
            .Where(v => v.Length > 0)
            .OrderBy(id => id)
            .ToList();

    private static IReadOnlyList<string> SourceUnifiee(ApiDistanteFactory api)
        => api.Services.GetRequiredService<IEnumerationActeursFoyer>()
            .EnumererActeurs().OrderBy(id => id).ToList();

    [Fact]
    public async Task Le_selecteur_d_edition_config_du_premier_ecran_reflete_sans_rechargement_un_acteur_ajoute_depuis_le_second_ecran_et_reste_coherent_avec_la_source_unifiee()
    {
        // Given — UNE seule API distante réelle (store singleton partagé). Deux écrans de configuration
        // distincts (deux DI), l'onglet « Acteurs » actif par défaut sur chacun.
        using var api = new ApiDistanteFactory();
        ConfigurerEcranConfig(this, api);
        using var ecran2 = new TestContext();
        ConfigurerEcranConfig(ecran2, api);

        var config1 = RenderComponent<ConfigurationFoyer>();
        config1.WaitForState(() => config1.FindAll("[data-testid='acteur-foyer']").Count > 0, TimeSpan.FromSeconds(10));
        var config2 = ecran2.RenderComponent<ConfigurationFoyer>();
        config2.WaitForState(() => config2.FindAll("[data-testid='acteur-foyer']").Count > 0, TimeSpan.FromSeconds(10));

        // … cohérence de base (anti faux-vert) : la table du 1ᵉʳ écran énumère EXACTEMENT les acteurs déclarés
        // du store unifié (même source que dialogs + grille), et « Carla » n'y figure pas.
        Assert.Equal(SourceUnifiee(api), IdsSelecteurEdition(config1));
        Assert.DoesNotContain(
            config1.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla");

        // When — un parent ajoute « Carla » (rose) depuis le SECOND écran via la MODAL d'ajout (refonte s32 ;
        // canal d'écriture HTTP réel : POST /api/foyer/acteurs → store partagé muté + diffusion).
        ConfigActeursModalHarness.OuvrirAjout(ecran2, config2);
        config2.SurDispatcher(() => config2.Find("[data-testid='champ-nom-ajout']").Change("Carla"));
        config2.SurDispatcher(() => config2.Find("[data-testid='pastille-couleur-ajout-rose']").Click()); // palette (Sc.6)
        config2.SurDispatcher(() => config2.Find("#form-ajout").Submit());
        config2.WaitForAssertion(
            () => Assert.Contains(config2.FindAll("[data-testid='acteur-foyer']"),
                li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla"),
            TimeSpan.FromSeconds(10));

        // … re-diffusion de fond idempotente (store déjà muté) pour que le push SignalR tombe forcément
        // APRÈS l'établissement des deux connexions long polling, sans dépendre du timing (anti-flake).
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
            // Then — sans rechargement (même instance rendue), le sélecteur d'édition du PREMIER écran
            // reflète « Carla », propagé par la diffusion SignalR, ET reste strictement cohérent avec la
            // source unifiée (mêmes identifiants stables que le store, dialogs et grille).
            config1.WaitForAssertion(
                () =>
                {
                    Assert.Contains(
                        config1.FindAll("[data-testid='acteur-foyer']"),
                        li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla");
                    Assert.Equal(SourceUnifiee(api), IdsSelecteurEdition(config1));
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
