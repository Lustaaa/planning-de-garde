using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ IHM, <c>@limite</c> — annulation). Ouvrir la dialog de
/// suppression de slot puis la <b>fermer sans supprimer</b> ne doit émettre <b>aucune</b> commande
/// d'écriture : le slot reste présent et la case reste inchangée. On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Planning.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel). Le bouton « Fermer » (<c>OnAnnule</c> → <c>FermerDialog</c>)
/// ne passe jamais par le canal d'écriture : preuve observable = aucun accusé « Slot supprimé », le slot
/// toujours relu depuis le store distant, la case affichant toujours « École ».
///
/// Anti « vert qui ment » : le baseline (slot École rendu) est asserté avant ; si la fermeture émettait une
/// suppression, le slot disparaîtrait de la case et du store → rouge.
/// </summary>
public sealed class FrontWasmSupprimerSlotAnnulationTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    private static bool CaseContientSlot(AngleSharp.Dom.IElement caseJour, string libelle)
        => caseJour.QuerySelectorAll("[data-testid='slot-case']")
            .Any(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Trim() == libelle);

    [Fact]
    public void Should_Laisser_le_slot_et_la_case_du_16_06_inchanges_When_un_parent_ferme_la_dialog_de_suppression_sans_supprimer()
    {
        // Given — la grille câblée à l'API distante, pour un Parent ; un slot « École » 08h30-16h30 pour
        // Léa le mardi 16/06/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerSlot(api, GrilleRuntimeHarness.EnfantParDefaut, "École", new DateTime(2026, 6, 16, 8, 30, 0), new DateTime(2026, 6, 16, 16, 30, 0));

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … baseline : la case du 16/06 rend le slot « École ».
        Assert.True(CaseContientSlot(GrilleRuntimeHarness.CaseDuJour(grille, "16/06"), "École"),
            "la case du 16/06 doit rendre le slot École au départ.");

        // When — j'ouvre la dialog de suppression (menu clic-case → « Supprimer un slot »).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-supprimer-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-supprimer-slot']"));
            },
            TimeSpan.FromSeconds(10));

        // … la dialog liste le slot École (chargement async, attendu sans re-cliquer la case).
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='slot-supprimable']"),
                l => l.TextContent.Contains("École")),
            TimeSpan.FromSeconds(10));

        // … et je ferme la dialog SANS supprimer (bouton « Fermer »).
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-supprimer-slot'] [data-testid='dialog-annuler']").Click());

        // Then — la dialog est fermée, AUCUNE commande de suppression n'a été émise : aucun accusé
        // « Slot supprimé », la case du 16/06 affiche toujours le slot « École ».
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-supprimer-slot']"));
                Assert.Empty(grille.FindAll("[data-testid='accuse-slot-supprime']"));
                Assert.True(CaseContientSlot(GrilleRuntimeHarness.CaseDuJour(grille, "16/06"), "École"),
                    "la case du 16/06 doit encore rendre le slot École après l'annulation.");
            },
            TimeSpan.FromSeconds(10));

        // … et le slot est toujours présent dans le store de l'API distante (rien n'a été supprimé).
        using var scope = api.Services.CreateScope();
        var slotsDuJour = scope.ServiceProvider.GetRequiredService<SlotsDuJourQuery>();
        Assert.Contains(slotsDuJour.Lister(new DateOnly(2026, 6, 16)), s => s.LieuId == "École");
    }
}
