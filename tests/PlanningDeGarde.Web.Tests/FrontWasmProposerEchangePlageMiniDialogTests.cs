using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 52 — Sc.7 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : le mini-dialog EXISTANT « proposer un échange »
/// (s47, ouvert depuis l'entrée <c>action-proposer-echange</c> du <c>menu-actions-case</c>, Parent-gated) est
/// ENRICHI d'un champ « jusqu'au » (miroir EXACT du champ ajouté à la dialog de délégation en s45). Son DÉFAUT =
/// le jour cliqué (fin = début) → l'échange d'UN jour (s47) reste STRICTEMENT inchangé. Choisir un recevant + une
/// date de fin postérieure puis valider émet la proposition d'échange sur la PLAGE <c>[jour cliqué..J3]</c> via le
/// CANAL D'ÉCRITURE (POST /api/canal/proposer-echange) — SANS aucune écriture de surcharge (canal de consentement) :
/// une Proposition <c>pending</c> portant l'intervalle est créée chez le recevant.
///
/// Anti « vert qui ment » : la grille est câblée à l'API distante RÉELLE (store réel, canal réel) ; la proposition
/// est prouvée jusqu'au store distant (pending portant l'intervalle, 0 surcharge), jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmProposerEchangePlageMiniDialogTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // Lundi 29/06/2026

    [Fact]
    public void Le_champ_jusqu_au_a_pour_defaut_le_jour_clique()
    {
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        OuvrirDialogViaMenu(grille, "29/06");

        // Le mini-dialog porte un champ « jusqu'au » dont le DÉFAUT est le jour cliqué (parité s47 : fin = début).
        var champFin = grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-jusqu-au']");
        Assert.Equal("2026-06-29", champFin.GetAttribute("value"));
    }

    [Fact]
    public void Un_parent_propose_un_echange_sur_une_PLAGE_qui_cree_une_pending_portant_l_intervalle_sans_ecriture()
    {
        // Store VIERGE (comme la délégation-plage s45) : les cases sont neutres → le cédant résolu est distinct
        // du recevant parent-a, la proposition n'est jamais un « échange à soi-même ».
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Ouvrir le mini-dialog (29/06), choisir Alice (parent-a), porter « jusqu'au » au 01/07, valider.
        OuvrirDialogViaMenu(grille, "29/06");
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-recevant']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] [data-testid='champ-jusqu-au']").Change("2026-07-01"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] form").Submit());

        // La dialog se ferme (succès). Une Proposition pending portant l'intervalle [29/06..01/07] est créée au
        // store distant, vers parent-a, SANS aucune surcharge écrite (canal de consentement).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-proposer']")),
            TimeSpan.FromSeconds(10));

        var proposition = api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots()
            .Single(p => p.VersActeurId == "parent-a");
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(new DateOnly(2026, 6, 29), proposition.Jour);
        Assert.Equal(new DateOnly(2026, 7, 1), proposition.JourFin);
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
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
}
