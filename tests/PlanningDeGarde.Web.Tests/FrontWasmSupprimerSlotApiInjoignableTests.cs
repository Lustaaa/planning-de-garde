using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.9 (🖥️ IHM, <c>@erreur</c> — règle 28). Une suppression dont le
/// <c>POST /api/canal/supprimer-slot</c> n'atteint pas l'API distante (échec de transport déterministe via
/// <see cref="GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable"/>) doit : afficher un message d'échec
/// clair <b>dans</b> la dialog, <b>laisser la dialog ouverte</b>, ne <b>rien</b> appliquer (le slot reste,
/// la case inchangée), et n'effectuer <b>aucune mise en file ni rejeu</b>.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Planning.PlanningPartage"/> (front WASM) câblée à
/// l'<b>API distante réelle</b> ; seule l'écriture de suppression est coupée au transport (la lecture des
/// slots de la dialog transite normalement, comme en condition réelle). Anti « vert qui ment » : le slot
/// est observé toujours présent dans le store distant après l'échec.
/// </summary>
public sealed class FrontWasmSupprimerSlotApiInjoignableTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    private static bool CaseContientSlot(AngleSharp.Dom.IElement caseJour, string libelle)
        => caseJour.QuerySelectorAll("[data-testid='slot-case']")
            .Any(s => s.QuerySelector(".grille-slot-libelle")!.TextContent.Trim() == libelle);

    [Fact]
    public void Should_Garder_la_dialog_ouverte_avec_message_d_echec_et_ne_rien_appliquer_When_le_post_de_suppression_de_slot_n_atteint_pas_l_api()
    {
        // Given — la grille câblée à l'API distante (mais dont le POST de suppression est injoignable au
        // transport) ; un slot « École » 08h30-16h30 pour Léa le mardi 16/06/2026.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerSlot(api, GrilleRuntimeHarness.EnfantParDefaut, "École", new DateTime(2026, 6, 16, 8, 30, 0), new DateTime(2026, 6, 16, 16, 30, 0));
        var client = GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "/slots/");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026, client);

        // … baseline : la case du 16/06 rend le slot « École ».
        Assert.True(CaseContientSlot(GrilleRuntimeHarness.CaseDuJour(grille, "16/06"), "École"),
            "la case du 16/06 doit rendre le slot École au départ.");

        // When — j'ouvre la dialog (la LECTURE des slots transite normalement) et liste le slot École.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-supprimer-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-supprimer-slot']"));
            },
            TimeSpan.FromSeconds(10));
        grille.WaitForAssertion(
            () => Assert.Contains(grille.FindAll("[data-testid='slot-supprimable']"),
                l => l.TextContent.Contains("École")),
            TimeSpan.FromSeconds(10));

        // … je supprime le slot École : le POST échoue (API injoignable au transport).
        this.SurDispatcher(() => grille.FindAll("[data-testid='slot-supprimable']")
            .Single(l => l.TextContent.Contains("École"))
            .QuerySelector("[data-testid='bouton-supprimer-slot']")!.Click());

        // Then — un message d'échec clair s'affiche DANS la dialog, qui reste OUVERTE ; aucun accusé de
        // succès ; la case du 16/06 rend toujours le slot École.
        grille.WaitForAssertion(
            () =>
            {
                var dialog = grille.Find("[data-testid='dialog-supprimer-slot']");
                Assert.Equal(MessagesEcriture.ServiceInjoignable,
                    dialog.QuerySelector("[data-testid='motif-echec']")!.TextContent.Trim());
                Assert.Empty(grille.FindAll("[data-testid='accuse-slot-supprime']"));
                Assert.True(CaseContientSlot(GrilleRuntimeHarness.CaseDuJour(grille, "16/06"), "École"),
                    "la case du 16/06 doit encore rendre le slot École après l'échec.");
            },
            TimeSpan.FromSeconds(10));

        // … la dialog est toujours présente, le slot École toujours dans le store distant (rien appliqué,
        // aucune mise en file ni rejeu : le code ne réémet jamais).
        Assert.NotEmpty(grille.FindAll("[data-testid='dialog-supprimer-slot']"));

        using var scope = api.Services.CreateScope();
        var slotsDuJour = scope.ServiceProvider.GetRequiredService<SlotsDuJourQuery>();
        Assert.Contains(slotsDuJour.Lister(new DateOnly(2026, 6, 16)), s => s.LieuId == "École");
    }
}
