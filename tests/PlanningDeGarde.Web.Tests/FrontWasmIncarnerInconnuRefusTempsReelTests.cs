using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ IHM, <c>@erreur</c>) — le <b>refus silencieux</b> d'incarner
/// un identifiant <b>absent du référentiel réel</b>, sur la grille <b>réellement câblée</b> (front WASM
/// <see cref="PlanningPartage"/>, API distante réelle <see cref="ApiDistanteFactory"/>, référentiel +
/// type seed réels chargés via le canal de lecture HTTP). Le configurateur tente d'incarner
/// <c>acteur-inexistant</c> — un identifiant qu'aucun acteur du foyer ne porte — en faisant parvenir cette
/// valeur au binding réel du sélecteur d'incarnation :
/// <list type="bullet">
///   <item>aucun bandeau « Vous incarnez » n'est affiché ;</item>
///   <item>il reste sous son identité réelle (menu clic-case inchangé, <b>visible</b>).</item>
/// </list>
///
/// <para>Le refus s'appuie sur la résolution contre le <b>catalogue réel</b> (énumération de lecture,
/// identifiant stable — règle 19), pas sur un acteur fantôme doublé : la garde conditionnelle de
/// <c>SessionPlanning.Incarner</c> (no-op si l'id est absent) a été posée avec le socle Sc.1, l'inner-loop
/// driver est couvert par
/// <c>SessionPlanningIncarnationTests.Should_ConserverLIdentiteReelle_When_OnIncarneUnIdentifiantInconnu</c>.
/// Contrôle de non-vacuité : le sélecteur est prouvé peuplé d'acteurs réels AVANT la tentative — l'absence
/// de bandeau n'est donc pas due à un référentiel vide ou à une incarnation globalement cassée (Sc.1 prouve
/// par ailleurs qu'un id réel incarne bien).</para>
/// </summary>
public sealed class FrontWasmIncarnerInconnuRefusTempsReelTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = GrilleRuntimeHarness.Lundi_29_06_2026;

    [Fact]
    public void Should_ResterSousLIdentiteReelleSansBandeau_When_OnTenteDIncarnerUnIdentifiantAbsentDuReferentiel()
    {
        // Given — la grille réellement câblée ; le sélecteur d'incarnation est peuplé depuis le référentiel
        // réel (contrôle de non-vacuité : des acteurs réels existent, l'incarnation serait possible) …
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        grille.WaitForState(
            () => grille.FindAll("[data-testid='selecteur-incarnation'] option[value='parent-b']").Count == 1,
            TimeSpan.FromSeconds(10));

        // … et aucun acteur ne porte l'identifiant ciblé (le référentiel réel ne l'énumère pas).
        Assert.Empty(grille.FindAll("[data-testid='selecteur-incarnation'] option[value='acteur-inexistant']"));
        Assert.Empty(grille.FindAll("[data-testid='bandeau-incarnation']"));

        // When — le configurateur tente d'incarner un identifiant absent du référentiel (valeur portée au
        // binding réel du sélecteur, comme si elle lui parvenait).
        this.SurDispatcher(() => grille.Find("[data-testid='selecteur-incarnation']").Change("acteur-inexistant"));

        // Then — refus silencieux : aucun bandeau « Vous incarnez » n'est affiché …
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='bandeau-incarnation']")),
            TimeSpan.FromSeconds(10));

        // … et il reste sous son identité réelle : le menu clic-case est inchangé (visible).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
