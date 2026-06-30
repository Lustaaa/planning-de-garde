using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 19 — Sc.6 (🖥️ @ihm) — Acceptation de NIVEAU RUNTIME : <b>store d'acteurs vide</b> (1er
/// lancement) → les sélecteurs de responsable des dialogs d'écriture sont vides ET invitent à ajouter
/// un acteur (« Aucun acteur, ajoutez-en. »), la grille est entièrement neutre, la légende vide, et
/// <b>aucun fantôme</b> (« Parent A / Parent B ») n'apparaît nulle part.
///
/// On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée
/// à une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>) dont le store d'acteurs est
/// <b>vidé</b> (Given : on supprime tous les acteurs seedés du store réel pour reproduire le 1er
/// lancement Mongo vide, asymétrie s15). Rempart anti « vert qui ment » : tant qu'aucune invite n'est
/// rendue, ou qu'un acteur fictif fuite, l'observable réel reste muet → rouge.
/// </summary>
public sealed class FrontWasmStoreVideInviteTests : TestContext
{
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    /// <summary>Given — vide le store d'acteurs réel de l'API distante (1er lancement : aucun acteur déclaré).</summary>
    private static void ViderStoreActeurs(ApiDistanteFactory api)
    {
        var editeur = api.Services.GetRequiredService<IEditeurConfigurationFoyer>();
        foreach (var id in api.Services.GetRequiredService<IEnumerationActeursFoyer>().EnumererActeurs().ToList())
            editeur.Supprimer(id);
    }

    [Fact]
    public void Store_vide_la_grille_est_neutre_la_legende_vide_et_aucun_fantome_n_apparait()
    {
        using var api = new ApiDistanteFactory();
        ViderStoreActeurs(api);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // Aucune case n'affiche de responsable (toutes neutres), aucune entrée de légende.
        Assert.Empty(grille.FindAll("[data-testid='nom-responsable']"));
        Assert.Empty(grille.FindAll("[data-testid='legende-entree']"));

        // Zéro fantôme : nulle part dans le rendu de la grille « Parent A » / « Parent B ».
        Assert.DoesNotContain("Parent A", grille.Markup);
        Assert.DoesNotContain("Parent B", grille.Markup);
    }

    [Fact]
    public void Store_vide_le_selecteur_de_la_dialog_affecter_periode_est_vide_et_invite_a_ajouter_un_acteur()
    {
        using var api = new ApiDistanteFactory();
        ViderStoreActeurs(api);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click();
                grille.Find("[data-testid='action-affecter-periode']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));

        grille.WaitForAssertion(
            () =>
            {
                // Sélecteur vide d'acteur (aucune option de valeur non vide), invite affichée.
                var idsProposes = grille.FindAll("[data-testid='champ-responsable'] option")
                    .Select(o => o.GetAttribute("value") ?? "")
                    .Where(v => v.Length > 0)
                    .ToList();
                Assert.Empty(idsProposes);

                var invite = grille.Find("[data-testid='dialog-affecter-periode'] [data-testid='aucun-acteur-invite']");
                Assert.Contains("Aucun acteur", invite.TextContent);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Store_vide_les_selecteurs_de_la_dialog_transfert_sont_vides_et_invitent_a_ajouter_un_acteur()
    {
        using var api = new ApiDistanteFactory();
        ViderStoreActeurs(api);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click();
                grille.Find("[data-testid='action-definir-transfert']").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));

        grille.WaitForAssertion(
            () =>
            {
                foreach (var testid in new[] { "champ-depose", "champ-recupere" })
                {
                    var idsProposes = grille.FindAll($"[data-testid='{testid}'] option")
                        .Select(o => o.GetAttribute("value") ?? "")
                        .Where(v => v.Length > 0)
                        .ToList();
                    Assert.Empty(idsProposes);
                }

                var invite = grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='aucun-acteur-invite']");
                Assert.Contains("Aucun acteur", invite.TextContent);
            },
            TimeSpan.FromSeconds(10));
    }
}
