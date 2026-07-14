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
/// Sprint 40 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, câblée à l'API distante RÉELLE, hub SignalR
/// RÉEL — jamais une doublure). Parent-gated LECTURE : sous identité EFFECTIVE non-Parent (Invité), les
/// badges de complétude du couple R3 restent VISIBLES en lecture seule, sans aucun contrôle d'édition.
/// Convergence SignalR : deux écrans /configuration ouverts, les badges rendus sur les deux ; changer un
/// rôle-du-lien depuis la modal Enfants du 1ᵉʳ écran fait CONVERGER le BADGE du 2ᵉ (incomplet ↔ complet)
/// SANS rechargement, par REPROJECTION CLIENT depuis la diffusion (aucun GET sur push, garde conception
/// s38) — la diffusion reste le canal SignalR de LECTURE SEULE (s20 préservé).
/// </summary>
public sealed class FrontWasmConfigBadgeCompletudeGatingConvergenceTests : TestContext
{
    /// <summary>Libellé du badge de complétude du couple pour un enfant donné, ou null si absent.</summary>
    private static string? Badge(IRenderedComponent<ConfigurationFoyer> config, string enfantId)
        => config.FindAll("[data-testid='graphe-enfant-racine']")
            .SingleOrDefault(r => r.GetAttribute("data-enfant-id") == enfantId)
            ?.QuerySelector("[data-testid='graphe-enfant-badge']")?.TextContent.Trim();

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
    public void Sous_identite_Invite_les_badges_restent_visibles_en_lecture_seule_sans_controle_d_edition()
    {
        // Given — « Léa » liée à Alice (père) ET Bruno (mère) dans le store réel → couple complet.
        using var api = new ApiDistanteFactory();
        var enfants = api.Services.GetRequiredService<IEditeurEnfants>();
        enfants.LierParent("Léa", "parent-a", RoleDuLien.Pere);
        enfants.LierParent("Léa", "parent-b", RoleDuLien.Mere);

        // When — j'arrive sur la Config foyer sous identité EFFECTIVE non-Parent (Invité).
        var invite = RendreConfig(this, api, new SessionPlanning { Role = RoleAuteur.Invite });

        // Then — le badge de complétude reste VISIBLE (Invité voit le badge).
        invite.WaitForAssertion(
            () => Assert.Equal("couple complet", Badge(invite, "Léa")),
            TimeSpan.FromSeconds(10));

        // Et STRICTEMENT en lecture : aucun contrôle d'édition dans la section graphe.
        var section = invite.Find("[data-testid='graphe-foyer']");
        Assert.Empty(section.QuerySelectorAll("button, input, select, textarea, a"));
    }

    [Fact]
    public async Task Deux_ecrans_le_badge_converge_incomplet_vers_complet_sur_un_changement_de_role_du_lien_sans_rechargement()
    {
        // Given — UNE API distante réelle (store partagé, hub réel). « Léa » liée à Alice (père) ET Bruno
        // (parent-libre) au départ → 2 parents mais pas le couple père+mère → INCOMPLET sur les deux écrans.
        using var api = new ApiDistanteFactory();
        var enfants = api.Services.GetRequiredService<IEditeurEnfants>();
        enfants.LierParent("Léa", "parent-a", RoleDuLien.Pere);
        enfants.LierParent("Léa", "parent-b", RoleDuLien.ParentLibre);
        var config1 = RendreConfig(this, api, hubReel: true);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api, hubReel: true);
        config1.WaitForAssertion(() => Assert.Equal("couple incomplet", Badge(config1, "Léa")), TimeSpan.FromSeconds(10));
        config2.WaitForAssertion(() => Assert.Equal("couple incomplet", Badge(config2, "Léa")), TimeSpan.FromSeconds(10));

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
            // When (écran 1) — j'ouvre la modal de « Léa », je passe le rôle-du-lien de Bruno à « mère »,
            // j'enregistre → le couple père+mère est désormais complet côté store.
            config1.InvokeAsync(() => config1.FindAll("[data-testid='crayon-enfant']")
                .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
            config1.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
            config1.InvokeAsync(() => ((AngleSharp.Html.Dom.IHtmlSelectElement)config1.FindAll("[data-testid='role-parent-select']")
                .Single(s => s.GetAttribute("data-acteur-id") == "parent-b")).Change("Mere"));
            config1.InvokeAsync(() => config1.Find("#form-enfant").Submit());

            // Then (convergence) — sans rechargement, le BADGE du 2ᵉ écran passe à « couple complet », par
            // REPROJECTION CLIENT depuis la diffusion (canal de LECTURE SEULE — aucune écriture par la diffusion).
            config2.WaitForAssertion(
                () => Assert.Equal("couple complet", Badge(config2, "Léa")),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
