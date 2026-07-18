using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 49 — Sc.5 (🖥️ IHM, <c>@limite</c>) — drag en SENS INVERSE (J3 → J1) : l'intervalle est
/// <b>NORMALISÉ</b> <c>[min, max]</c> (début ≤ fin garanti), strictement équivalent au sens direct. La
/// surbrillance couvre J1, J2, J3 (même intervalle que le sens direct), et la dialog s'ouvre avec début = J1
/// et fin = J3 — jamais une plage inversée ni vide. Prouvé jusqu'au store distant réel : UNE période
/// <c>[09/06, 11/06]</c> (début &lt; fin), jamais <c>[11/06, 09/06]</c>. Rendu sur la grille <b>réellement
/// câblée</b> à l'API distante (store réel, projection réelle).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmSelectionPlageDragInverseTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Normaliser_l_intervalle_min_max_et_ecrire_une_seule_periode_09_au_11_06_debut_avant_fin_When_un_Parent_drague_de_J3_vers_J1_sens_inverse_sur_l_app_reellement_cablee()
    {
        // Given — la grille réelle câblée à l'API distante (store vierge), Parent, aujourd'hui = 10/06/2026
        // (fenêtre 4 semaines démarrant au lundi 08/06).
        using var api = new ApiDistanteFactory();
        var relachement = GrilleRuntimeHarness.DoublerRelachementPointeur(this);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);

        // When — pointerdown sur J3 (11/06), survol (pointerover) jusqu'à J1 (09/06) : SENS INVERSE. La
        // surbrillance couvre néanmoins J1, J2, J3 (intervalle normalisé, identique au sens direct).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "11/06").PointerDown());
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "09/06").PointerOver());

        grille.WaitForAssertion(
            () =>
            {
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "09/06").GetAttribute("data-plage-drag"));
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "10/06").GetAttribute("data-plage-drag"));
                Assert.Equal("1", GrilleRuntimeHarness.CaseDuJour(grille, "11/06").GetAttribute("data-plage-drag"));
            },
            TimeSpan.FromSeconds(10));

        // … au relâchement (pointerup document), la dialog s'ouvre pré-remplie sur l'intervalle normalisé [09/06, 11/06].
        this.SurDispatcher(() => relachement.RelacherPointeurDocument().GetAwaiter().GetResult());
        grille.WaitForState(
            () => grille.FindAll("[data-testid='dialog-affecter-periode']").Count == 1,
            TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => grille.Find("[data-testid='champ-responsable']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-affecter-periode'] form").Submit());

        // Then — UNE SEULE période [09/06, 11/06] transite jusqu'au store distant, début ≤ fin (jamais inversée) :
        // le sens inverse est strictement équivalent au sens direct (normalisation min/max, contrat point 3).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']")),
            TimeSpan.FromSeconds(10));
        var snapshot = Assert.Single(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        Assert.Equal("parent-a", snapshot.ResponsableId);
        Assert.Equal(new DateTime(2026, 6, 9), snapshot.Debut);
        Assert.Equal(new DateTime(2026, 6, 11), snapshot.Fin);
        Assert.True(snapshot.Debut <= snapshot.Fin, "l'intervalle doit être normalisé (début ≤ fin), jamais inversé.");
    }
}
