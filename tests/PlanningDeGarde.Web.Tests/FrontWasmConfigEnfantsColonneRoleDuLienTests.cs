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
/// Sprint 37 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : la colonne « Parents
/// liés » du tableau Enfants affiche, pour chaque parent, son NOM ET son rôle-du-lien (père / mère / parent).
/// Sous identité EFFECTIVE non-Parent (Invité) le tableau reste en LECTURE SEULE (rôles-du-lien visibles),
/// sans crayon ni « Ajouter ». Deux écrans câblés au hub réel : une modification du rôle-du-lien depuis le
/// 1ᵉʳ écran fait CONVERGER le tableau du 2ᵉ (nom + rôle-du-lien) sans rechargement, via SignalR lecture seule.
/// </summary>
public sealed class FrontWasmConfigEnfantsColonneRoleDuLienTests : TestContext
{
    private static string? Prenom(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static string ParentsLies(IRenderedComponent<ConfigurationFoyer> config, string prenom)
        => config.FindAll("[data-testid='enfant-foyer']").Single(li => Prenom(li) == prenom)
            .QuerySelector("[data-testid='enfant-parents-lies']")!.TextContent.Trim();

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
            () => config.FindAll("[data-testid='enfant-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void La_colonne_parents_lies_affiche_nom_et_role_du_lien_et_reste_visible_en_lecture_seule_sous_Invite()
    {
        // Given — « Léa » liée à Alice (parent-a) « père » et Bruno (parent-b) « mère » dans le store réel.
        using var api = new ApiDistanteFactory();
        var enfants = api.Services.GetRequiredService<IEditeurEnfants>();
        enfants.LierParent("Léa", "parent-a", RoleDuLien.Pere);
        enfants.LierParent("Léa", "parent-b", RoleDuLien.Mere);

        // Then (Parent) — la colonne affiche NOM + rôle-du-lien pour chaque parent.
        var config = RendreConfig(this, api);
        Assert.Equal("Alice (père), Bruno (mère)", ParentsLies(config, "Léa"));

        // Then (Invité) — le tableau reste VISIBLE en lecture seule (rôles-du-lien consultables), sans surface d'écriture.
        using var ecranInvite = new TestContext();
        var invite = RendreConfig(ecranInvite, api, new SessionPlanning { Role = RoleAuteur.Invite });
        Assert.Equal("Alice (père), Bruno (mère)", ParentsLies(invite, "Léa"));
        Assert.Empty(invite.FindAll("[data-testid='crayon-enfant']"));
        Assert.Empty(invite.FindAll("[data-testid='bouton-ajouter-enfant']"));
    }

    [Fact]
    public async Task Deux_ecrans_convergent_sur_un_changement_de_role_du_lien_sans_rechargement()
    {
        // Given — UNE API distante réelle (store partagé, hub réel). « Léa » liée à Alice (parent-a) en
        // « parent-libre » au départ, sur les DEUX écrans.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a", RoleDuLien.ParentLibre);
        var config1 = RendreConfig(this, api, hubReel: true);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api, hubReel: true);
        Assert.Equal("Alice (parent)", ParentsLies(config1, "Léa"));
        Assert.Equal("Alice (parent)", ParentsLies(config2, "Léa"));

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

            // Then (convergence) — sans rechargement, le 2ᵉ écran voit « Alice (père) » (relu via SignalR, lecture seule).
            config2.WaitForAssertion(
                () => Assert.Equal("Alice (père)", ParentsLies(config2, "Léa")),
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
