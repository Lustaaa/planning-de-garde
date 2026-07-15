using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 45 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : une délégation de PLAGE que le DOMAINE
/// REFUSE (ici <c>fin &lt; début</c> — plage vide) laisse le mini-dialog OUVERT, affiche le MOTIF de refus,
/// et CONSERVE la saisie — l'acteur choisi ET la PLAGE (« jusqu'au »). Le store réel reste INTACT (aucune
/// écriture partielle sur la plage). La fermeture ne survient que sur Annuler / Échap ou sur un succès.
///
/// Anti « vert qui ment » : le refus provient du DOMAINE réel (via le canal d'écriture réel de l'API
/// distante), pas d'une doublure de transport — le motif affiché est celui renvoyé par le handler, et la
/// conservation de la PLAGE (champ « jusqu'au ») est observée sur le DOM réellement re-rendu.
/// </summary>
public sealed class FrontWasmDeleguerPlageRefusResteOuverteTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026

    [Fact]
    public void Un_refus_de_plage_laisse_le_dialog_ouvert_avec_motif_et_saisie_plage_conservee_store_intact()
    {
        // Given — grille câblée réelle (store vierge), Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — ouvrir « déléguer ce jour » (29/06) via le menu clic-case, choisir Alice (parent-a) et porter
        // « jusqu'au » AU 28/06 (AVANT le début) → plage vide REFUSÉE par le domaine (bornes invalides).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-deleguer']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-jusqu-au']").Change("2026-06-28"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — le mini-dialog RESTE OUVERT, affiche un MOTIF, et CONSERVE la saisie : acteur ET plage.
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
                Assert.False(string.IsNullOrWhiteSpace(grille.Find("[data-testid='motif-echec-deleguer']").TextContent));
                Assert.Equal("parent-a", grille.Find("[data-testid='champ-delegataire']").GetAttribute("value"));
                Assert.Equal("2026-06-28", grille.Find("[data-testid='champ-jusqu-au']").GetAttribute("value"));
            },
            TimeSpan.FromSeconds(10));

        // … et le store réel reste INTACT : AUCUNE période écrite (aucune écriture partielle sur la plage).
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }
}
