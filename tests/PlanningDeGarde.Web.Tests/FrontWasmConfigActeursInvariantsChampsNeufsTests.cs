using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : les invariants de la modal acteur
/// (refus→modal ouverte + saisie conservée ; Parent-gated ; convergence SignalR sans écriture par la
/// diffusion) tiennent sur les CHAMPS NEUFS de ce sprint — toggle admin/actif (Sc.4), adresse (Sc.5),
/// palette couleur (Sc.6). Écran réellement câblé à l'API distante réelle, store réel, DI réelle ; jamais
/// une doublure. On ne redouble pas les tests généraux s32 (déjà verts sur le nom) : on cible les neufs.
/// </summary>
public sealed class FrontWasmConfigActeursInvariantsChampsNeufsTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParId(IRenderedComponent<ConfigurationFoyer> config, string acteurId)
        => config.FindAll("[data-testid='acteur-foyer']").Single(li => li.GetAttribute("data-acteur-id") == acteurId);

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api, SessionPlanning? session = null)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session ?? new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    // ─── Invariant 1 : refus domaine → modal ouverte, saisie des CHAMPS NEUFS conservée, table inchangée ───
    [Fact]
    public void Un_refus_domaine_conserve_l_adresse_la_couleur_choisie_et_le_toggle_admin_dans_la_modal_ouverte_sans_toucher_la_table()
    {
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // When — je saisis une adresse, choisis « vert », bascule le toggle admin ON, PUIS mets un nom
        // tout-espaces (refusé par le domaine) et j'enregistre.
        this.SurDispatcher(() => config.Find("[data-testid='champ-adresse']").Change("99 rue de l'Essai"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-vert']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='toggle-admin']").Change(true));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("   "));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal RESTE OUVERTE, motif dedans, et ma saisie des champs NEUFS est CONSERVÉE.
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-acteur']");
                Assert.Equal("le nom ne peut pas être vide",
                    modal.QuerySelector("[data-testid='motif-echec']")!.TextContent.Trim());
                Assert.Equal("99 rue de l'Essai", modal.QuerySelector("[data-testid='champ-adresse']")!.GetAttribute("value"));
                Assert.Contains("selectionnee", modal.QuerySelector("[data-testid='pastille-couleur-vert']")!.GetAttribute("class"));
                Assert.True(((IHtmlInputElement)modal.QuerySelector("[data-testid='toggle-admin']")!).IsChecked);
            },
            TimeSpan.FromSeconds(10));

        // … et la TABLE reste inchangée : parent-a toujours « Alice », couleur bleue, sans badge admin, sans adresse.
        var ligne = LigneParId(config, "parent-a");
        Assert.Equal("Alice", NomLigne(ligne));
        Assert.Equal("bleu", ligne.GetAttribute("data-couleur"));
        Assert.Empty(ligne.QuerySelectorAll("[data-testid='acteur-admin-marqueur']"));
        Assert.Empty(ligne.QuerySelectorAll("[data-testid='acteur-adresse']"));
    }

    // ─── Invariant 2 : gating non-Parent → toggles / adresse / palette INATTEIGNABLES (aucune modal) ───
    [Fact]
    public void Sous_identite_Invite_les_toggles_l_adresse_et_la_palette_sont_inatteignables()
    {
        using var api = new ApiDistanteFactory();
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreConfig(api, session);
        Assert.False(session.EstParent);

        // Then — aucune surface d'écriture des champs neufs n'est atteignable (elles vivent dans la modal, non rendue).
        Assert.Empty(config.FindAll("[data-testid='toggle-admin']"));
        Assert.Empty(config.FindAll("[data-testid='toggle-actif']"));
        Assert.Empty(config.FindAll("[data-testid='champ-adresse']"));
        Assert.Empty(config.FindAll("[data-testid='palette-couleur']"));

        // Contrôle positif (anti faux-vert) — sous Parent, la modal ouverte les rend tous.
        session.Role = RoleAuteur.Parent;
        config.Render();
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        Assert.NotEmpty(config.FindAll("[data-testid='toggle-admin']"));
        Assert.NotEmpty(config.FindAll("[data-testid='champ-adresse']"));
        Assert.NotEmpty(config.FindAll("[data-testid='palette-couleur']"));
    }

    // ─── Invariant 3 : convergence SignalR sur les CHAMPS NEUFS (adresse + couleur) sans rechargement ───
    [Fact]
    public async Task Une_edition_adresse_et_couleur_depuis_l_ecran_1_fait_converger_la_table_du_second_ecran_sans_rechargement()
    {
        using var api = new ApiDistanteFactory();
        var config1 = RendreConfigTempsReel(this, api);
        using var ecran2 = new TestContext();
        var config2 = RendreConfigTempsReel(ecran2, api);

        // Baseline écran 2 : parent-a bleu, sans adresse.
        Assert.Equal("bleu", LigneParId(config2, "parent-a").GetAttribute("data-couleur"));
        Assert.Empty(LigneParId(config2, "parent-a").QuerySelectorAll("[data-testid='acteur-adresse']"));

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
            // When (édition adresse + couleur depuis l'écran 1) via la modal (POST réel editer-acteur).
            ConfigActeursModalHarness.OuvrirEdition(this, config1, "parent-a");
            this.SurDispatcher(() => config1.Find("[data-testid='champ-adresse']").Change("77 avenue Neuve"));
            this.SurDispatcher(() => config1.Find("[data-testid='pastille-couleur-violet']").Click());
            this.SurDispatcher(() => config1.Find("#form-edition").Submit());

            // Then — sans rechargement, la table du SECOND écran converge sur les champs neufs (couleur + adresse).
            config2.WaitForAssertion(
                () =>
                {
                    var ligne = LigneParId(config2, "parent-a");
                    Assert.Equal("violet", ligne.GetAttribute("data-couleur"));
                    Assert.Contains("77 avenue Neuve",
                        ligne.QuerySelector("[data-testid='acteur-adresse']")!.TextContent);
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }

    private static IRenderedComponent<ConfigurationFoyer> RendreConfigTempsReel(TestContext ctx, ApiDistanteFactory api)
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
