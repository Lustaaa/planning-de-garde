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
/// Sprint 22 — Sc.9 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, temps réel SignalR) : deux écrans de
/// configuration (DI séparées) câblés à la <b>MÊME API distante réelle</b> (<see cref="ApiDistanteFactory"/>
/// unique → store singleton partagé, hub SignalR réel commun). Depuis le second écran : la <b>création d'un
/// compte</b> puis la <b>désignation de l'admin du foyer</b> convergent sur le premier écran <b>sans
/// rechargement</b> (liste des comptes ET admin affiché relus depuis le store partagé sur diffusion SignalR).
/// Un compte dont l'acteur est supprimé apparaît <b>désassocié</b> sur les deux écrans, sans compte fantôme.
///
/// <para>Convention anti-flake *TempsReel* : diffusion de fond idempotente (le store est déjà muté) pour que
/// le push SignalR tombe forcément APRÈS l'établissement des connexions long polling. Baseline asserté avant
/// chaque changement (anti « vert qui ment »).</para>
/// </summary>
public sealed class FrontWasmConfigCompteAdminDeuxEcransConvergenceTempsReelTests : TestContext
{
    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == nom);

    [Fact]
    public async Task Should_propager_creation_de_compte_et_designation_admin_au_second_ecran_et_desassocier_a_la_suppression()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). Alice (parent-a) et
        // Bruno (parent-b) sont des acteurs Parent du seed.
        using var api = new ApiDistanteFactory();

        var config1 = RendreConfig(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api);

        // Baseline sur les DEUX écrans : Bruno n'a pas de compte, Alice n'est pas admin.
        Assert.Empty(LigneDe(config1, "Bruno").QuerySelectorAll("[data-testid='compte-acteur']"));
        Assert.Empty(LigneDe(config1, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']"));

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
            // When (création de compte depuis l'écran 2) — le second écran crée le compte de Bruno.
            config2.InvokeAsync(() =>
                LigneDe(config2, "Bruno").QuerySelector("[data-testid='champ-email-compte']")!.Change("bruno@foyer.fr"));
            config2.InvokeAsync(() =>
                LigneDe(config2, "Bruno").QuerySelector("[data-testid='bouton-creer-compte']")!.Click());

            // Then (convergence création) — sans rechargement, le PREMIER écran voit le compte de Bruno.
            config1.WaitForAssertion(
                () =>
                {
                    var compte = LigneDe(config1, "Bruno").QuerySelector("[data-testid='compte-acteur']");
                    Assert.NotNull(compte);
                    Assert.Contains("bruno@foyer.fr", compte!.TextContent);
                },
                TimeSpan.FromSeconds(15));

            // When (désignation admin depuis l'écran 2) — le second écran désigne Alice (Parent) admin du foyer.
            config2.InvokeAsync(() =>
                LigneDe(config2, "Alice").QuerySelector("[data-testid='bouton-designer-admin']")!.Click());

            // Then (convergence admin) — sans rechargement, le PREMIER écran affiche Alice comme admin.
            config1.WaitForAssertion(
                () => Assert.NotEmpty(LigneDe(config1, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']")),
                TimeSpan.FromSeconds(15));

            // When (suppression de l'acteur associé depuis l'écran 2) — le second écran supprime Bruno.
            config2.InvokeAsync(() =>
                LigneDe(config2, "Bruno").QuerySelector("[data-testid='bouton-supprimer']")!.Click());

            // Then (désassociation propre sur les DEUX écrans) — Bruno quitte la liste ; aucune ligne ne porte
            // plus son compte (compte désassocié, pas de compte fantôme référençant l'acteur absent).
            config1.WaitForAssertion(
                () =>
                {
                    Assert.DoesNotContain(config1.FindAll("[data-testid='acteur-foyer']"),
                        li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Bruno");
                    Assert.DoesNotContain(config1.FindAll("[data-testid='compte-acteur']"),
                        c => c.TextContent.Contains("bruno@foyer.fr"));
                },
                TimeSpan.FromSeconds(15));
            config2.WaitForAssertion(
                () => Assert.DoesNotContain(config2.FindAll("[data-testid='compte-acteur']"),
                    c => c.TextContent.Contains("bruno@foyer.fr")),
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
