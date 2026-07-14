using System;
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
/// Sprint 32 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, temps réel SignalR) : deux écrans
/// /configuration (deux <see cref="Bunit.TestContext"/> = navigateurs / DI séparés) sur l'onglet
/// « Acteurs », câblés à la <b>MÊME API distante réelle</b> (<see cref="ApiDistanteFactory"/> unique →
/// store singleton partagé, hub SignalR réel commun). Quand un acteur est <b>édité</b> (nom via la modal)
/// puis <b>ajouté</b> (via la modal) depuis le PREMIER écran, la <b>table du SECOND écran CONVERGE</b>
/// (ligne mise à jour, ligne ajoutée) <b>sans rechargement</b>, propagé par la diffusion SignalR de lecture
/// (s20) — laquelle reste en LECTURE SEULE (elle ne fait que ré-énumérer le store, aucune écriture).
///
/// <para>Convention anti-flake *TempsReel* : diffusion de fond idempotente (le store est déjà muté) pour
/// que le push SignalR tombe forcément APRÈS l'établissement des connexions long polling, jamais un délai
/// fixe ; assertions finales sous <see cref="BunitRenderedComponentExtensions.WaitForAssertion"/>. Anti
/// « vert qui ment » : le baseline « Alice » / absence de « Carla » est asserté sur le 2ᵉ écran avant les
/// écritures ; un bUnit à doublure ne prouverait ni le store partagé, ni le second client SignalR, ni la
/// re-projection runtime de la table.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigActeursTableDeuxEcransConvergenceTempsReelTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParId(IRenderedComponent<ConfigurationFoyer> config, string acteurId)
        => config.FindAll("[data-testid='acteur-foyer']").Single(li => li.GetAttribute("data-acteur-id") == acteurId);

    [Fact]
    public async Task Should_faire_converger_la_table_du_second_ecran_sur_une_edition_puis_un_ajout_via_la_modal_du_premier_ecran_sans_rechargement()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). Deux écrans de
        // configuration distincts (deux DI), l'onglet « Acteurs » actif par défaut sur chacun.
        using var api = new ApiDistanteFactory();
        var config1 = RendreConfig(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api);

        // … baseline sur le SECOND écran : parent-a s'affiche « Alice », et « Carla » n'existe pas encore.
        Assert.Equal("Alice", NomLigne(LigneParId(config2, "parent-a")));
        Assert.DoesNotContain(config2.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Carla");

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
            // When (édition depuis l'écran 1) — je renomme parent-a en « Alicia » via la MODAL (POST réel).
            ConfigActeursModalHarness.OuvrirEdition(this, config1, "parent-a");
            this.SurDispatcher(() => config1.Find("[data-testid='champ-nom']").Change("Alicia"));
            this.SurDispatcher(() => config1.Find("#form-edition").Submit());

            // Then (convergence édition) — sans rechargement, la table du SECOND écran voit parent-a → « Alicia »
            // (même id stable, ligne mise à jour) via la diffusion SignalR de lecture.
            config2.WaitForAssertion(
                () => Assert.Equal("Alicia", NomLigne(LigneParId(config2, "parent-a"))),
                TimeSpan.FromSeconds(15));

            // When (ajout depuis l'écran 1) — j'ajoute « Carla » via la MODAL d'ajout (POST réel).
            ConfigActeursModalHarness.OuvrirAjout(this, config1);
            this.SurDispatcher(() => config1.Find("[data-testid='champ-nom-ajout']").Change("Carla"));
            this.SurDispatcher(() => config1.Find("#form-ajout").Submit());

            // Then (convergence ajout) — sans rechargement, la table du SECOND écran fait apparaître « Carla ».
            config2.WaitForAssertion(
                () => Assert.Contains(config2.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Carla"),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }

    /// <summary>Rend un écran de configuration réellement câblé à l'API distante, avec le hub SignalR
    /// redirigé vers le TestServer (long polling) pour que la diffusion temps réel soit observable au runtime.</summary>
    private static IRenderedComponent<ConfigurationFoyer> RendreConfig(TestContext ctx, ApiDistanteFactory api)
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

        var config = ctx.RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }
}
