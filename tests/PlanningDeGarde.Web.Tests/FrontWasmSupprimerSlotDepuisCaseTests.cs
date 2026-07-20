using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@nominal</c> — 6ᵉ usage du menu clic-case). Le
/// comportement neuf vit dans le <c>.razor</c> : cliquer une case ouvre le menu d'actions dont
/// « Supprimer un slot » ouvre une dialog qui <b>liste les slots couvrant la date</b> (canal de lecture
/// réel, <c>GET /api/slots/…</c>) ; supprimer l'un d'eux transite par le canal d'écriture réel
/// (<c>POST /api/canal/supprimer-slot</c>), lève un <b>accusé non bloquant « Slot supprimé »</b> et fait
/// <b>disparaître le slot de la case</b> à la relecture de la grille distante — les autres slots restent.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à
/// une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, projection réelle). Anti
/// « vert qui ment » : le baseline (les deux slots rendus) est asserté avant ; si la dialog ne liste pas,
/// si la suppression ne transite pas jusqu'au store distant, ou si la grille ne se relit pas, l'observable
/// reste figé → rouge. Un bUnit à doublure ne verrait ni le câblage distant ni la relecture réelle.
/// </summary>
public sealed class FrontWasmSupprimerSlotDepuisCaseTests : TestContext
{
    // Mardi 16/06/2026 : la case ciblée. Référence « aujourd'hui » au 16/06 → fenêtre démarrant au
    // lundi 15/06, qui couvre le mardi 16/06.
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    private static bool CaseContientSlot(AngleSharp.Dom.IElement caseJour, string libelle)
        => caseJour.QuerySelectorAll("[data-testid='slot-case']")
            .Any(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Trim() == libelle);

    [Fact]
    public void Should_Supprimer_le_slot_Piscine_via_la_dialog_avec_accuse_puis_laisser_Ecole_dans_la_case_When_un_parent_supprime_depuis_le_menu_clic_case()
    {
        // Given — la grille réellement câblée à l'API distante, pour un Parent. Deux slots de Léa le
        // mardi 16/06/2026 : « École » 08h30-12h00 et « Piscine » 14h00-15h30.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerSlot(api, GrilleRuntimeHarness.EnfantParDefaut, "École", new DateTime(2026, 6, 16, 8, 30, 0), new DateTime(2026, 6, 16, 12, 0, 0));
        GrilleRuntimeHarness.SemerSlot(api, GrilleRuntimeHarness.EnfantParDefaut, "Piscine", new DateTime(2026, 6, 16, 14, 0, 0), new DateTime(2026, 6, 16, 15, 30, 0));

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … baseline : la case du 16/06 rend les deux slots « École » et « Piscine ».
        var case16 = GrilleRuntimeHarness.CaseDuJour(grille, "16/06");
        Assert.True(CaseContientSlot(case16, "École"), "la case du 16/06 doit rendre le slot École au départ.");
        Assert.True(CaseContientSlot(case16, "Piscine"), "la case du 16/06 doit rendre le slot Piscine au départ.");

        // When — clic sur la case du 16/06 → menu d'actions → « Supprimer un slot » → la dialog s'ouvre.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-supprimer-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-supprimer-slot']"));
            },
            TimeSpan.FromSeconds(10));

        // … la dialog liste, depuis le canal de lecture réel, les slots couvrant le 16/06 dont École et Piscine.
        grille.WaitForAssertion(
            () =>
            {
                var lignes = grille.FindAll("[data-testid='slot-supprimable']");
                Assert.Contains(lignes, l => l.TextContent.Contains("École"));
                Assert.Contains(lignes, l => l.TextContent.Contains("Piscine"));
            },
            TimeSpan.FromSeconds(10));

        // … on supprime le slot « Piscine » dans la dialog.
        this.SurDispatcher(() => grille.FindAll("[data-testid='slot-supprimable']")
            .Single(l => l.TextContent.Contains("Piscine"))
            .QuerySelector("[data-testid='bouton-supprimer-slot']")!.Click());

        // Then — un accusé « Slot supprimé » s'affiche à part (non bloquant), la case du 16/06 ne montre
        // plus Piscine mais montre encore École.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("Slot supprimé",
                    grille.Find("[data-testid='accuse-slot-supprime']").TextContent.Trim().TrimEnd('×').Trim());
                var caseMardi = GrilleRuntimeHarness.CaseDuJour(grille, "16/06");
                Assert.False(CaseContientSlot(caseMardi, "Piscine"), "la case du 16/06 ne doit plus rendre Piscine.");
                Assert.True(CaseContientSlot(caseMardi, "École"), "la case du 16/06 doit encore rendre École.");
            },
            TimeSpan.FromSeconds(10));

        // … et le slot Piscine a réellement disparu du store de l'API distante (rempart anti vert-qui-ment),
        // observé via la lecture réelle des slots couvrant le 16/06 ; École demeure.
        using var scope = api.Services.CreateScope();
        var slotsDuJour = scope.ServiceProvider.GetRequiredService<SlotsDuJourQuery>().Lister(new DateOnly(2026, 6, 16));
        Assert.DoesNotContain(slotsDuJour, s => s.LieuId == "Piscine");
        Assert.Contains(slotsDuJour, s => s.LieuId == "École");
    }
}
