using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 45 — Sc.4 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le mini-dialog EXISTANT « déléguer ce jour »
/// (ouvert depuis l'entrée <c>action-deleguer</c> du <c>menu-actions-case</c>, s44) est ENRICHI d'un champ
/// « jusqu'au » (date de fin de plage). Son DÉFAUT = le jour cliqué (fin = début) → la délégation d'UN jour
/// (s44) reste STRICTEMENT inchangée. Choisir un acteur recevant + une date de fin postérieure puis valider
/// émet la commande de délégation <c>[début..fin]</c> via le CANAL D'ÉCRITURE (POST /api/canal/deleguer-recuperation)
/// qui COMPOSE l'écriture surcharge MULTI-JOURS (s06) : CHAQUE case de la plage converge vers le délégataire.
///
/// Anti « vert qui ment » : la grille est câblée à l'API distante RÉELLE (store réel, projection réelle,
/// canal réel) ; la délégation est prouvée jusqu'au store distant (surcharge multi-jours relue par la
/// projection réelle), jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmDeleguerPlageMiniDialogTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026

    [Fact]
    public void Le_champ_jusqu_au_a_pour_defaut_le_jour_clique()
    {
        // Given — grille câblée réelle, Parent. When — ouvrir le mini-dialog via le menu clic-case du 29/06.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        OuvrirDialogViaMenu(grille, "29/06");

        // Then — le mini-dialog porte un champ « jusqu'au » (date de fin) dont le DÉFAUT est le jour cliqué.
        var champFin = grille.Find("[data-testid='champ-jusqu-au']");
        Assert.Equal("2026-06-29", champFin.GetAttribute("value"));
    }

    [Fact]
    public void Un_parent_delegue_une_PLAGE_et_chaque_case_converge_vers_le_delegataire()
    {
        // Given — grille câblée réelle (store vierge → cases neutres), Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — ouvrir le mini-dialog (29/06), choisir Alice (parent-a), porter « jusqu'au » au 01/07, valider.
        OuvrirDialogViaMenu(grille, "29/06");
        this.SurDispatcher(() => grille.Find("[data-testid='champ-delegataire']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-jusqu-au']").Change("2026-07-01"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-deleguer'] form").Submit());

        // Then — la dialog se ferme ET CHAQUE case de la plage [29/06 .. 01/07] converge vers « Alice ».
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-deleguer']"));
                foreach (var jjMM in new[] { "29/06", "30/06", "01/07" })
                    Assert.Equal(
                        "Alice",
                        GrilleRuntimeHarness.CaseDuJour(grille, jjMM).QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … et la délégation a réellement transité jusqu'au store distant : UNE surcharge MULTI-JOURS
        // [29/06 .. 01/07] responsable parent-a, observée via la projection réelle (rempart anti vert-qui-ment).
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        foreach (var d in new[] { new DateOnly(2026, 6, 29), new DateOnly(2026, 6, 30), new DateOnly(2026, 7, 1) })
            Assert.Equal("Alice", projection.Projeter(d).Jours.Single(j => j.Date == d).NomResponsable);
    }

    /// <summary>
    /// Ouvre le mini-dialog « déléguer ce jour » via l'ENTRÉE DU MENU CLIC-CASE (s44) : clic sur la case-jour
    /// de <paramref name="jjMM"/> → menu → clic sur <c>action-deleguer</c> → mini-dialog. Idempotent sous
    /// WaitForAssertion (robuste aux re-renders async de la connexion du hub).
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
}
