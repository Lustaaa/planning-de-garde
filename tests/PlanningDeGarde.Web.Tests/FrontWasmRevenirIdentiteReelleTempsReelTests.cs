using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ IHM, <c>@nominal</c>) — le <b>retour à l'identité réelle</b>
/// sur la grille <b>réellement câblée</b> (front WASM <see cref="PlanningPartage"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store + référentiel + type seed réels). Le configurateur incarne
/// Bruno (Parent) — bandeau « Vous incarnez Bruno » affiché, menu clic-case visible — puis <b>revient à
/// son identité réelle</b> via l'affordance du bandeau :
/// <list type="bullet">
///   <item>le bandeau « Vous incarnez Bruno » n'est <b>plus affiché</b> ;</item>
///   <item>le menu d'actions au clic sur une case est de nouveau celui de l'identité réelle (<b>visible</b>).</item>
/// </list>
///
/// <para>Geste inverse de Sc.1 (<see cref="FrontWasmIncarnerRefleteRoleTempsReelTests"/>). L'affordance de
/// retour (bouton du bandeau) et <c>SessionPlanning.RevenirIdentiteReelle</c> ont été posés avec le socle
/// Sc.1 ; ce test caractérise le retour <b>observable sur l'app réellement câblée</b> — un bUnit forçant
/// l'interactivité ou doublant la session ne prouverait pas que le clic retire réellement le bandeau et
/// restaure le menu. Contrôle de non-vacuité intégré : l'état incarné (bandeau présent + menu visible) est
/// prouvé AVANT le retour, sinon l'absence de bandeau après serait un faux vert.</para>
/// </summary>
public sealed class FrontWasmRevenirIdentiteReelleTempsReelTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = GrilleRuntimeHarness.Lundi_29_06_2026;

    [Fact]
    public void Should_RetirerLeBandeauEtRestaurerLeMenuReel_When_LeConfigurateurRevientASonIdentiteReelle()
    {
        // Given — la grille réellement câblée ; le configurateur incarne Bruno (Parent).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        grille.WaitForState(
            () => grille.FindAll("[data-testid='selecteur-incarnation'] option[value='parent-b']").Count == 1,
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='selecteur-incarnation']").Change("parent-b"));

        // CONTRÔLE POSITIF — sous l'incarnation : bandeau « Vous incarnez Bruno » affiché ET menu visible
        // au clic (l'incarnation est bien active avant qu'on en sorte).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Contains("Vous incarnez Bruno",
                    grille.Find("[data-testid='bandeau-incarnation']").TextContent);
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));
        // Referme le menu avant de revenir (clic hors panneau) — idempotent sous WaitForAssertion.
        grille.WaitForAssertion(
            () =>
            {
                if (grille.FindAll("[data-testid='menu-actions-case']").Count > 0)
                    this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case']").Click());
                Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));

        // When — le configurateur revient à son identité réelle via l'affordance du bandeau.
        this.SurDispatcher(() => grille.Find("[data-testid='revenir-identite-reelle']").Click());

        // Then — le bandeau « Vous incarnez Bruno » n'est plus affiché …
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='bandeau-incarnation']")),
            TimeSpan.FromSeconds(10));

        // … et le menu clic-case est de nouveau celui de l'identité réelle (visible).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
