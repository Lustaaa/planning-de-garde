using System;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ IHM, <c>@nominal</c>, driver) — l'impersonation bornée
/// sur la grille <b>réellement câblée</b> (front WASM <see cref="PlanningPartage"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel, <b>référentiel réel</b> dont le <b>type d'acteur est
/// surfacé read-only depuis le seed</b> — D3). Depuis le <b>sélecteur d'incarnation</b>, le configurateur
/// incarne un acteur déjà déclaré du foyer :
/// <list type="bullet">
///   <item>un <b>bandeau « Vous incarnez X »</b> s'affiche ;</item>
///   <item>le <b>menu d'actions au clic sur une case</b> est <b>visible</b> si l'incarné est Parent/Admin,
///   <b>masqué</b> s'il est Autre (règle 8, <c>EstParent</c> dérivé de l'identité EFFECTIVE).</item>
/// </list>
///
/// <para>Fixture du foyer (types issus du seed read-only) : <b>Bruno</b> (parent-b, Parent → menu visible),
/// <b>Nina la nounou</b> (nounou, Autre → menu masqué), <b>Marie-Hélène Grand-Dubois</b> (parent-c, Admin →
/// menu visible). NB : l'exemple Admin du scénario (« Carla ») est porté par l'acteur Admin déjà seedé
/// parent-c — « Carla » est réservé aux tests d'AJOUT d'acteur (nom frais), on ne le seede donc pas ; le
/// comportement Admin (menu visible) prouvé est identique.</para>
///
/// <para>Anti « vert qui ment » : prouvé sur l'app réellement câblée (render mode interactif, DI réelle,
/// référentiel réel résolu via le canal de lecture HTTP, type surfacé depuis le seed). Un bUnit forçant
/// l'interactivité ou doublant le type d'acteur ne prouverait pas que le menu clic-case suit réellement le
/// type de l'incarné. Contrôle de non-vacuité intégré : le cas Parent (menu visible) garantit que le menu
/// n'est pas cassé pour tous, en regard du cas Autre (menu masqué).</para>
/// </summary>
public sealed class FrontWasmIncarnerRefleteRoleTempsReelTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = GrilleRuntimeHarness.Lundi_29_06_2026;

    [Theory]
    [InlineData("parent-b", "Bruno", true)]                                   // Parent → menu visible
    [InlineData("nounou", "Nina la nounou", false)]                          // Autre  → menu masqué
    [InlineData("parent-c", "Marie-Hélène Grand-Dubois", true)]              // Admin  → menu visible
    public void Should_AfficherLeBandeauEtAdapterLeMenuClicCase_When_LeConfigurateurIncarneUnActeurDeclare(
        string acteurId, string nomIncarne, bool menuVisible)
    {
        // Given — la grille réellement câblée à l'API distante (store + référentiel + types seed réels).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // … le sélecteur d'incarnation est peuplé depuis le référentiel réel (GET HTTP asynchrone) ; on
        // attend qu'il propose au moins l'acteur ciblé avant d'incarner.
        grille.WaitForState(
            () => grille.FindAll($"[data-testid='selecteur-incarnation'] option[value='{acteurId}']").Count == 1,
            TimeSpan.FromSeconds(10));

        // … aucun bandeau d'incarnation à l'état initial (identité réelle).
        Assert.Empty(grille.FindAll("[data-testid='bandeau-incarnation']"));

        // When — le configurateur incarne l'acteur ciblé depuis le sélecteur d'incarnation réel.
        grille.Find("[data-testid='selecteur-incarnation']").Change(acteurId);

        // Then — un bandeau « Vous incarnez <nom> » s'affiche …
        grille.WaitForAssertion(
            () =>
            {
                var bandeau = grille.Find("[data-testid='bandeau-incarnation']");
                Assert.Contains($"Vous incarnez {nomIncarne}", bandeau.TextContent);
            },
            TimeSpan.FromSeconds(10));

        // … et le menu d'actions au clic sur une case reflète le rôle de l'incarné (visible Parent/Admin,
        // masqué Autre). Sous WaitForAssertion pour laisser le re-render du changement d'identité s'appliquer ;
        // le clic répété reste idempotent (OuvrirMenu sort tôt si l'identité effective n'écrit pas).
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click();
                if (menuVisible)
                    Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
                else
                    Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
