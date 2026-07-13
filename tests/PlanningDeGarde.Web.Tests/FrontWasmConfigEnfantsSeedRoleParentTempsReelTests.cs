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
/// Sprint 36 — Sc.7 (🖥️ @ihm — PREUVE RUNTIME sur le SEED démo, câblée à l'API distante RÉELLE, store réel,
/// hub SignalR réel — jamais une doublure ni un seed sur-mesure adjacent). Sur le profil de données réel du
/// foyer (Alice → Papa & Bruno → Maman marqués parent ; Nina → Nounou & grand-père → Grand-parent non
/// marqués ; Marie-Hélène sans rôle) : (1) le sélecteur des parents de la modal Enfants propose Alice & Bruno
/// et EXCLUT Nina, grand-père et Marie-Hélène (retour PO gate corrigé) ; lier Alice puis Bruno reflète la
/// colonne « Parents liés » avec les DEUX noms (distinction Papa/Maman par le NOM, hors scope champ dédié).
/// (2) Décocher « rôle parent » sur le rôle Papa depuis un AUTRE écran retire Alice du sélecteur en TEMPS
/// RÉEL (SignalR) — le flag est la source de vérité vivante de l'éligibilité.
/// </summary>
public sealed class FrontWasmConfigEnfantsSeedRoleParentTempsReelTests : TestContext
{
    private static string? Prenom(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static string ParentsLies(IRenderedComponent<ConfigurationFoyer> config)
        => config.FindAll("[data-testid='enfant-foyer']").Single(li => Prenom(li) == "Léa")
            .QuerySelector("[data-testid='enfant-parents-lies']")!.TextContent.Trim();

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
        config.WaitForState(() => config.FindAll("[data-testid='enfant-foyer']").Count > 0, TimeSpan.FromSeconds(10));
        return config;
    }

    private static void OuvrirModalLea(TestContext ctx, IRenderedComponent<ConfigurationFoyer> config)
    {
        ctx.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Sur_le_seed_Alice_et_Bruno_liables_Nina_et_grand_pere_exclus_lier_reflete_les_deux_noms()
    {
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(this, api);
        Assert.Equal("—", ParentsLies(config));

        OuvrirModalLea(this, config);
        var candidats = config.FindAll("[data-testid='checkbox-parent']")
            .Select(c => c.GetAttribute("data-acteur-id")).ToList();
        Assert.Contains("parent-a", candidats);   // Alice → Papa (marqué parent)
        Assert.Contains("parent-b", candidats);   // Bruno → Maman (marqué parent)
        Assert.DoesNotContain("nounou", candidats);     // Nina → Nounou (non marqué) — retour PO gate corrigé
        Assert.DoesNotContain("grand-pere", candidats); // grand-père → Grand-parent (non marqué)
        Assert.DoesNotContain("parent-c", candidats);   // Marie-Hélène → sans rôle
        Assert.Equal(2, candidats.Count);

        // Je lie Alice PUIS Bruno → deux POST réels lier-enfant-parent.
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(true));
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-b").Change(true));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // La colonne « Parents liés » distingue « Alice » ET « Bruno » PAR LE NOM ; le domaine porte les deux liens.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                var parents = ParentsLies(config);
                Assert.Contains("Alice", parents);
                Assert.Contains("Bruno", parents);
            },
            TimeSpan.FromSeconds(10));
        var lies = api.Services.GetRequiredService<IEnumerationEnfants>().EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies;
        Assert.Contains("parent-a", lies);
        Assert.Contains("parent-b", lies);
    }

    [Fact]
    public async Task Decocher_role_parent_sur_Papa_depuis_un_autre_ecran_retire_Alice_du_selecteur_en_temps_reel()
    {
        // Given — UNE seule API distante réelle (store partagé, hub SignalR réel commun), deux écrans.
        using var api = new ApiDistanteFactory();
        var config1 = RendreConfig(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api);

        // Écran 1 : la modal Enfants de Léa est OUVERTE ; son sélecteur propose Alice (parent-a) & Bruno.
        OuvrirModalLea(this, config1);
        Assert.Contains(config1.FindAll("[data-testid='checkbox-parent']"),
            c => c.GetAttribute("data-acteur-id") == "parent-a");

        // Diffusion de fond idempotente (push SignalR garanti après établissement des connexions, s21/s33).
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
            // When — l'écran 2 DÉCOCHE « rôle parent » sur le rôle Papa (POST marquer-role-parent réel).
            config2.InvokeAsync(() => config2.FindAll("[data-testid='crayon-role']")
                .Single(b => b.GetAttribute("data-role-id") == "role-papa").Click());
            config2.WaitForElement("[data-testid='dialog-role']", TimeSpan.FromSeconds(10));
            config2.InvokeAsync(() => config2.Find("[data-testid='checkbox-role-parent']").Change(false));
            config2.InvokeAsync(() => config2.Find("#form-role").Submit());

            // Then — sur l'écran 1, le sélecteur OUVERT de Léa perd Alice EN TEMPS RÉEL (le flag est la source
            // de vérité vivante), tandis que Bruno (Maman, toujours marqué parent) reste proposé.
            config1.WaitForAssertion(
                () =>
                {
                    var candidats = config1.FindAll("[data-testid='checkbox-parent']")
                        .Select(c => c.GetAttribute("data-acteur-id")).ToList();
                    Assert.DoesNotContain("parent-a", candidats); // Alice retirée (Papa n'est plus marqué parent)
                    Assert.Contains("parent-b", candidats);       // Bruno reste (Maman toujours marqué parent)
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
