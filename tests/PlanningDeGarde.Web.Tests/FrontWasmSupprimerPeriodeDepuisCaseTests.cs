using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@nominal</c> — 4ᵉ usage du menu clic-case). Le
/// comportement neuf vit dans le <c>.razor</c> : cliquer une case ouvre le menu d'actions dont
/// « Supprimer une période » ouvre une dialog qui <b>liste les périodes couvrant la date</b> (canal de
/// lecture réel, <c>GET /api/periodes/…</c>) ; supprimer l'une d'elles transite par le canal d'écriture
/// réel (<c>POST /api/canal/supprimer-periode</c>), lève un <b>accusé non bloquant « Période supprimée »</b>
/// et fait <b>re-résoudre la case</b> (repli surcharge &gt; fond) à la relecture de la grille distante.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à
/// une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, projection + référentiel
/// réels). Anti « vert qui ment » : le baseline « Nina la nounou » est asserté avant ; si la dialog ne
/// liste pas, si la suppression ne transite pas jusqu'au store distant, ou si la grille ne se re-résout
/// pas, l'observable reste figé → rouge. Un bUnit à doublure ne verrait ni le câblage distant ni la
/// résolution réelle du foyer.
/// </summary>
public sealed class FrontWasmSupprimerPeriodeDepuisCaseTests : TestContext
{
    // Mardi 16/06/2026 : la case ciblée. Référence « aujourd'hui » au 16/06 → fenêtre démarrant au
    // lundi 15/06, qui couvre le mardi 16/06.
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Supprimer_la_periode_de_Nounou_via_la_dialog_puis_faire_retomber_la_case_du_16_06_sur_Parent_A_avec_accuse_When_un_parent_supprime_depuis_le_menu_clic_case()
    {
        // Given — la grille réellement câblée à l'API distante, pour un Parent. Le cycle de fond
        // (N=1, index 0 → parent-a) attribue « Alice » à toute la fenêtre ; une période « Nina la
        // nounou » surcharge le mardi 16/06/2026 (prime sur le fond).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IReferentielCycleDeFond>()
            .DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = "parent-a" }), GrilleRuntimeHarness.EnfantParDefaut);
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … baseline : la surcharge prime → la case du 16/06 porte « Nina la nounou », et la légende
        // dédoublonnée le fait apparaître.
        Assert.Equal("Nina la nounou", GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Contains(grille.FindAll("[data-testid='legende-entree']"),
            e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");

        // When — clic sur la case du 16/06 → menu d'actions → « Supprimer une période » → la dialog s'ouvre.
        // On asserte l'OUVERTURE de la dialog (synchrone) dans le bloc de clic — pas son contenu async —
        // pour ne pas re-cliquer la case en boucle pendant le chargement (sinon le menu se ré-ouvre sur la
        // dialog : re-render ré-entrant). Ouverture idempotente sous WaitForAssertion (robuste aux re-renders
        // async du hub SignalR du harnais).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-supprimer-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-supprimer-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // … la dialog liste, depuis le canal de lecture réel, les périodes couvrant le 16/06 dont celle de
        // « Nina la nounou » (chargement async, attendu SANS re-cliquer la case).
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='periode-supprimable']"),
                l => l.TextContent.Contains("Nina la nounou")),
            TimeSpan.FromSeconds(10));

        // … on supprime la période de « Nina la nounou » dans la dialog.
        this.SurDispatcher(() => grille.FindAll("[data-testid='periode-supprimable']")
            .Single(l => l.TextContent.Contains("Nina la nounou"))
            .QuerySelector("[data-testid='bouton-supprimer-periode']")!.Click());

        // Then — un accusé « Période supprimée » s'affiche à part (non bloquant), la case du 16/06 retombe
        // sur « Alice » / « bleu » (repli surcharge > fond), et la légende ne fait plus apparaître Nounou.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("Période supprimée",
                    grille.Find("[data-testid='accuse-periode-supprimee']").TextContent.Trim().TrimEnd('×').Trim());
                var caseMardi = GrilleRuntimeHarness.CaseDuJour(grille, "16/06");
                Assert.Equal("Alice", caseMardi.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                Assert.Equal("bleu", caseMardi.GetAttribute("data-couleur"));
                Assert.DoesNotContain(grille.FindAll("[data-testid='legende-entree']"),
                    e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Nina la nounou");
            },
            TimeSpan.FromSeconds(10));

        // … et la période a réellement disparu du store de l'API distante (rempart anti vert-qui-ment),
        // observée via la lecture réelle des périodes couvrant le 16/06.
        using var scope = api.Services.CreateScope();
        var periodesDuJour = scope.ServiceProvider.GetRequiredService<PeriodesDuJourQuery>();
        Assert.Empty(periodesDuJour.Lister(new DateOnly(2026, 6, 16)));
    }
}
