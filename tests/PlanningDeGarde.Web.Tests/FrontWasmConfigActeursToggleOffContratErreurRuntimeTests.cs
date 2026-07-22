using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 41 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : contrat d'erreur du sens OFF. Écran
/// réellement câblé à l'API distante réelle (store réel, DI réelle, canal HTTP réel). Quand le OFF du
/// toggle admin vise le DERNIER admin du foyer, le refus domaine (borne Sc.2) revient : la modal RESTE
/// OUVERTE, le motif est affiché DEDANS, l'état des toggles est CONSERVÉ, et AUCUNE écriture partielle ne
/// touche le tableau (l'acteur demeure admin dans le store réel). « Échap » ferme la modal STRICTEMENT
/// comme « Annuler » : aucune commande émise, aucune mutation (port IEcouteurEchapModal s33).
/// </summary>
public sealed class FrontWasmConfigActeursToggleOffContratErreurRuntimeTests : TestContext
{
    /// <summary>Double à la main du port d'écoute Échap (spy) : capte le callback d'attache pour rejouer
    /// l'appui Échap document (miroir du test s33).</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;
        public ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
        {
            _onEchap = onEchap;
            return ValueTask.FromResult<IAsyncDisposable>(new Abonnement(this));
        }
        public Task DeclencherEchapDocument() => _onEchap?.Invoke() ?? Task.CompletedTask;
        private sealed class Abonnement : IAsyncDisposable
        {
            private readonly EspionEchap _e;
            public Abonnement(EspionEchap e) => _e = e;
            public ValueTask DisposeAsync() { _e._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    private static AngleSharp.Dom.IElement LigneDe(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.QuerySelector(".acteur-nom")?.TextContent.Trim() == nom);

    private (IRenderedComponent<ConfigurationFoyer> config, EspionEchap espion) RendreConfig(ApiDistanteFactory api)
    {
        var espion = new EspionEchap();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return (config, espion);
    }

    [Fact]
    public void Le_OFF_du_dernier_admin_est_refuse_la_modal_reste_ouverte_avec_le_motif_et_le_store_reste_intact()
    {
        // Given — parent-a (Alice) est le SEUL admin du foyer (dernier admin, borne Sc.2).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurAdminsFoyer>().DesignerAdmin("parent-a");
        var (config, _) = RendreConfig(api);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // When — je bascule le toggle admin ON→OFF puis j'enregistre (POST réel /api/canal/de-designer-admin).
        this.SurDispatcher(() => config.Find("[data-testid='toggle-admin']").Change(false));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal RESTE OUVERTE, le motif du refus (borne dernier admin) est affiché DEDANS.
        config.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(config.FindAll("[data-testid='dialog-acteur']")); // modal restée ouverte
                var motif = config.Find("[data-testid='motif-echec']").TextContent;
                Assert.Contains("admin", motif, StringComparison.OrdinalIgnoreCase); // « garder au moins un admin »
            },
            TimeSpan.FromSeconds(10));

        // Et — AUCUNE écriture partielle : l'acteur demeure admin dans le store réel (borne AVANT écriture).
        Assert.Contains("parent-a", api.Services.GetRequiredService<IEnumerationAdminsFoyer>().EnumererAdmins());
    }

    [Fact]
    public async Task Echap_document_sur_la_modal_OFF_en_etat_de_refus_la_ferme_sans_rien_reemettre_ni_muter()
    {
        // Given — parent-a est le dernier admin ; on tente le OFF → REFUS (modal ouverte, motif affiché).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurAdminsFoyer>().DesignerAdmin("parent-a");
        var (config, espion) = RendreConfig(api);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='toggle-admin']").Change(false));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());
        config.WaitForElement("[data-testid='motif-echec']", TimeSpan.FromSeconds(10));

        // When — Échap document sur la modal EN ÉTAT DE REFUS (callback capté par le spy = appui réel simulé).
        await config.InvokeAsync(() => espion.DeclencherEchapDocument());

        // Then — Échap ferme (= annuler) sans rien réémettre : la modal disparaît, l'acteur reste admin
        // (aucune commande de dé-désignation aboutie, le store est intact).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-acteur']")),
            TimeSpan.FromSeconds(10));
        Assert.Contains("parent-a", api.Services.GetRequiredService<IEnumerationAdminsFoyer>().EnumererAdmins());
    }
}
