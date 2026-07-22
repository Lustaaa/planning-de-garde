using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.1 (@back, borne de convergence — acceptation de NIVEAU RUNTIME) : le
/// <b>sélecteur d'édition</b> de l'écran de configuration (choix de l'acteur à renommer / recolorier)
/// énumère ses acteurs <b>exclusivement</b> depuis le <b>store vivant unifié</b>
/// (<see cref="IEnumerationActeursFoyer"/>, id stables), et <b>non plus</b> depuis la liste statique
/// front <c>Foyer.ActeursEditables</c>. Après ce sprint il n'existe qu'<b>un seul chemin de lecture</b>
/// du référentiel acteurs : la source du sélecteur d'édition est <b>strictement identique</b> à celle
/// que lisent les sélecteurs des dialogs et la grille (<c>GET /api/foyer/acteurs</c>).
///
/// Rempart anti « vert qui ment » : on déclare au runtime un acteur RÉEL « Carla » (id <c>carla</c>)
/// <b>absent</b> de la liste statique <c>Foyer.ActeursEditables</c>. Tant que le sélecteur d'édition
/// lit encore la liste statique, « carla » manque de ses options → rouge. Un bUnit à doublure de
/// transport ne prouverait ni la DI réelle, ni l'énumération depuis le store durable via le canal HTTP.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigSelecteurEditionStoreUnifieTempsReelTests : TestContext
{
    [Fact]
    public void Le_selecteur_d_edition_de_l_ecran_config_enumere_exactement_le_store_vivant_unifie_dont_un_acteur_reel_ajoute_au_runtime_et_plus_la_liste_statique()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé),
        // dans lequel un acteur RÉEL « Carla » est déclaré AVANT l'ouverture de l'écran (absent de la
        // liste statique front Foyer.ActeursEditables).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().Ajouter("carla", "Carla", "rose");
        var sourceUnifiee = api.Services.GetRequiredService<IEnumerationActeursFoyer>()
            .EnumererActeurs().OrderBy(id => id).ToList(); // même source que dialogs + grille (canal de lecture)

        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning()); // Parent par défaut : le formulaire d'édition est visible (gating Sc.7)

        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran énumère les acteurs DEPUIS LE STORE (GET HTTP réel) : on attend la fin du chargement.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Then — refonte s32 : la table (lecture) de l'écran énumère EXACTEMENT les acteurs du store vivant
        // unifié (dont « carla »), chaque ligne portant l'id stable — même source que les dialogs et la grille
        // (le crayon de chaque ligne ouvre la modal d'édition sur cet id, jamais une liste statique front).
        config.WaitForAssertion(
            () =>
            {
                var idsTable = config.FindAll("[data-testid='acteur-foyer']")
                    .Select(li => li.GetAttribute("data-acteur-id") ?? "")
                    .Where(v => v.Length > 0)
                    .OrderBy(id => id)
                    .ToList();
                Assert.Equal(sourceUnifiee, idsTable);
                Assert.Contains("carla", idsTable);
            },
            TimeSpan.FromSeconds(10));
    }
}
