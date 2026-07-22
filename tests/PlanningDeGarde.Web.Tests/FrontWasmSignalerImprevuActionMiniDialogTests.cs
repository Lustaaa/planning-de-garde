using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 48 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : l'entrée « signaler un imprévu » du MENU
/// CLIC-CASE de la grille agenda (à côté de « déléguer ce jour » s44 / « proposer un échange » s47), Parent-gated.
/// L'entrée ouvre un mini-dialog de choix du type (malade / retard) + un champ motif OPTIONNEL ; valider émet la
/// commande via le CANAL D'ÉCRITURE (POST /api/imprevus) qui consigne une trace au JOURNAL DE
/// CHANGEMENTS existant (s47) — purement INFORMATIF : le signalement N'ÉCRIT AUCUNE surcharge (la résolution du
/// planning reste STRICTEMENT inchangée, invariant s48). Échap FERME sans émettre (port IEcouteurEchapModal s33) ;
/// l'Invité ne voit ni le menu ni l'entrée (Parent-gated). La grille est réellement câblée à l'API distante
/// (store réel, journal réel, canal réel).
///
/// Anti « vert qui ment » : l'événement d'imprévu est prouvé jusqu'au journal distant réel, ET l'absence
/// d'écriture de résolution est prouvée sur le store des périodes réel — jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmSignalerImprevuActionMiniDialogTests : TestContext
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

    /// <summary>Ouvre le mini-dialog « signaler un imprévu » via l'ENTRÉE DU MENU CLIC-CASE : clic sur la case-jour
    /// de <paramref name="jjMM"/> → le menu <c>menu-actions-case</c> s'ouvre → clic sur l'entrée
    /// <c>action-signaler-imprevu</c> → mini-dialog. Idempotent sous WaitForAssertion (robuste aux re-renders async
    /// de la connexion du hub).</summary>
    private void OuvrirDialogViaMenu(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-signaler-imprevu']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-signaler-imprevu']"));
            },
            TimeSpan.FromSeconds(10));

    [Fact]
    public void Le_menu_porte_l_entree_signaler_imprevu_et_le_dialog_offre_type_et_motif()
    {
        // Given — la grille réellement câblée à l'API distante, Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Then — l'entrée « signaler un imprévu » est offerte par le menu clic-case, à côté des actions palier 5/7.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
        var menu = grille.Find("[data-testid='menu-actions-case']");
        Assert.NotNull(menu.QuerySelector("[data-testid='action-signaler-imprevu']"));

        // … et l'ouvrir déroule un mini-dialog offrant le choix du TYPE (malade / retard) ET un champ motif OPTIONNEL.
        OuvrirDialogViaMenu(grille, "29/06");
        var dialog = grille.Find("[data-testid='dialog-signaler-imprevu']");
        Assert.NotNull(dialog.QuerySelector("[data-testid='champ-type-imprevu']"));
        Assert.NotNull(dialog.QuerySelector("[data-testid='champ-motif-imprevu']"));
    }

    [Fact]
    public void Un_parent_signale_un_imprevu_via_le_canal_decriture_consigne_au_journal_SANS_surcharge()
    {
        // Given — la grille réellement câblée (store réel vierge), Parent (identité effective = « configurateur »).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — le Parent clique la case du jour (29/06) → menu → « signaler un imprévu » → choisit « retard »,
        // saisit un motif, et valide (émission par le canal d'écriture).
        OuvrirDialogViaMenu(grille, "29/06");
        this.SurDispatcher(() => grille.Find("[data-testid='champ-type-imprevu']").Change("Retard"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-motif-imprevu']").Change("bouchons A6"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-signaler-imprevu'] form").Submit());

        // Then — la dialog se ferme ET un événement d'imprévu (retard) est consigné au JOURNAL distant réel,
        // signalé par l'identité effective de la session — rempart anti vert-qui-ment (journal réel).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-signaler-imprevu']"));
                Assert.Contains(
                    api.Services.GetRequiredService<IJournalChangements>().Tout(),
                    e => e.Type == TypeChangement.Imprevu && e.Jour == new DateOnly(2026, 6, 29)
                        && e.Imprevu == TypeImprevu.Retard && e.RecevantId == "configurateur" && e.Motif == "bouchons A6");
            },
            TimeSpan.FromSeconds(10));

        // … et INVARIANT s48 : le signalement N'A ÉCRIT AUCUNE surcharge (la résolution du planning est intacte).
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        Assert.Empty(api.Services.GetRequiredService<ITransfertRepository>().AllSnapshots());
    }

    [Fact]
    public void Echap_ferme_le_mini_dialog_sans_emettre_aucune_commande()
    {
        // Given — grille câblée réelle, Parent, port Échap DOUBLÉ (spy). Le mini-dialog est ouvert depuis le menu.
        using var api = new ApiDistanteFactory();
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        OuvrirDialogViaMenu(grille, "29/06");
        // La modal ouverte a ATTACHÉ l'écouteur document (contrat câblé, capture au niveau document s33).
        grille.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-motif-imprevu']").Change("saisie en cours"));

        // When — Échap document (rejoué via le callback capté par le spy).
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la dialog se ferme SANS émettre : le journal ne porte AUCUN imprévu, le store aucune surcharge.
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-signaler-imprevu']")),
            TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(
            api.Services.GetRequiredService<IJournalChangements>().Tout(),
            e => e.Type == TypeChangement.Imprevu);
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }

    [Fact]
    public void L_invite_ne_voit_ni_le_menu_ni_l_entree_signaler_imprevu()
    {
        // Given — grille câblée réelle. When — l'identité effective bascule en Invité (consultation seule).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // Then — cliquer une case n'ouvre PAS le menu (Parent-gated) : aucune commande d'imprévu n'est émissible
        // pour l'Invité (la grille reste consultable en lecture seule, cohérent s44/s47).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='action-signaler-imprevu']"));
    }
}
