using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 49 — Sc.4 (🖥️ IHM, <c>@limite</c>) — FILET de caractérisation : un <b>clic simple</b> (mousedown +
/// mouseup sur la MÊME case, sans déplacement) conserve STRICTEMENT le comportement existant — il ouvre le
/// <b>menu clic-case</b> (Affecter une période / Définir un transfert / …), et N'OUVRE PAS la dialog
/// « Affecter une période » pré-remplie sur une PLAGE. Distinction clic vs drag par les CASES (curseur resté
/// sur l'ancre → clic simple), posée avec Sc.3 (contrat point 1) : ce test verrouille la non-régression du
/// clic simple face à l'affordance de drag. Rendu sur la grille <b>réellement câblée</b> à l'API distante.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelectionPlageClicSimpleInchangeTempsReelTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06/2026

    [Fact]
    public void Should_Ouvrir_le_menu_clic_case_et_pas_la_dialog_de_plage_When_un_Parent_presse_et_relache_sur_la_meme_case_sans_deplacement_sur_l_app_reellement_cablee()
    {
        // Given — la grille réelle câblée à l'API distante, affichée pour un Parent, aujourd'hui = 29/06/2026.
        using var api = new ApiDistanteFactory();
        var relachement = GrilleRuntimeHarness.DoublerRelachementPointeur(this);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — pointerdown PUIS relâchement (pointerup document) sur la MÊME case (29/06), sans aucun survol
        // d'une autre case (pas de drag) : le curseur reste sur l'ancre → clic simple.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").PointerDown());
        this.SurDispatcher(() => relachement.RelacherPointeurDocument().GetAwaiter().GetResult());

        // Then — le menu clic-case EXISTANT s'ouvre (avec ses entrées), et la dialog de PLAGE n'est PAS ouverte.
        grille.WaitForAssertion(
            () =>
            {
                var menu = grille.Find("[data-testid='menu-actions-case']");
                Assert.NotNull(menu.QuerySelector("[data-testid='action-affecter-periode']"));
                Assert.NotNull(menu.QuerySelector("[data-testid='action-definir-transfert']"));
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
                // … et aucune case n'est en surbrillance de plage (le geste s'est soldé sans sélection d'intervalle).
                Assert.Empty(grille.FindAll("[data-plage-drag='1']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
