using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 44 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : une délégation que le DOMAINE REFUSE
/// (ici « à soi-même » — le délégataire choisi récupère déjà ce jour-là) laisse le mini-dialog OUVERT,
/// affiche le MOTIF de refus, CONSERVE la saisie (acteur choisi), et le store réel reste INTACT (aucune
/// écriture partielle). La fermeture ne survient que sur Annuler / Échap ou sur un succès.
///
/// Anti « vert qui ment » : le refus provient du DOMAINE réel (via le canal d'écriture réel de l'API
/// distante), pas d'une doublure de transport — le motif affiché est celui renvoyé par le handler.
/// </summary>
public sealed class FrontWasmDeleguerRefusResteOuverteTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026

    [Fact]
    public void Un_refus_domaine_laisse_le_dialog_ouvert_avec_motif_et_saisie_conservee_store_intact()
    {
        // Given — le jour courant est DÉJÀ résolu à parent-a (« Alice ») via une surcharge semée : déléguer à
        // parent-a serait une délégation À SOI-MÊME, refusée par le domaine. Grille câblée réelle, Parent.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", Aujourdhui, Aujourdhui);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        Assert.Equal(
            "Alice",
            grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-qui']").TextContent.Trim());

        // When — le Parent ouvre « déléguer ce jour » sur la carte, choisit Alice (parent-a = soi-même) et valide.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => grille.Find("[data-testid='carte-aujourdhui'] [data-testid='carte-deleguer']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — le mini-dialog RESTE OUVERT, affiche un MOTIF de refus, et CONSERVE la saisie (parent-a).
        grille.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-deleguer']"));
                var motif = grille.Find("[data-testid='motif-echec-deleguer']").TextContent;
                Assert.False(string.IsNullOrWhiteSpace(motif));
                Assert.Equal("parent-a", grille.Find("[data-testid='champ-delegataire']").GetAttribute("value"));
            },
            TimeSpan.FromSeconds(10));

        // … et le store réel reste INTACT : toujours l'UNIQUE surcharge parent-a semée (aucune écriture partielle).
        var periodes = api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots();
        Assert.Equal("parent-a", Assert.Single(periodes).ResponsableId);
    }
}
