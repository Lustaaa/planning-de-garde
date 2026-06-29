using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.5 (🖥️ scénario IHM, <c>@nominal</c>) — affecter une période sur une
/// <b>plage de 2 cases contiguës</b>. Le comportement neuf vit dans le <c>.razor</c> (sélection de plage
/// front + dialog pré-remplie sur l'intervalle) ; le <b>write réutilise <c>AffecterPeriode</c> existant</b>
/// (backend early green : une période <c>[début, fin]</c> couvre déjà un intervalle, inclusif aux deux
/// bornes). On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM)
/// câblée à une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, projection réelle
/// <see cref="GrilleAgendaQuery"/>, palette + référentiel <b>réels</b> du foyer).
///
/// <para><b>Ancrage.</b> Aujourd'hui = mercredi 10/06/2026 → fenêtre démarrant au lundi 08/06 (vue 4
/// semaines). Un cycle de fond de 2 semaines est semé (index 0 → parent-a « Alice » bleu, index 1 →
/// parent-b « Bruno » orange) : la semaine du 08/06 (ISO 24 paire, index 0) résout le <b>fond Alice/bleu</b>
/// sur 09/06, 10/06 ET 11/06. Le Parent sélectionne la plage mardi 09/06 → mercredi 10/06 et affecte
/// « Bruno » : <b>une seule</b> période <c>[09/06, 10/06]</c> est enregistrée, les 2 cases réapparaissent
/// « Bruno » / orange (la <b>surcharge prime le fond</b>), et la case hors plage du 11/06 reste Alice/bleu.</para>
///
/// <para><b>Anti « vert qui ment ».</b> La réapparition est observée par <b>relecture</b> de la projection
/// réelle (jamais une mutation locale), et l'unicité de la période est vérifiée directement sur le
/// <see cref="IPeriodeRepository"/> du store distant. Tant que la sélection de plage n'existe pas côté front,
/// aucune période n'est créée sur l'intervalle → rouge. Un bUnit à doublure ne verrait ni le câblage distant
/// ni la résolution réelle du foyer.</para>
/// </summary>
public sealed class FrontWasmAffecterPeriodePlageContigueTempsReelTests : TestContext
{
    private static readonly DateTime Mercredi_10_06_2026 = new(2026, 6, 10);

    [Fact]
    public void Should_Enregistrer_une_seule_periode_du_09_au_10_06_2026_responsable_Bruno_et_faire_reapparaitre_les_deux_cases_nommees_Bruno_en_orange_sans_modifier_les_autres_When_un_Parent_selectionne_la_plage_des_deux_cases_contigues_et_affecte_Bruno_sur_l_app_reellement_cablee()
    {
        // Given — l'API distante réelle (store vierge : aucune période). On sème un cycle de fond de
        // 2 semaines : index 0 → parent-a (Alice, bleu), index 1 → parent-b (Bruno, orange). La semaine du
        // 08/06 (index 0) résout donc le fond Alice/bleu sur 09/06, 10/06 et 11/06, sans aucune période.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api,
            new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

        // … la grille réelle câblée à l'API distante, affichée pour un Parent, aujourd'hui = mercredi
        // 10/06/2026 (fenêtre 4 semaines démarrant au lundi 08/06).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_10_06_2026);

        // … les cases 09/06 et 10/06 affichent le fond Alice/bleu, et 11/06 aussi (témoin hors plage).
        AssertCase(grille, "09/06", "Alice", "bleu");
        AssertCase(grille, "10/06", "Alice", "bleu");
        AssertCase(grille, "11/06", "Alice", "bleu");

        // When — un Parent active la sélection de plage, clique la case de début (mardi 09/06) puis la case
        // de fin (mercredi 10/06) : la dialog d'affectation s'ouvre pré-remplie sur l'intervalle [09/06, 10/06].
        grille.Find("[data-testid='mode-plage']").Click();
        grille.WaitForState(
            () => grille.Find("[data-testid='barre-navigation']").GetAttribute("data-mode-plage") == "1",
            TimeSpan.FromSeconds(10));

        GrilleRuntimeHarness.CaseDuJour(grille, "09/06").Click();
        GrilleRuntimeHarness.CaseDuJour(grille, "10/06").Click();
        grille.WaitForState(
            () => grille.FindAll("[data-testid='dialog-affecter-periode']").Count == 1,
            TimeSpan.FromSeconds(10));

        // … il choisit « Bruno » (id stable parent-b) et valide : UNE commande AffecterPeriode est émise
        // sur l'intervalle pré-rempli.
        grille.Find("[data-testid='champ-responsable']").Change("parent-b");
        grille.Find("[data-testid='dialog-affecter-periode'] form").Submit();

        // Then — la dialog se ferme, les 2 cases 09/06 et 10/06 réapparaissent « Bruno »/orange (surcharge
        // primant le fond Alice), et la case hors plage 11/06 reste Alice/bleu — le tout relu depuis le store
        // réel de l'API distante.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
                AssertCase(grille, "09/06", "Bruno", "orange");
                AssertCase(grille, "10/06", "Bruno", "orange");
                AssertCase(grille, "11/06", "Alice", "bleu");
            },
            TimeSpan.FromSeconds(10));

        // … et UNE SEULE période [09/06, 10/06] / parent-b a réellement transité jusqu'au store distant
        // (rempart anti vert-qui-ment) : un seul snapshot, couvrant l'intervalle, jamais deux écritures jour.
        var snapshots = api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots();
        var snapshot = Assert.Single(snapshots);
        Assert.Equal("parent-b", snapshot.ResponsableId);
        Assert.Equal(new DateTime(2026, 6, 9), snapshot.Debut);
        Assert.Equal(new DateTime(2026, 6, 10), snapshot.Fin);
    }

    private static void AssertCase(
        IRenderedComponent<Web.Components.Pages.PlanningPartage> grille, string jjMM, string nom, string couleur)
    {
        var caseJour = GrilleRuntimeHarness.CaseDuJour(grille, jjMM);
        Assert.Equal(nom, caseJour.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal(couleur, caseJour.GetAttribute("data-couleur"));
    }
}
