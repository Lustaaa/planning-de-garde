using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 52 — Sc.9 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME sur la mini-dialog « proposer un échange » de plage :
/// (1) Échap = Annuler (port <see cref="IEcouteurEchapModal"/> s33, capture document) ferme la dialog SANS émettre
/// aucune proposition ; (2) un refus DOMAINE (<c>fin &lt; début</c>) laisse la dialog OUVERTE, affiche le MOTIF et
/// CONSERVE la saisie — l'acteur choisi ET la PLAGE (« jusqu'au ») — le store restant INTACT (0 proposition).
///
/// Anti « vert qui ment » : Échap capté au niveau document via le port réel (spy à la main) ; le refus provient du
/// DOMAINE réel (canal d'écriture réel de l'API distante), le motif et la conservation de la plage observés sur le
/// DOM réellement re-rendu.
/// </summary>
public sealed class FrontWasmProposerEchangePlageEchapEtRefusTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026

    /// <summary>Double à la main du port d'écoute Échap (spy document).</summary>
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

    private void OuvrirDialogViaMenu(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-proposer-echange']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-proposer']"));
            },
            TimeSpan.FromSeconds(10));

    [Fact]
    public void Echap_ferme_la_dialog_de_proposition_sans_emettre_aucune_proposition()
    {
        // Given — port Échap doublé enregistré AVANT le rendu ; grille câblée réelle, Parent.
        using var api = new ApiDistanteFactory();
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        OuvrirDialogViaMenu(grille, "29/06");
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-recevant']").Change("parent-a"));

        // When — Échap document.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la dialog est fermée (Annuler), AUCUNE proposition émise (store intact).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-proposer']")),
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots());
    }

    [Fact]
    public void Un_refus_domaine_laisse_la_dialog_ouverte_avec_motif_et_saisie_acteur_et_plage_conservee()
    {
        // Given — grille câblée réelle (store vierge), Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — ouvrir « proposer un échange » (29/06), choisir Alice (parent-a), porter « jusqu'au » AU 28/06
        // (AVANT le début) → plage vide REFUSÉE par le domaine (bornes invalides), puis valider.
        OuvrirDialogViaMenu(grille, "29/06");
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-recevant']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-jusqu-au']").Change("2026-06-28"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] form").Submit());

        // Then — la dialog RESTE OUVERTE, affiche un MOTIF, et CONSERVE la saisie : acteur ET plage.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-proposer']"));
                Assert.False(string.IsNullOrWhiteSpace(grille.Find("[data-testid='motif-echec-proposer']").TextContent));
                Assert.Equal("parent-a", grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-recevant']").GetAttribute("value"));
                Assert.Equal("2026-06-28", grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-jusqu-au']").GetAttribute("value"));
            },
            TimeSpan.FromSeconds(10));

        // … et le store réel reste INTACT : AUCUNE proposition écrite (aucune écriture partielle).
        Assert.Empty(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots());
    }
}
