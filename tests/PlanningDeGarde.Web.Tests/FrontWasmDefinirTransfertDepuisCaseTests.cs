using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ scénario IHM, <c>@nominal</c> — palier 7 « écriture en
/// contexte », 3ᵉ dialog qui referme l'épic É12) : le comportement neuf vit dans le <c>.razor</c> —
/// cliquer une case ouvre le <b>menu d'actions</b> de la case, désormais à <b>3 entrées</b>, dont la
/// 3ᵉ « Définir un transfert » ouvre la dialog <see cref="Web.Components.DefinirTransfertDialog"/>
/// pré-remplie sur la date de la case ; la validation <b>ferme la dialog</b>, lève un <b>accusé
/// « Transfert défini » à part, non bloquant</b>, et le transfert transite réellement jusqu'au store de
/// l'API distante. On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/>
/// (front WASM) câblée à une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel,
/// canal HTTP réel). Aucun handler ni règle backend neuf : réutilisation de la commande
/// <c>DefinirTransfert</c> et de l'endpoint <c>POST /api/transferts</c> (s01→s05).
///
/// Anti « vert qui ment » : si le clic n'ouvre pas le menu, si la 3ᵉ entrée manque, si la validation ne
/// transite pas jusqu'au store distant, ou si le transfert retombe à une autre date, l'observable distant
/// reste vide → rouge. Un bUnit à doublure ne verrait ni le câblage distant ni l'enregistrement réel.
/// </summary>
public sealed class FrontWasmDefinirTransfertDepuisCaseTests : TestContext
{
    // Mardi 16/06/2026 : la case cliquée. Référence « aujourd'hui » au 16/06 → fenêtre démarrant au
    // lundi 15/06, qui couvre le mardi 16/06.
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Fermer_la_dialog_afficher_l_accuse_transfert_defini_a_part_et_enregistrer_le_transfert_parent_a_vers_parent_b_ecole_08h30_le_16_06_2026_When_un_parent_le_definit_via_la_3e_entree_du_menu_clic_case()
    {
        // Given — la grille réellement câblée à l'API distante, affichée pour un Parent (store réel vierge),
        // fenêtre couvrant le mardi 16/06/2026.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … aucun accusé n'est affiché tant qu'aucun transfert n'a été défini.
        Assert.Empty(grille.FindAll("[data-testid='accuse-transfert-defini']"));

        // When — un Parent clique la case du mardi 16/06 → le menu d'actions s'ouvre → il choisit la 3ᵉ
        // entrée « Définir un transfert » → la dialog s'ouvre. Ouverture idempotente sous WaitForAssertion :
        // robuste aux re-renders async (connexion du hub SignalR du harnais) sous charge parallèle.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-definir-transfert']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
            },
            TimeSpan.FromSeconds(10));

        // … il choisit « Parent A » dépositaire, « Parent B » récupérateur (ids stables, règle 19), lieu
        // « École » à 08:30, sans toucher la date pré-remplie sur la case (16/06), puis valide.
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-depose']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-recupere']").Change("parent-b"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-lieu']").Change("école"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] [data-testid='champ-heure']").Change("08:30"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-definir-transfert'] form").Submit());

        // Then — la dialog se ferme ET un accusé « Transfert défini » s'affiche à part (non bloquant),
        // relu depuis l'app réellement câblée.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-definir-transfert']"));
                Assert.NotEmpty(grille.FindAll("[data-testid='accuse-transfert-defini']"));
            },
            TimeSpan.FromSeconds(10));

        // … et le transfert a réellement transité jusqu'au store de l'API distante (rempart anti
        // vert-qui-ment) : Parent A → Parent B, lieu « école », 08:30, le mardi 16/06/2026.
        using var scope = api.Services.CreateScope();
        var transferts = scope.ServiceProvider.GetRequiredService<ITransfertRepository>();
        var transfert = Assert.Single(transferts.AllSnapshots());
        Assert.Equal("parent-a", transfert.DeposeParId);
        Assert.Equal("parent-b", transfert.RecupereParId);
        Assert.Equal("école", transfert.LieuId);
        Assert.Equal(TimeSpan.FromHours(8.5), transfert.Heure);
        Assert.Equal(new DateOnly(2026, 6, 16), DateOnly.FromDateTime(transfert.Date));
    }
}
