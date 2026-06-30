using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 19 — Sc.5 (🖥️ @ihm) — Acceptation de NIVEAU RUNTIME : les sélecteurs de responsable des
/// dialogs d'écriture du planning ne proposent QUE les acteurs DÉCLARÉS du foyer (énumérés depuis le
/// store vivant via <c>GET /api/foyer/acteurs</c>, clé = identifiant stable), jamais une liste statique
/// codée en dur (<c>Foyer.Responsables</c>) qui ignorait les acteurs réels ajoutés/supprimés.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée
/// à une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, canal HTTP réel) :
/// un acteur RÉEL « Carla » est déclaré au runtime, puis on ouvre chaque dialog d'écriture et on
/// vérifie que son sélecteur de responsable expose EXACTEMENT l'ensemble des acteurs déclarés (dont
/// Carla). Rempart anti « vert qui ment » : tant que le dialog lit la liste statique, Carla (et les
/// autres acteurs déclarés au-delà des deux figés) manquent → rouge. Un bUnit à doublure de transport
/// ne verrait ni le câblage distant ni l'énumération réelle du store.
///
/// Les dialogs d'« éditer une période » lisent déjà le store (s17) ; « poser un slot » n'a pas de
/// sélecteur de responsable (lieu/enfant). Ce scénario borne donc les deux sélecteurs encore figés :
/// « affecter une période » et « définir un transfert » (dépose + récupère).
/// </summary>
public sealed class FrontWasmSelecteursActeursDeclaresTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    /// <summary>Déclare un acteur réel « Carla » dans le store vivant de l'API distante (Given), puis
    /// restitue l'ensemble — trié — des identifiants stables des acteurs déclarés (cible attendue des
    /// sélecteurs).</summary>
    private static IReadOnlyList<string> DeclarerCarlaEtListerActeursDeclares(ApiDistanteFactory api)
    {
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().Ajouter("carla", "Carla", "rose");
        return api.Services.GetRequiredService<IEnumerationActeursFoyer>()
            .EnumererActeurs().OrderBy(id => id).ToList();
    }

    [Fact]
    public void Le_selecteur_de_la_dialog_affecter_periode_ne_propose_que_les_acteurs_declares_dont_un_acteur_reel_ajoute_au_runtime()
    {
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);
        var declares = DeclarerCarlaEtListerActeursDeclares(api);

        // When — ouvrir le menu de la case puis la dialog « Affecter une période ».
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click();
                grille.Find("[data-testid='action-affecter-periode']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // Then — le sélecteur expose EXACTEMENT les acteurs déclarés (dont « carla »), lus du store vivant.
        grille.WaitForAssertion(
            () =>
            {
                var ids = grille.FindAll("[data-testid='champ-responsable'] option")
                    .Select(o => o.GetAttribute("value") ?? "")
                    .Where(v => v.Length > 0)
                    .OrderBy(id => id)
                    .ToList();
                Assert.Equal(declares, ids);
                Assert.Contains("carla", ids);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Les_selecteurs_depose_et_recupere_de_la_dialog_transfert_ne_proposent_que_les_acteurs_declares_dont_un_acteur_reel_ajoute_au_runtime()
    {
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);
        var declares = DeclarerCarlaEtListerActeursDeclares(api);

        // When — ouvrir le menu de la case puis la dialog « Définir un transfert ».
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click();
                grille.Find("[data-testid='action-definir-transfert']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));

        // Then — les DEUX sélecteurs (dépose + récupère) exposent EXACTEMENT les acteurs déclarés (dont « carla »).
        grille.WaitForAssertion(
            () =>
            {
                foreach (var testid in new[] { "champ-depose", "champ-recupere" })
                {
                    var ids = grille.FindAll($"[data-testid='{testid}'] option")
                        .Select(o => o.GetAttribute("value") ?? "")
                        .Where(v => v.Length > 0)
                        .OrderBy(id => id)
                        .ToList();
                    Assert.Equal(declares, ids);
                    Assert.Contains("carla", ids);
                }
            },
            TimeSpan.FromSeconds(10));
    }
}
