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
/// Sprint 41 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le sens OFF est Parent-gated et converge
/// en temps réel. Écran réellement câblé à l'API distante réelle (store réel, DI réelle, hub SignalR réel).
///  - <b>Gating</b> : sous identité EFFECTIVE non-Parent (Invité), le tableau Acteurs reste en LECTURE
///    SEULE — aucun crayon ni toggle actionnable atteignable (gating s14/s20/s33 préservé, R8/R9 non modifié).
///  - <b>Convergence SignalR</b> : deux écrans Config foyer (DI séparées) sur la MÊME API distante réelle ;
///    dé-désigner l'admin d'un acteur via la modal du 1ᵉʳ écran fait CONVERGER la ligne du 2ᵉ écran sur le
///    nouvel état admin SANS rechargement (ré-énumération du store partagé sur diffusion lecture seule, s20).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigActeursToggleOffGatingConvergenceTempsReelTests : TestContext
{
    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.QuerySelector(".acteur-nom")?.TextContent.Trim() == nom);

    [Fact]
    public void Sous_identite_Invite_le_tableau_Acteurs_reste_en_lecture_seule_aucun_crayon_ni_toggle_actionnable()
    {
        // Given — un acteur admin/actif est semé (parent-a), écran câblé sous identité effective « Invité ».
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurAdminsFoyer>().DesignerAdmin("parent-a");
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s41", "alice@foyer.fr", StatutCompte.Actif, "parent-a");
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        Assert.False(session.EstParent); // garde-fou : un Invité n'écrit pas (R8/R9)
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.AttendreLignes(config);

        // Then — l'état admin/actif reste LISIBLE (badges), mais AUCUN crayon ni toggle actionnable n'existe
        // (le sens OFF est inatteignable sous Invité — gating identité effective préservé, R8/R9 non modifié).
        Assert.NotNull(LigneDe(config, "Alice").QuerySelector("[data-testid='acteur-admin-marqueur']"));
        Assert.Empty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
        Assert.Empty(config.FindAll("[data-testid='toggle-admin']"));
        Assert.Empty(config.FindAll("[data-testid='toggle-actif']"));
    }

    [Fact]
    public async Task Should_propager_la_de_designation_d_admin_au_second_ecran_sans_rechargement()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). parent-a (Alice)
        // ET parent-b (Bruno) sont admins : la dé-désignation d'Alice ne bute pas sur la borne dernier admin.
        using var api = new ApiDistanteFactory();
        var editeurAdmins = api.Services.GetRequiredService<IEditeurAdminsFoyer>();
        editeurAdmins.DesignerAdmin("parent-a");
        editeurAdmins.DesignerAdmin("parent-b");

        var config1 = RendreConfig(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api);

        // Baseline sur les DEUX écrans : Alice est marquée « admin ».
        Assert.NotEmpty(LigneDe(config1, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']"));
        Assert.NotEmpty(LigneDe(config2, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']"));

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
            // When (dé-désignation depuis l'écran 1) — la modal d'Alice bascule le toggle admin ON→OFF puis
            // enregistre (POST /api/canal/de-designer-admin réel).
            ConfigActeursModalHarness.OuvrirEdition(this, config1, "parent-a");
            config1.InvokeAsync(() => config1.Find("[data-testid='toggle-admin']").Change(false));
            config1.InvokeAsync(() => config1.Find("#form-edition").Submit());

            // Then (convergence) — sans rechargement, le SECOND écran voit Alice perdre son badge « admin »
            // (admins relus du store partagé sur diffusion SignalR lecture seule).
            config2.WaitForAssertion(
                () => Assert.Empty(LigneDe(config2, "Alice").QuerySelectorAll("[data-testid='acteur-admin-marqueur']")),
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
