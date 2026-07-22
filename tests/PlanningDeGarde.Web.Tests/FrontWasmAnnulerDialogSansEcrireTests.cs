using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.5 (🖥️ scénario IHM, <c>@limite</c>) — annuler une dialog
/// <b>n'émet aucune écriture</b> et laisse la grille intacte (règle 14). Contredit le chemin de
/// validation (Sc.2) : la fermeture par annulation ne doit <b>pas</b> émettre de commande.
///
/// Non-vacuité (garde-fou CP) : on n'observe pas seulement « la case n'a pas changé » (qui masquerait
/// une écriture annulée côté store) — on <b>espionne le canal</b> via un handler qui compte les
/// <c>POST</c> d'écriture (<c>/api/canal/…</c>) et on exige <b>0</b> à l'annulation, ET la grille
/// inchangée (la case reste sur « Bruno »), ET le store sans affectation d'Alice.
/// </summary>
public sealed class FrontWasmAnnulerDialogSansEcrireTests : TestContext
{
    // Samedi 20/06/2026 : la case d'où l'on ouvre la dialog. Référence au 20/06 → fenêtre couvrant ce jour.
    private static readonly DateTime Samedi_20_06_2026 = new(2026, 6, 20);

    /// <summary>Handler qui relaie tout vers l'API distante réelle mais COMPTE les écritures du canal
    /// (POST vers <c>/api/canal/…</c>) — la négociation SignalR (POST vers <c>/hubs/…</c>) et les
    /// lectures (GET) ne sont pas comptées. Permet d'exiger 0 écriture émise à l'annulation.</summary>
    private sealed class CompteurEcrituresHandler : DelegatingHandler
    {
        private int _ecritures;
        public int Ecritures => _ecritures;

        public CompteurEcrituresHandler(HttpMessageHandler inner) : base(inner) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if ((request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete)
                && (request.RequestUri?.AbsolutePath.StartsWith("/api/", StringComparison.Ordinal) ?? false))
            {
                Interlocked.Increment(ref _ecritures);
            }

            return base.SendAsync(request, ct);
        }
    }

    [Fact]
    public void Should_N_emettre_aucune_ecriture_et_garder_la_case_sur_Bruno_When_un_parent_annule_la_dialog_sans_valider()
    {
        // Given — l'API distante réelle ; la case du samedi 20/06 affiche « Bruno » (période semée
        // pour parent-b). Le client du front compte les écritures émises sur le canal.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 6, 20), new DateTime(2026, 6, 20));
        var compteur = new CompteurEcrituresHandler(api.Server.CreateHandler());
        var clientCompteur = new HttpClient(compteur) { BaseAddress = api.Server.BaseAddress };

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Samedi_20_06_2026, clientCompteur);

        // … la case du 20/06 affiche bien « Bruno » au départ.
        Assert.Equal("Bruno",
            GrilleRuntimeHarness.CaseDuJour(grille, "20/06").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // When — un Parent ouvre la dialog « Affecter une période » depuis la case, choisit « Alice »
        // (parent-a), puis ANNULE sans valider (ouverture idempotente, robuste aux re-renders du hub).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "20/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-affecter-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-responsable']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-affecter-periode'] [data-testid='dialog-annuler']").Click());

        // Then — la dialog se ferme, AUCUNE écriture n'a été émise sur le canal, et la case reste « Bruno ».
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
                Assert.Equal("Bruno",
                    GrilleRuntimeHarness.CaseDuJour(grille, "20/06").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));
        Assert.Equal(0, compteur.Ecritures); // aucune écriture émise (spy canal) — règle 14

        // … et le store distant n'a reçu aucune affectation d'Alice : seul Bruno reste responsable du 20/06.
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
        Assert.Equal("Bruno", grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 20)).NomResponsable);
        Assert.DoesNotContain(grilleStore.Jours, j => j.NomResponsable == "Alice");
    }
}
