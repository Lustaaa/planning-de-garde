using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 30 — S10 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : la dialog « Poser un slot » (ponctuel ET
/// récurrent) porte un <b>sélecteur d'enfant EXPLICITE</b> alimenté par l'énumération des enfants du foyer
/// (store vivant, GET /api/foyer/enfants), en remplacement du <b>fantôme</b> <c>Session.EnfantId</c> transmis
/// à l'aveugle (s29). Sur la grille réellement câblée (front WASM + API distante réelle, store/projection/canal
/// HTTP réels), l'enfant CHOISI est transmis au canal d'écriture existant : le slot enregistré porte
/// l'identifiant stable de l'enfant sélectionné, jamais l'EnfantId de session.
///
/// Rempart anti « vert qui ment » : tant que la dialog n'a pas de sélecteur d'enfant (et transmet
/// <c>Session.EnfantId</c>), le slot posé pour « Tom » porterait « Léa » (session) → rouge. On observe le
/// store réel de l'API distante (rempart : le fantôme retiré est prouvé par l'id transmis, pas par le rendu).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelecteurEnfantDialogPoseTempsReelTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    private static System.Collections.Generic.IReadOnlyList<string> EnfantsProposes(IRenderedComponent<PlanningPartage> grille)
        => grille.FindAll("[data-testid='champ-enfant'] option")
            .Select(o => o.TextContent.Trim())
            .Where(t => t.Length > 0 && t != "— choisir —")
            .ToList();

    private void OuvrirDialogPose(IRenderedComponent<PlanningPartage> grille, string jjMM)
        => grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, jjMM).Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-poser-slot']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));
            },
            TimeSpan.FromSeconds(10));

    [Fact]
    public void Le_selecteur_d_enfant_de_la_dialog_de_pose_transmet_l_enfant_choisi_au_canal_ponctuel_et_recurrent()
    {
        // Given — l'API distante réelle avec les enfants « Léa » (seed) et « Tom » (id stable opaque connu),
        // et le lieu « piscine » (école est déjà seedé InMemory) — établis AVANT le rendu de la grille.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("enfant-tom", "Tom");
        api.Services.GetRequiredService<IEditeurActivites>().Ajouter("piscine", "piscine");
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // === Phase 1 — slot PONCTUEL ===
        OuvrirDialogPose(grille, "04/07");

        // … un sélecteur d'enfant est présent et propose « Léa » ET « Tom » (l'enfant n'est plus implicite).
        Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot'] [data-testid='champ-enfant']"));
        Assert.Contains("Léa", EnfantsProposes(grille));
        Assert.Contains("Tom", EnfantsProposes(grille));

        // When — le parent choisit « Tom », le lieu « école » et pose le slot.
        this.SurDispatcher(() => grille.Find("[data-testid='champ-enfant']").Change("enfant-tom"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("école"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — le slot enregistré au store distant porte l'identifiant stable de « Tom », jamais l'EnfantId
        // de session (« Léa »).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']")),
            TimeSpan.FromSeconds(10));
        var slotPonctuel = Assert.Single(api.Services.GetRequiredService<ISlotRepository>().AllSnapshots());
        Assert.Equal("enfant-tom", slotPonctuel.EnfantId);
        Assert.Equal("école", slotPonctuel.LieuId);

        // === Phase 2 — slot RÉCURRENT ===
        OuvrirDialogPose(grille, "11/07");
        this.SurDispatcher(() => grille.Find("[data-testid='champ-repeter-hebdo']").Change(true));
        grille.WaitForElement("[data-testid='champ-heure-debut']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-enfant']").Change("enfant-tom"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-lieu']").Change("piscine"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-debut']").Change("11:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-heure-fin']").Change("12:15"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-poser-slot'] form").Submit());

        // Then — le slot RÉCURRENT enregistré porte aussi l'identifiant stable de « Tom » (le sélecteur unique
        // de la dialog transmet l'enfant choisi sur les DEUX chemins d'écriture).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']")),
            TimeSpan.FromSeconds(10));
        var slotRecurrent = Assert.Single(api.Services.GetRequiredService<ISlotRecurrentRepository>().AllSnapshots());
        Assert.Equal("enfant-tom", slotRecurrent.EnfantId);
        Assert.Equal("piscine", slotRecurrent.LieuId);
    }
}
