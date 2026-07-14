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
/// Sprint 24 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, temps réel SignalR) : deux écrans de
/// configuration (DI séparées) câblés à la <b>MÊME API distante réelle</b> (<see cref="ApiDistanteFactory"/>
/// unique → store singleton partagé, hub SignalR réel commun). L'<b>activation d'un compte</b> depuis le
/// second écran propage le nouveau statut « actif » au premier écran (onglet Acteurs) <b>sans rechargement</b>
/// (liste des comptes relue depuis le store partagé sur diffusion SignalR lecture seule, s20).
///
/// <para>Convention anti-flake *TempsReel* : diffusion de fond idempotente (le store est déjà muté) pour que
/// le push SignalR tombe forcément APRÈS l'établissement des connexions long polling. Baseline « inactif »
/// assertée sur les deux écrans avant le changement (anti « vert qui ment »).</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigActiverCompteDeuxEcransConvergenceTempsReelTests : TestContext
{
    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == nom);

    [Fact]
    public async Task Should_propager_l_activation_d_un_compte_au_second_ecran_sans_rechargement()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). Un compte INACTIF
        // d'Alice (parent-a) est semé dans le store réel via le port d'écriture.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s24", "alice@foyer.fr", StatutCompte.Inactif, "parent-a");

        var config1 = RendreConfig(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api);

        // Baseline sur les DEUX écrans : le compte d'Alice est « inactif » (lu en ligne) et le TOGGLE « actif »
        // de la MODAL de l'écran 2 (refonte s32 + swap s33 Sc.4) est actionnable — ouvert au crayon, il porte
        // l'activation appliquée à l'« Enregistrer ».
        Assert.Contains("inactif", LigneDe(config1, "Alice").QuerySelector("[data-testid='compte-acteur']")!.TextContent,
            StringComparison.OrdinalIgnoreCase);
        ConfigActeursModalHarness.OuvrirEdition(ecran2, config2, "parent-a");
        Assert.False(config2.Find("[data-testid='toggle-actif']").HasAttribute("disabled"));

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
            // When (activation depuis l'écran 2) — le second écran bascule le toggle « actif » OFF→ON puis
            // enregistre depuis sa modal (POST /api/canal/activer-compte réel).
            config2.InvokeAsync(() =>
                config2.Find("[data-testid='toggle-actif']").Change(true));
            config2.InvokeAsync(() =>
                config2.Find("#form-edition").Submit());

            // Then (convergence) — sans rechargement, le PREMIER écran voit le compte d'Alice passer « actif »
            // (statut + badge d'état relus du store partagé sur diffusion SignalR).
            config1.WaitForAssertion(
                () =>
                {
                    var compte = LigneDe(config1, "Alice").QuerySelector("[data-testid='compte-acteur']");
                    Assert.NotNull(compte);
                    Assert.DoesNotContain("inactif", compte!.TextContent, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("actif", compte.TextContent, StringComparison.OrdinalIgnoreCase);
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }

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
