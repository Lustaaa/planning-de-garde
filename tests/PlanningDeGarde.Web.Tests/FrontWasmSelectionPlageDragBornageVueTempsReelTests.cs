using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 49 — Sc.6 (🖥️ IHM, <c>@limite</c>) — le drag est <b>BORNÉ À LA VUE chargée</b> : tirer au-delà du
/// bord de la fenêtre ne sélectionne aucune case hors-vue (le curseur est naturellement clampé — seules les
/// cases rendues émettent le survol), ne déclenche <b>aucune navigation</b> passé/futur (l'ancre ne bouge
/// pas), et ne charge rien de plus (la même fenêtre de 28 cases reste projetée). La sélection est un état
/// d'interaction <b>VOLATILE</b> (borne anti-cliquet) : un <b>changement de vue l'efface</b> — elle ne
/// survit pas à la re-projection. Rendu sur la grille <b>réellement câblée</b> à l'API distante.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelectionPlageDragBornageVueTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_borner_la_selection_a_la_vue_sans_naviguer_et_l_effacer_au_changement_de_vue_When_un_Parent_drague_jusqu_au_bord_de_la_fenetre_sur_l_app_reellement_cablee()
    {
        // Given — la grille réelle câblée à l'API distante (store vierge), Parent, aujourd'hui = 10/06/2026
        // (fenêtre 4 semaines glissantes : lundi 08/06 → dimanche 05/07, soit 28 cases).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);
        var session = Services.GetRequiredService<SessionPlanning>();
        var ancreAvant = session.Ancre;

        // When — mousedown sur la 1ʳᵉ case interne (08/06, début de fenêtre), puis survol jusqu'à la DERNIÈRE
        // case rendue (05/07, bord de la vue) : impossible de tirer « au-delà » — aucune case hors-vue n'existe
        // dans le DOM, le curseur est donc clampé à la fenêtre chargée.
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "08/06").PointerDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "05/07").PointerOver());

        // Then — la surbrillance couvre l'intervalle jusqu'au bord, MAIS le geste n'a NI navigué (ancre
        // inchangée) NI chargé une autre fenêtre (toujours 28 cases projetées : aucune case hors-vue).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "08/06").GetAttribute("data-plage-drag"));
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "05/07").GetAttribute("data-plage-drag"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Equal(ancreAvant, session.Ancre); // le drag débordant n'a déclenché aucune navigation
        Assert.Equal(28, grille.FindAll("[data-testid='jour-case']").Count); // même fenêtre, rien chargé hors-vue

        // When — changement de vue PENDANT que la sélection volatile est en cours (non relâchée).
        this.SurDispatcher(() => grille.Find("[data-testid='selecteur-vue']").Change("mois"));

        // Then — la sélection volatile est EFFACÉE (borne anti-cliquet) : plus AUCUNE case en surbrillance de
        // plage après la re-projection — l'état d'interaction ne survit pas au changement de vue.
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-plage-drag='1']")),
            TimeSpan.FromSeconds(10));
    }
}
