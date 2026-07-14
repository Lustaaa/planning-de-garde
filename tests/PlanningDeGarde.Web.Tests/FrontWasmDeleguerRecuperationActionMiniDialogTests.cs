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
/// Sprint 44 — Sc.4 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : la carte « Aujourd'hui » (s42) ET le panneau
/// « À venir » (s43) HÉBERGENT une action « déléguer ce jour » ouvrant un mini-dialog de choix de l'acteur
/// recevant ; valider émet la commande via le CANAL D'ÉCRITURE (POST /api/canal/deleguer-recuperation) qui
/// COMPOSE l'écriture surcharge ponctuelle (s06) ; Échap FERME sans émettre (port IEcouteurEchapModal s33) ;
/// l'Invité ne voit AUCUNE action (Parent-gated). La grille et sa carte sont réellement câblées à l'API
/// distante (store réel, projection réelle, canal d'écriture réel).
///
/// Anti « vert qui ment » : la délégation est prouvée jusqu'au store distant réel (relecture par la projection
/// réelle), jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmDeleguerRecuperationActionMiniDialogTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026

    /// <summary>Double à la main du port d'écoute Échap (spy) : capte le callback d'attache et rejoue Échap document.</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;
        public int Attachements { get; private set; }

        public ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
        {
            Attachements++;
            _onEchap = onEchap;
            return ValueTask.FromResult<IAsyncDisposable>(new Abonnement(this));
        }

        public Task DeclencherEchapDocument() => _onEchap?.Invoke() ?? Task.CompletedTask;

        private sealed class Abonnement : IAsyncDisposable
        {
            private readonly EspionEchap _espion;
            public Abonnement(EspionEchap espion) => _espion = espion;
            public ValueTask DisposeAsync() { _espion._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    [Fact]
    public void Un_parent_delegue_ce_jour_depuis_la_carte_via_le_canal_decriture()
    {
        // Given — la grille réellement câblée à l'API distante (store réel vierge), Parent, jour courant
        // NEUTRE (aucun responsable) : la carte affiche « Personne assignée » et porte l'action « déléguer ».
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        Assert.Equal(
            "Personne assignée",
            grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-qui']").TextContent.Trim());

        // When — le Parent clique « déléguer ce jour » sur la carte → le mini-dialog s'ouvre. Ouverture
        // idempotente sous WaitForAssertion (robuste aux re-renders async de la connexion du hub).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-deleguer']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));

        // … il choisit « Alice » (id stable parent-a) comme acteur recevant et valide.
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — la dialog se ferme ET la carte converge vers « Alice » (surcharge du jour, relue du store).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']"));
                Assert.Equal(
                    "Alice",
                    grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-qui']").TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … et la délégation a réellement transité jusqu'au store de l'API distante (rempart anti
        // vert-qui-ment) : une surcharge d'UN jour (29/06) au responsable parent-a, observée via la projection réelle.
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var caseStore = projection.Projeter(new DateOnly(2026, 6, 29))
            .Jours.Single(j => j.Date == new DateOnly(2026, 6, 29));
        Assert.Equal("Alice", caseStore.NomResponsable);
    }

    [Fact]
    public void Un_parent_delegue_un_jour_a_venir_depuis_le_panneau()
    {
        // Given — grille câblée réelle, Parent. Le panneau « À venir » porte une action « déléguer » par jour.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        var jour = grille.FindAll("[data-testid='a-venir-jour']").First();
        var dateIso = jour.GetAttribute("data-date")!;

        // When — le Parent clique « déléguer » sur le 1ᵉʳ jour à-venir → mini-dialog → choisit Alice → valide.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => grille.FindAll("[data-testid='a-venir-deleguer']")
                    .First(b => b.GetAttribute("data-date") == dateIso).Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — la dialog se ferme ET ce jour du panneau converge vers « Alice » (surcharge relue du store).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']"));
                var jourApres = grille.FindAll("[data-testid='a-venir-jour']").Single(j => j.GetAttribute("data-date") == dateIso);
                Assert.Equal("Alice", jourApres.QuerySelector("[data-testid='a-venir-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … transité jusqu'au store distant réel (projection réelle).
        var date = DateOnly.Parse(dateIso);
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        Assert.Equal("Alice", projection.Projeter(date).Jours.Single(j => j.Date == date).NomResponsable);
    }

    [Fact]
    public void Echap_ferme_le_mini_dialog_sans_emettre_aucune_commande()
    {
        // Given — grille câblée réelle, Parent, port Échap DOUBLÉ (spy). Le mini-dialog est ouvert depuis la carte.
        using var api = new ApiDistanteFactory();
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-deleguer']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        // La modal ouverte a ATTACHÉ l'écouteur document (contrat câblé, capture au niveau document s33).
        grille.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));

        // When — Échap document (rejoué via le callback capté par le spy).
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la dialog se ferme SANS émettre : le store distant ne porte AUCUNE surcharge le jour courant.
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']")),
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }

    [Fact]
    public void L_invite_ne_voit_aucune_action_deleguer_sur_la_carte_ni_le_panneau()
    {
        // Given — grille câblée réelle. When — l'identité effective bascule en Invité (consultation seule).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // Then — AUCUNE action « déléguer ce jour » n'est rendue (Parent-gated), ni sur la carte ni dans le panneau.
        Assert.Empty(grille.FindAll("[data-testid='carte-deleguer']"));
        Assert.Empty(grille.FindAll("[data-testid='a-venir-deleguer']"));
    }
}
