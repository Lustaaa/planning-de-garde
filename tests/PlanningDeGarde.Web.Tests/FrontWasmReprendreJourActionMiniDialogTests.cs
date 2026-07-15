using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 46 — Sc.4 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : l'action « reprendre ce jour » est une
/// ENTRÉE CONDITIONNELLE du MENU CLIC-CASE existant (à côté de « déléguer ce jour » s44), PRÉSENTE quand la
/// case cliquée porte une délégation active (surcharge résolue). La confirmer émet AnnulerDelegation via le
/// CANAL d'écriture (POST /api/canal/annuler-delegation) — jamais la diffusion — qui COMPOSE la suppression de
/// surcharge existante (s16) : la case relue retombe sur le FOND (cycle), le transfert dérivé s31 disparaît.
/// La grille est réellement câblée à l'API distante (store réel, projection réelle, canal réel).
///
/// Anti « vert qui ment » : la reprise est prouvée jusqu'au store distant réel (relecture par la projection
/// réelle), jamais une doublure de transport.
/// </summary>
public sealed class FrontWasmReprendreJourActionMiniDialogTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026
    // Mercredi 08/07/2026 : dans la fenêtre par défaut (29/06 → 26/07), semaine ISO 28 PAIRE → fond parent-a (Alice).
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);

    private static CycleDeFond CycleAliceBruno()
        => new(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" });

    [Fact]
    public void Un_parent_reprend_ce_jour_depuis_le_menu_clic_case_via_le_canal_decriture()
    {
        // Given — grille câblée réelle. Un cycle de fond (Alice paire ISO), et une DÉLÉGATION ACTIVE sur le
        // mercredi 08/07 : une surcharge confie ce jour à Bruno (parent-b), qui prime le fond Alice.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, CycleAliceBruno());
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b",
            Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue));
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // Précondition — la case du 08/07 résout Bruno (surcharge > fond).
        grille.WaitForAssertion(
            () => Assert.Equal(
                "Bruno",
                GrilleRuntimeHarness.CaseDuJour(grille, "08/07").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim()),
            TimeSpan.FromSeconds(10));

        // When — clic sur la case → le menu porte l'entrée « reprendre ce jour » (case déléguée) → clic → mini-dialog de confirmation.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "08/07").Click());
        var menu = grille.Find("[data-testid='menu-actions-case']");
        Assert.NotNull(menu.QuerySelector("[data-testid='action-reprendre']"));
        this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-reprendre']").Click());
        Assert.NotEmpty(grille.FindAll("[data-testid='dialog-reprendre']"));

        // When — je confirme la reprise.
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-reprendre-confirmer']").Click());

        // Then — la dialog se ferme ET la case du 08/07 retombe sur le FOND (Alice), relue du store.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-reprendre']"));
                Assert.Equal(
                    "Alice",
                    GrilleRuntimeHarness.CaseDuJour(grille, "08/07").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
            },
            TimeSpan.FromSeconds(10));

        // … et la reprise a réellement transité jusqu'au store de l'API distante (rempart anti vert-qui-ment) :
        // la surcharge du 08/07 a disparu, la projection réelle résout le fond (Alice), aucun transfert dérivé.
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var caseStore = projection.Projeter(Mercredi_08_07_2026).Jours.Single(j => j.Date == Mercredi_08_07_2026);
        Assert.Equal("Alice", caseStore.NomResponsable);
        Assert.Null(caseStore.Transfert);
    }
}
