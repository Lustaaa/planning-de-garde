using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ scénario IHM, <c>@limite</c> 🏷️ caractérisation — palier 7,
/// 3ᵉ dialog) : <b>caractérisation (filet)</b> de l'issue d'annulation (règle 14 — grille en lecture seule,
/// annuler n'émet aucune commande). On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP réel). Un Parent ouvre la dialog depuis une
/// case, choisit dépositaire/récupérateur, puis <b>annule sans valider</b> : la dialog se ferme, aucun
/// accusé « Transfert défini » ne s'affiche, et — rempart anti vert-qui-ment — <b>aucune écriture n'a
/// transité jusqu'au store réel</b> (vérifié vide AVANT l'annulation, comme témoin/baseline, ET après).
///
/// L'issue d'annulation (<c>OnAnnule</c>) est acquise par construction dès le câblage du Sc.1 (pattern vert
/// s11) : ce test est attendu <b>vert d'emblée</b> (caractérisation), pas un défaut. Le rouge surviendrait
/// si l'annulation émettait malgré tout une écriture (store non vide) ou laissait la dialog/accusé visible.
/// </summary>
public sealed class FrontWasmDefinirTransfertAnnulationTests : TestContext
{
    // Samedi 20/06/2026 : la case cliquée. Référence « aujourd'hui » au 20/06 → fenêtre démarrant au lundi
    // 15/06, qui couvre le samedi 20/06.
    private static readonly DateTime Samedi_20_06_2026 = new(2026, 6, 20);

    [Fact]
    public void Should_Fermer_la_dialog_sans_emettre_d_ecriture_ni_accuse_et_laisser_la_grille_intacte_When_un_parent_annule_la_dialog_de_transfert_sans_valider()
    {
        // Given — la grille réellement câblée à l'API distante, affichée pour un Parent, fenêtre couvrant le
        // samedi 20/06/2026.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Samedi_20_06_2026);

        // … témoin/baseline : AVANT toute action, le store réel ne contient aucun transfert et aucun accusé
        // n'est affiché.
        using (var scopeAvant = api.Services.CreateScope())
        {
            Assert.Empty(scopeAvant.ServiceProvider.GetRequiredService<ITransfertRepository>().AllSnapshots());
        }

        Assert.Empty(grille.FindAll("[data-testid='accuse-transfert-defini']"));

        // When — un Parent clique la case du samedi 20/06 → le menu d'actions s'ouvre → il choisit la 3ᵉ
        // entrée « Définir un transfert » → la dialog s'ouvre. Ouverture idempotente sous WaitForAssertion :
        // robuste aux re-renders async (connexion du hub SignalR du harnais) sous charge parallèle.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "20/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-definir-transfert']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));

        // … il choisit « Parent A » dépositaire et « Parent B » récupérateur (ids stables, règle 19), PUIS
        // annule sans valider.
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-recupere']").Change("parent-b"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='dialog-annuler']").Click());

        // Then — la dialog se ferme ET aucun accusé « Transfert défini » ne s'affiche (l'annulation n'est pas
        // un succès).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
                Assert.Empty(grille.FindAll("[data-testid='accuse-transfert-defini']"));
            },
            TimeSpan.FromSeconds(10));

        // … et — rempart anti vert-qui-ment — AUCUNE écriture n'a transité jusqu'au store de l'API distante :
        // l'annulation n'a laissé aucune trace (store toujours vide, comme avant).
        using var scopeApres = api.Services.CreateScope();
        Assert.Empty(scopeApres.ServiceProvider.GetRequiredService<ITransfertRepository>().AllSnapshots());
    }
}
