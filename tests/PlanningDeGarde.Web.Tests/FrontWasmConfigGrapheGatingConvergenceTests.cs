using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 38 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE, hub SignalR
/// RÉEL — jamais une doublure). Parent-gated LECTURE : sous identité EFFECTIVE non-Parent (Invité), la vue
/// graphe reste VISIBLE en lecture seule (Invité voit la vue), sans aucun contrôle d'édition. Convergence
/// SignalR : deux écrans /configuration ouverts, le graphe rendu sur les deux ; lier / délier / changer un
/// rôle-du-lien depuis la modal Enfants du 1ᵉʳ écran fait CONVERGER le graphe du 2ᵉ (racine, branche,
/// nom + rôle-du-lien) SANS rechargement, par le canal SignalR de LECTURE SEULE (s20 préservé).
/// </summary>
public sealed class FrontWasmConfigGrapheGatingConvergenceTests : TestContext
{
    /// <summary>Texte de la branche parent (« nom (rôle) ») du graphe pour un enfant/acteur donnés, ou null si absente.</summary>
    private static string? Branche(IRenderedComponent<ConfigurationFoyer> config, string enfantId, string acteurId)
        => config.FindAll("[data-testid='graphe-parent-branche']")
            .SingleOrDefault(b => b.GetAttribute("data-enfant-id") == enfantId && b.GetAttribute("data-acteur-id") == acteurId)
            ?.TextContent.Trim();

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(
        TestContext ctx, ApiDistanteFactory api, SessionPlanning? session = null, bool hubReel = false)
    {
        ctx.Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        ctx.Services.AddSingleton(session ?? new SessionPlanning());
        if (hubReel)
        {
            ctx.Services.AddSingleton(new OptionsConnexionHub
            {
                Configurer = options =>
                {
                    options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                },
            });
        }

        var config = ctx.RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='graphe-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Sous_identite_Invite_la_vue_graphe_reste_visible_en_lecture_seule_sans_controle_d_edition()
    {
        // Given — « Léa » liée à Alice (parent-a) « mère » dans le store réel.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a", RoleDuLien.Mere);

        // When — j'arrive sur la Config foyer sous identité EFFECTIVE non-Parent (Invité).
        var invite = RendreConfig(this, api, new SessionPlanning { Role = RoleAuteur.Invite });

        // Then — la vue graphe reste VISIBLE (Invité voit la vue) : Léa en racine, branche « Alice (mère) ».
        invite.WaitForAssertion(
            () => Assert.Equal("Alice (mère)", Branche(invite, "Léa", "parent-a")),
            TimeSpan.FromSeconds(10));

        // Et STRICTEMENT en lecture : aucun contrôle d'édition dans la section graphe.
        var section = invite.Find("[data-testid='graphe-foyer']");
        Assert.Empty(section.QuerySelectorAll("button, input, select, textarea, a"));
    }

    [Fact]
    public async Task Deux_ecrans_le_graphe_converge_sur_un_changement_de_role_du_lien_sans_rechargement()
    {
        // Given — UNE API distante réelle (store partagé, hub réel). « Léa » liée à Alice (parent-a) en
        // « parent-libre » au départ ; le graphe est rendu sur les DEUX écrans (branche « Alice (parent) »).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a", RoleDuLien.ParentLibre);
        var config1 = RendreConfig(this, api, hubReel: true);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api, hubReel: true);
        // Le graphe se charge en asynchrone (GET /api/foyer/graphe) : on attend que la branche « Alice (parent) »
        // soit peuplée sur les DEUX écrans avant d'observer la convergence.
        config1.WaitForAssertion(() => Assert.Equal("Alice (parent)", Branche(config1, "Léa", "parent-a")), TimeSpan.FromSeconds(10));
        config2.WaitForAssertion(() => Assert.Equal("Alice (parent)", Branche(config2, "Léa", "parent-a")), TimeSpan.FromSeconds(10));

        // Diffusion de fond idempotente (convention anti-flake *TempsReel*) : le push tombe forcément APRÈS
        // l'établissement des connexions long polling, sans dépendre du timing.
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
            // When (écran 1) — j'ouvre la modal de « Léa », je passe le rôle-du-lien d'Alice à « père », j'enregistre.
            config1.InvokeAsync(() => config1.FindAll("[data-testid='crayon-enfant']")
                .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
            config1.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
            config1.InvokeAsync(() => ((AngleSharp.Html.Dom.IHtmlSelectElement)config1.FindAll("[data-testid='role-parent-select']")
                .Single(s => s.GetAttribute("data-acteur-id") == "parent-a")).Change("Pere"));
            config1.InvokeAsync(() => config1.Find("#form-enfant").Submit());

            // Then (convergence) — sans rechargement, le graphe du 2ᵉ écran voit « Alice (père) » (relu via
            // SignalR, canal de LECTURE SEULE — aucune écriture par la diffusion).
            config2.WaitForAssertion(
                () => Assert.Equal("Alice (père)", Branche(config2, "Léa", "parent-a")),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
