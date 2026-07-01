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
/// Sprint 21 — Sc.10 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, temps réel SignalR) : deux écrans de
/// configuration (deux navigateurs / DI séparées) câblés à la <b>MÊME API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/> unique → store singleton partagé, hub SignalR réel commun). Une
/// <b>création puis suppression de rôle</b> émise depuis le second écran converge sur le premier écran
/// <b>sans rechargement</b> : la liste des rôles ET les sélecteurs de rôle du premier écran reflètent le
/// changement (relus depuis le store partagé sur diffusion SignalR). Un acteur portant le rôle supprimé
/// retombe « sans rôle » sur les deux écrans, sans rôle fantôme.
///
/// <para>Convention anti-flake *TempsReel* : on ré-émet la diffusion de fond (idempotente — le store est
/// déjà muté) pour que le push SignalR tombe forcément APRÈS l'établissement des connexions long polling,
/// sans dépendre du timing. Anti « vert qui ment » : baseline asserté avant chaque changement.</para>
/// </summary>
public sealed class FrontWasmConfigRolesDeuxEcransConvergenceTempsReelTests : TestContext
{
    private static string? LibelleLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".role-libelle")?.TextContent.Trim();

    [Fact]
    public async Task Should_propager_creation_et_suppression_de_role_au_second_ecran_avec_repli_sans_role_sur_les_deux()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). Un acteur
        // (parent-a / Alice) porte un rôle « Nounou » déjà présent au référentiel : on observera qu'à sa
        // suppression, il retombe « sans rôle » sur les DEUX écrans.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().AffecterRole("parent-a", "role-nounou");

        // Écran 1 = ce TestContext ; écran 2 = un second TestContext (DI séparée), câblé à la MÊME api.
        var config1 = RendreConfig(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api);

        // … baseline asserté sur les DEUX écrans : « Nounou » présent, Alice le porte.
        Assert.Contains(config1.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Nounou");
        Assert.Equal("Nounou", RoleCourantDe(config1, "Alice"));
        Assert.Equal("Nounou", RoleCourantDe(config2, "Alice"));

        // Diffusion de fond idempotente (push SignalR garanti après établissement des connexions).
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
            // When (création depuis l'écran 2) — le second écran crée un rôle « Grand-parent ». Le pousseur de
            // diffusion re-rend l'écran en fond : on enveloppe find+action dans InvokeAsync (aucun re-render
            // entre la résolution et le déclenchement — workaround bUnit).
            config2.InvokeAsync(() => config2.Find("[data-testid='champ-libelle-role']").Change("Grand-parent"));
            config2.InvokeAsync(() => config2.Find("#form-creer-role").Submit());

            // Then (convergence création) — sans rechargement, le PREMIER écran voit « Grand-parent » dans
            // la liste des rôles ET dans le sélecteur de rôle d'un acteur (relu via SignalR).
            config1.WaitForAssertion(
                () =>
                {
                    Assert.Contains(config1.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Grand-parent");
                    Assert.Contains(
                        config1.FindAll("[data-testid='acteur-foyer']").First()
                            .QuerySelector("[data-testid='selecteur-role-acteur']")!.QuerySelectorAll("option")
                            .Select(o => o.TextContent.Trim()),
                        t => t == "Grand-parent");
                },
                TimeSpan.FromSeconds(15));

            // When (suppression depuis l'écran 2) — le second écran supprime « Nounou » (porté par Alice).
            config2.InvokeAsync(() =>
                config2.FindAll("[data-testid='role-foyer']")
                    .Single(li => LibelleLigne(li) == "Nounou")
                    .QuerySelector("[data-testid='bouton-supprimer-role']")!.Click());

            // Then (convergence suppression + repli) — sans rechargement, « Nounou » quitte la liste des rôles
            // du premier écran, et Alice retombe « sans rôle » sur les DEUX écrans (repli neutre, pas de fantôme).
            config1.WaitForAssertion(
                () =>
                {
                    Assert.DoesNotContain(config1.FindAll("[data-testid='role-foyer']"), li => LibelleLigne(li) == "Nounou");
                    Assert.Equal("sans rôle", RoleCourantDe(config1, "Alice"));
                },
                TimeSpan.FromSeconds(15));
            config2.WaitForAssertion(
                () => Assert.Equal("sans rôle", RoleCourantDe(config2, "Alice")),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }

    /// <summary>Rôle courant affiché pour l'acteur nommé <paramref name="nom"/> sur un écran de config.</summary>
    private static string RoleCourantDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == nom)
            .QuerySelector("[data-testid='role-acteur-courant']")!.TextContent.Trim();

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
