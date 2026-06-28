using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.4 (🖥️ scénario IHM, <c>@erreur</c> — palier 7, 3ᵉ dialog) :
/// <b>caractérisation early-green</b> du gating Invité (règle 9). Le déclencheur d'écriture de la case est
/// <b>mutualisé</b> sur <see cref="SessionPlanning.EstParent"/> : ajouter une 3ᵉ entrée au menu ne change
/// pas le point d'application du droit. On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Pages.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>).
///
/// Anti « vert qui ment » : <b>contrôle positif en regard</b> dans le même test — on prouve d'abord qu'un
/// <b>Parent</b> ouvre bien le menu d'actions au clic (le menu n'est pas cassé pour tous), puis on bascule
/// en <b>Invité</b> et on prouve que le clic n'ouvre <b>ni menu ni dialog</b> et que la grille reste en
/// lecture seule (case non cliquable). Sans le contrôle positif, l'absence de menu serait un faux vert.
/// </summary>
public sealed class FrontWasmDefinirTransfertGatingInviteTests : TestContext
{
    // Mardi 16/06/2026 : la case cliquée (référence dans la fenêtre démarrant au lundi 15/06).
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_N_ouvrir_aucun_menu_ni_dialog_de_transfert_et_garder_la_grille_en_lecture_seule_When_un_invite_en_consultation_seule_clique_une_case()
    {
        // Given — la grille réellement câblée à l'API distante, affichée d'abord pour un Parent.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);
        var session = Services.GetRequiredService<SessionPlanning>();

        // Contrôle positif — le Parent clique la case du 16/06 : le menu d'actions S'OUVRE (le mécanisme
        // n'est pas cassé pour tous). Idempotent sous WaitForAssertion (re-renders async du hub).
        grille.WaitForAssertion(
            () =>
            {
                GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();
                Assert.NotEmpty(grille.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));

        // … on referme le menu (clic sur le fond) pour repartir d'un état neutre.
        grille.Find("[data-testid='menu-actions-case']").Click();
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));

        // When — le rôle bascule en Invité (consultation seule) et la grille est re-rendue, puis l'Invité
        // clique la case du 16/06.
        session.Role = RoleAuteur.Invite;
        grille.Render();
        GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();

        // Then — aucun menu d'actions ne s'ouvre, aucune dialog « Définir un transfert » ne s'ouvre, et la
        // grille reste en lecture seule (le déclencheur de la case n'est plus marqué cliquable).
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
        Assert.Empty(grille.FindAll(".grille-jour-cliquable"));
    }
}
