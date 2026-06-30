using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ IHM, <c>@erreur</c> — gating Invité, règle 9). L'entrée
/// « Supprimer un slot » du menu clic-case est gardée par le déclencheur d'écriture <b>mutualisé</b>
/// (<see cref="SessionPlanning.EstParent"/>) : en consultation seule, le clic n'ouvre aucun menu, donc
/// aucune entrée de suppression ni commande possible. On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel).
///
/// Anti « vert vacuous » : <b>contrôle positif en regard</b> dans le même test — on prouve d'abord qu'un
/// <b>Parent</b> ouvre bien le menu ET y voit l'entrée « Supprimer un slot » (le déclencheur n'est pas
/// cassé pour tous), puis on bascule en <b>Invité</b> et on prouve que le clic n'ouvre ni menu ni entrée ni
/// dialog de suppression, et que le slot reste inchangé dans le store distant.
/// </summary>
public sealed class FrontWasmSupprimerSlotGatingInviteTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Ne_proposer_aucune_entree_ni_dialog_de_suppression_de_slot_et_laisser_le_slot_inchange_When_un_invite_en_consultation_seule_clique_une_case()
    {
        // Given — la grille câblée à l'API distante, affichée d'abord pour un Parent ; un slot « École »
        // 08h30-16h30 pour Léa le mardi 16/06/2026 (pour pouvoir l'observer inchangé à la fin).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerSlot(api, "lea", "École", new DateTime(2026, 6, 16, 8, 30, 0), new DateTime(2026, 6, 16, 16, 30, 0));
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);
        var session = Services.GetRequiredService<SessionPlanning>();

        // Contrôle positif — le Parent clique la case du 16/06 : le menu s'ouvre ET propose bien l'entrée
        // « Supprimer un slot » (le déclencheur n'est pas cassé pour tous). Idempotent sous WaitForAssertion.
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
                Assert.NotEmpty(grille.FindAll("[data-testid='action-supprimer-slot']"));
            },
            TimeSpan.FromSeconds(10));

        // … on referme le menu (clic sur le fond) pour repartir d'un état neutre.
        grille.Find("[data-testid='menu-actions-case']").Click();
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));

        // When — le rôle bascule en Invité (consultation seule), la grille est re-rendue, puis l'Invité
        // clique la case du 16/06.
        session.Role = RoleAuteur.Invite;
        grille.Render();
        GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();

        // Then — aucun menu, aucune entrée « Supprimer un slot », aucune dialog de suppression, et la
        // grille reste en lecture seule (le déclencheur de la case n'est plus marqué cliquable).
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='action-supprimer-slot']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-supprimer-slot']"));
        Assert.Empty(grille.FindAll(".grille-jour-cliquable"));

        // … et le slot « École » reste inchangé dans le store de l'API distante.
        using var scope = api.Services.CreateScope();
        var slotsDuJour = scope.ServiceProvider.GetRequiredService<SlotsDuJourQuery>();
        Assert.Contains(slotsDuJour.Lister(new DateOnly(2026, 6, 16)), s => s.LieuId == "École");
    }
}
