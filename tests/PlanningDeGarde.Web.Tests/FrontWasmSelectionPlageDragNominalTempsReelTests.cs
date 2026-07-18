using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 49 — Sc.3 (🖥️ IHM, <c>@nominal</c>) — acceptation de NIVEAU RUNTIME de la <b>sélection de plage
/// par DRAG</b> directement sur les cases de la grille agenda (affordance tranchée par le scrum-master).
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à
/// une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, projection réelle
/// <see cref="GrilleAgendaQuery"/>, palette + référentiel <b>réels</b> du foyer : <c>parent-a</c> → « Alice » / bleu).
///
/// <para><b>Geste.</b> mousedown sur J1 (mardi 09/06) → survol jusqu'à J3 (jeudi 11/06) → les cases J1, J2, J3
/// sont mises en SURBRILLANCE pendant le geste → au mouseup la dialog « Affecter une période » EXISTANTE (s06)
/// s'ouvre, pré-remplie sur l'intervalle. Choisir « Alice » et valider écrit UNE période <c>[09/06, 11/06]</c>
/// via le canal d'écriture (réemploi strict s06), et la grille converge : J1, J2, J3 rendent « Alice ».</para>
///
/// <para><b>Anti « vert qui ment ».</b> La convergence est observée par relecture de la projection réelle
/// (jamais une mutation locale), et l'unicité de la période sur l'intervalle est vérifiée directement sur le
/// <see cref="IPeriodeRepository"/> du store distant. Tant que le drag n'ouvre pas la dialog pré-remplie sur
/// l'intervalle, aucune période sur [09/06, 11/06] n'est créée → rouge.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelectionPlageDragNominalTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Ouvrir_la_dialog_pre_remplie_sur_l_intervalle_et_ecrire_une_seule_periode_09_au_11_06_faisant_converger_les_trois_cases_sur_Alice_When_un_Parent_drague_de_J1_a_J3_sur_l_app_reellement_cablee()
    {
        // Given — la grille réelle câblée à l'API distante (store vierge), affichée pour un Parent, aujourd'hui
        // = mercredi 10/06/2026 (fenêtre 4 semaines démarrant au lundi 08/06). Les cases J1..J3 sont neutres.
        using var api = new ApiDistanteFactory();
        var relachement = GrilleRuntimeHarness.DoublerRelachementPointeur(this);
        var mouvement = GrilleRuntimeHarness.DoublerMouvementPointeur(this);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);
        Assert.Null(GrilleRuntimeHarness.CaseDuJour(grille, "09/06").QuerySelector("[data-testid='nom-responsable']"));
        Assert.Null(GrilleRuntimeHarness.CaseDuJour(grille, "11/06").QuerySelector("[data-testid='nom-responsable']"));
        // Rempart de non-régression du correctif gate G3 : les cases portent la classe qui neutralise la
        // SÉLECTION DE TEXTE native (user-select:none) — sans elle, le glisser souris est avalé en navigateur réel.
        Assert.Contains("grille-plage-selectionnable", GrilleRuntimeHarness.CaseDuJour(grille, "09/06").ClassList);

        // When — pointerdown sur J1 (09/06), puis MOUVEMENT du pointeur jusqu'à J3 (11/06) : les 3 cases sont en
        // surbrillance. La voie d'événement est celle du 2ᵉ correctif du gate G3 : le survol est résolu au niveau
        // DOCUMENT (pointermove → elementFromPoint → data-date de la case), JAMAIS un @onpointerover de case (que
        // le navigateur réel manque pendant un glisser / court-circuite sous capture de pointeur).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "09/06").PointerDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.SurvolerCaseParPointeurDocument(mouvement, grille, "11/06"));

        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "09/06").GetAttribute("data-plage-drag"));
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "10/06").GetAttribute("data-plage-drag"));
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "11/06").GetAttribute("data-plage-drag"));
            },
            TimeSpan.FromSeconds(10));

        // … au relâchement (pointerup capté au niveau DOCUMENT, jamais sur la case seule), la dialog « Affecter
        // une période » EXISTANTE s'ouvre (pré-remplie sur l'intervalle) et la surbrillance disparaît.
        this.SurDispatcher(() => relachement.RelacherPointeurDocument().GetAwaiter().GetResult());
        grille.WaitForState(
            () => grille.FindAll("[data-testid='dialog-affecter-periode']").Count == 1,
            TimeSpan.FromSeconds(10));

        // … le Parent choisit « Alice » (id stable parent-a) et valide : UNE commande AffecterPeriode sur l'intervalle.
        this.SurDispatcher(() => grille.Find("[data-testid='champ-responsable']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-affecter-periode'] form").Submit());

        // Then — la dialog se ferme et la grille CONVERGE : J1, J2, J3 rendent « Alice »/bleu (relu depuis le store réel).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
                foreach (var jjMM in new[] { "09/06", "10/06", "11/06" })
                {
                    var caseJour = GrilleRuntimeHarness.CaseDuJour(grille, jjMM);
                    Assert.Equal("Alice", caseJour.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Equal("bleu", caseJour.GetAttribute("data-couleur"));
                }
            },
            TimeSpan.FromSeconds(10));

        // … et UNE SEULE période [09/06, 11/06] / parent-a a réellement transité jusqu'au store distant
        // (rempart anti vert-qui-ment) : un seul snapshot couvrant l'intervalle, jamais trois écritures jour.
        var snapshot = Assert.Single(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        Assert.Equal("parent-a", snapshot.ResponsableId);
        Assert.Equal(new DateTime(2026, 6, 9), snapshot.Debut);
        Assert.Equal(new DateTime(2026, 6, 11), snapshot.Fin);
    }
}
