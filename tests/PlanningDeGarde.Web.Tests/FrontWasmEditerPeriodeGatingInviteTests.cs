using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.9 (🖥️ IHM, <c>@erreur</c> — gating Invité, règle 9). L'entrée
/// « Éditer une période » du menu clic-case est gardée par le déclencheur d'écriture <b>mutualisé</b>
/// (<see cref="SessionPlanning.EstParent"/>) : en consultation seule, le clic n'ouvre aucun menu, donc
/// aucune entrée d'édition ni commande possible. On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel).
///
/// CARACTÉRISATION (⚠️ early green ATTENDU) : le gating est mutualisé sur le menu clic-case (acquis paliers
/// antérieurs) — l'entrée « Éditer » posée au Sc.7 en hérite sans code neuf. Anti « vert vacuous » :
/// <b>contrôle positif en regard</b> dans le même test — on prouve d'abord qu'un <b>Parent</b> ouvre bien le
/// menu ET y voit l'entrée « Éditer une période » (le déclencheur n'est pas cassé pour tous), puis on bascule
/// en <b>Invité</b> et on prouve que le clic n'ouvre ni menu ni entrée ni dialog d'édition, et que la période
/// reste inchangée dans le store distant.
/// </summary>
public sealed class FrontWasmEditerPeriodeGatingInviteTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Ne_proposer_aucune_entree_ni_dialog_d_edition_et_laisser_la_periode_inchangee_When_un_invite_en_consultation_seule_clique_une_case()
    {
        // Given — la grille câblée à l'API distante, affichée d'abord pour un Parent ; une période
        // « Nina la nounou » attribue le mardi 16/06/2026 (pour pouvoir l'observer inchangée à la fin).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "nounou", Mardi_16_06_2026, Mardi_16_06_2026);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);
        var session = Services.GetRequiredService<SessionPlanning>();

        // Contrôle positif — le Parent clique la case du 16/06 : le menu s'ouvre ET propose bien l'entrée
        // « Éditer une période » (le déclencheur n'est pas cassé pour tous). Idempotent sous WaitForAssertion.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
                Assert.NotEmpty(grille.FindAll("[data-testid='action-editer-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // … on referme le menu (clic sur le fond) pour repartir d'un état neutre.
        this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case']").Click());
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));

        // When — le rôle bascule en Invité (consultation seule), la grille est re-rendue, puis l'Invité
        // clique la case du 16/06.
        session.Role = RoleAuteur.Invite;
        grille.Render();
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());

        // Then — aucun menu, aucune entrée « Éditer une période », aucune dialog d'édition, et la grille
        // reste en lecture seule (le déclencheur de la case n'est plus marqué cliquable).
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='action-editer-periode']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-editer-periode']"));
        Assert.Empty(grille.FindAll(".grille-jour-cliquable"));

        // … et la période de « Nina la nounou » reste inchangée dans le store de l'API distante.
        using var scope = api.Services.CreateScope();
        var periodesDuJour = scope.ServiceProvider.GetRequiredService<PeriodesDuJourQuery>();
        Assert.Contains(periodesDuJour.Lister(new DateOnly(2026, 6, 16)), p => p.ResponsableId == "nounou");
    }
}
