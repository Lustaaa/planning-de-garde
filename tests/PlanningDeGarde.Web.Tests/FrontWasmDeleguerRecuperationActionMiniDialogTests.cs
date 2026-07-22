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
/// Sprint 44 — Sc.4 (🖥️ @ihm), SWAP DE SURFACE (décision PO au gate G3) — acceptation de NIVEAU RUNTIME :
/// l'action « déléguer ce jour » est désormais une ENTRÉE DU MENU CLIC-CASE de la grille agenda
/// (<c>menu-actions-case</c>, à côté d'« Affecter une période » / « Définir un transfert »), et NON PLUS un
/// bouton posé sur la carte « Aujourd'hui » (s42) ni le panneau « À venir » (s43) — ces surfaces redeviennent
/// STRICTEMENT en LECTURE (invariant CLAUDE.md « les cartes de lecture n'hébergent pas d'écriture »).
/// L'entrée ouvre le mini-dialog de choix de l'acteur recevant ; valider émet la commande via le CANAL
/// D'ÉCRITURE (POST /api/delegations) qui COMPOSE l'écriture surcharge ponctuelle (s06) ;
/// Échap FERME sans émettre (port IEcouteurEchapModal s33) ; l'Invité ne voit ni le menu ni l'entrée
/// (Parent-gated). La grille est réellement câblée à l'API distante (store réel, projection réelle, canal réel).
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

    /// <summary>
    /// Ouvre le mini-dialog « déléguer ce jour » via l'ENTRÉE DU MENU CLIC-CASE (surface tranchée au gate G3) :
    /// clic sur la case-jour de <paramref name="jjMM"/> → le menu <c>menu-actions-case</c> s'ouvre → clic sur
    /// l'entrée <c>action-deleguer</c> → mini-dialog. Idempotent sous WaitForAssertion (robuste aux re-renders
    /// async de la connexion du hub).
    /// </summary>
    private void OuvrirDialogViaMenu(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-deleguer']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));

    [Fact]
    public void Un_parent_delegue_ce_jour_depuis_le_menu_clic_case_via_le_canal_decriture()
    {
        // Given — la grille réellement câblée à l'API distante (store réel vierge), Parent, jour courant
        // NEUTRE (aucun responsable) : la case du jour ne porte aucun nom de responsable. L'action « déléguer
        // ce jour » vit dans le MENU CLIC-CASE (la grille est la seule surface de lecture, s44 Sc.7).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        Assert.Null(GrilleRuntimeHarness.CaseDuJour(grille, "29/06").QuerySelector("[data-testid='nom-responsable']"));

        // When — le Parent clique la case du jour (29/06) → menu → entrée « déléguer ce jour » → mini-dialog,
        // choisit « Alice » (id stable parent-a) comme acteur recevant et valide.
        OuvrirDialogViaMenu(grille, "29/06");
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — la dialog se ferme ET la CASE de la grille converge vers « Alice » (surcharge du jour, relue du store).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']"));
                Assert.Equal(
                    "Alice",
                    GrilleRuntimeHarness.CaseDuJour(grille, "29/06").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
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
    public void Un_parent_delegue_un_jour_a_venir_depuis_le_menu_clic_case()
    {
        // Given — grille câblée réelle, Parent. On délègue un jour À VENIR (30/06, lendemain, dans la fenêtre
        // chargée) via le MENU CLIC-CASE de la grille — seule surface de lecture/action (s44 Sc.7).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        var date = new DateOnly(2026, 6, 30);

        // When — le Parent clique la case de ce jour → menu → entrée « déléguer ce jour » → choisit Alice → valide.
        OuvrirDialogViaMenu(grille, date.ToString("dd/MM"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — la dialog se ferme ET la case de ce jour à venir converge vers « Alice » (surcharge relue du store).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']"));
                Assert.Equal(
                    "Alice",
                    GrilleRuntimeHarness.CaseDuJour(grille, date.ToString("dd/MM")).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … transité jusqu'au store distant réel (projection réelle).
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        Assert.Equal("Alice", projection.Projeter(date).Jours.Single(j => j.Date == date).NomResponsable);
    }

    [Fact]
    public void Echap_ferme_le_mini_dialog_sans_emettre_aucune_commande()
    {
        // Given — grille câblée réelle, Parent, port Échap DOUBLÉ (spy). Le mini-dialog est ouvert depuis le menu clic-case.
        using var api = new ApiDistanteFactory();
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        OuvrirDialogViaMenu(grille, "29/06");
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
    public void La_page_ne_rend_ni_carte_ni_panneau_et_le_menu_porte_l_entree_deleguer()
    {
        // Given — grille câblée réelle, Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Then — les surfaces de lecture s42/s43 sont RETIRÉES (décision PO s44 Sc.7) : ni carte « Aujourd'hui »
        // ni panneau « À venir » ne sont rendus ; la grille est la seule surface de lecture/action.
        Assert.Empty(grille.FindAll("[data-testid='carte-aujourdhui']"));
        Assert.Empty(grille.FindAll("[data-testid='panneau-a-venir']"));

        // … et l'entrée « déléguer ce jour » est offerte par le MENU CLIC-CASE, à côté des actions Palier 7.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
        var menu = grille.Find("[data-testid='menu-actions-case']");
        Assert.NotNull(menu.QuerySelector("[data-testid='action-deleguer']"));
        Assert.NotNull(menu.QuerySelector("[data-testid='action-affecter-periode']"));
        Assert.NotNull(menu.QuerySelector("[data-testid='action-definir-transfert']"));
    }

    [Fact]
    public void L_invite_ne_voit_ni_le_menu_ni_l_entree_deleguer()
    {
        // Given — grille câblée réelle. When — l'identité effective bascule en Invité (consultation seule).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // Then — cliquer une case n'ouvre PAS le menu (Parent-gated, OuvrirMenu) : aucune commande de
        // délégation n'est émissible pour l'Invité (la grille reste consultable en lecture seule).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='action-deleguer']"));
    }
}
