using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 53 — Sc.13 (🖥️ @ihm) — NIVEAU RUNTIME, gate G3 (constat PO navigateur : « le transfert de Mia
/// apparaît toujours chez Charlie »). Définir un transfert SAISI avec un enfant SÉLECTIONNÉ l'estampille de cet
/// enfant → la pastille bicolore n'apparaît QUE dans SA grille, ABSENTE de la grille d'un AUTRE enfant après
/// bascule ; la dialog affiche l'enfant courant en LECTURE SEULE. Câblage RÉEL (API + store + projection).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmTransfertSaisiScopeEnfantRuntimeTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);
    private const string LeaId = "Léa";
    private const string TomId = "tom";

    private IRenderedComponent<PlanningPartage> RendreGrille(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(GrilleRuntimeHarness.SessionConnectee());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Lundi_29_06_2026));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        return grille;
    }

    private void Selectionner(IRenderedComponent<PlanningPartage> grille, string enfantId)
        => this.SurDispatcher(() => grille.Find("[data-testid='selecteur-enfant-carte']").Change(enfantId));

    private static bool CasePorteBicolore(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).QuerySelector("[data-testid='case-transfert-bicolore']") is not null;

    [Fact]
    public async Task Definir_un_transfert_avec_Lea_selectionnee_l_affiche_chez_Lea_et_l_absente_chez_Tom_dialog_lecture_seule()
    {
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter(TomId, "Tom");

        var grille = RendreGrille(api);
        grille.WaitForElement("[data-testid='selecteur-enfant-carte']", TimeSpan.FromSeconds(10));
        Selectionner(grille, LeaId);

        // When — j'ouvre « Définir un transfert » sur le 30/06 depuis le menu clic-case.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "30/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-definir-transfert']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));

        // Then — la dialog affiche l'enfant COURANT en LECTURE SEULE (« Pour : Léa … »).
        Assert.Contains("Léa", grille.Find("[data-testid='definir-transfert-enfant-courant']").TextContent);

        // When — je saisis le transfert Alice → Bob, école, 08:30, et je valide.
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-recupere']").Change("parent-b"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-lieu']").Change("école"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-heure']").Change("08:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] form").Submit());

        // La grille du rédacteur CONVERGE via la diffusion SignalR (comme en production : l'écriture aboutie
        // déclenche MiseAJour → relecture de la fenêtre de l'enfant courant). On repousse la diffusion en boucle
        // de fond pour qu'un push tombe après l'établissement de la connexion (anti-flake timing).
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusion = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusion.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusion.Token); } catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — en vue Léa, la case 30/06 porte la pastille bicolore du transfert saisi.
            grille.WaitForAssertion(() => Assert.True(CasePorteBicolore(grille, "30/06")), TimeSpan.FromSeconds(15));

            // Then — en basculant sur Tom, le transfert de Léa est ABSENT (aucune pastille bicolore) : plus de fuite.
            Selectionner(grille, TomId);
            grille.WaitForAssertion(() => Assert.False(CasePorteBicolore(grille, "30/06")), TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusion.Cancel();
            await pousseur;
        }
    }
}
