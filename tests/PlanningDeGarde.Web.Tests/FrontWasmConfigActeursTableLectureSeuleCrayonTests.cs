using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 32 — Sc.1 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : sur l'onglet « Acteurs » de l'écran de
/// configuration réellement câblé (<see cref="ConfigurationFoyer"/>, API distante réelle, store réel),
/// sous identité Parent, la table des acteurs est PUREMENT EN LECTURE (pastille + nom, email + statut du
/// compte, rôle, état actif/admin en pastille/badge). Les deux cartes d'édition INLINE (« Modifier »,
/// « Ajouter ») et les contrôles d'écriture inline de la table (sélecteur de rôle, champ email, boutons
/// supprimer / désigner-admin / créer-compte) ne sont PLUS rendus. Une colonne « Actions » porte, par
/// ligne, un CRAYON d'édition, et un bouton « Ajouter un acteur » figure au bas du tableau.
/// </summary>
public sealed class FrontWasmConfigActeursTableLectureSeuleCrayonTests : TestContext
{
    [Fact]
    public void La_table_des_acteurs_est_en_lecture_seule_les_ecritures_inline_sont_retirees_et_une_colonne_crayon_plus_un_bouton_ajouter_apparaissent()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé,
        // identité Parent). On sème un compte inactif + une désignation admin sur parent-a (Alice) via
        // les ports d'écriture réels, pour observer l'état actif/admin en badge de LECTURE dans la table.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice", "alice@foyer.fr", StatutCompte.Inactif, "parent-a");
        api.Services.GetRequiredService<IEditeurAdminsFoyer>().DesignerAdmin("parent-a");

        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.AttendreLignes(config);

        // Then (lecture) — chaque acteur est sur une ligne de lecture : pastille + nom, rôle résolu,
        // et l'état en badge. Alice porte son compte (email + statut) et le badge « admin ».
        var alice = ConfigActeursModalHarness.LigneParNom(config, "Alice");
        Assert.NotNull(alice.QuerySelector(".acteur-pastille"));
        Assert.NotNull(alice.QuerySelector("[data-testid='role-acteur-courant']"));
        Assert.Contains("alice@foyer.fr", alice.QuerySelector("[data-testid='compte-acteur']")!.TextContent);
        Assert.NotNull(alice.QuerySelector("[data-testid='acteur-etat-compte']"));
        Assert.NotNull(alice.QuerySelector("[data-testid='acteur-admin-marqueur']"));

        // Then (inline RETIRÉ) — modal fermée, AUCUN contrôle d'écriture inline n'est rendu sur la page :
        // ni carte « Modifier » (sélecteur d'acteur, champ nom), ni carte « Ajouter » (champ nom d'ajout),
        // ni sélecteur de rôle / champ email / boutons inline de la table.
        Assert.Empty(config.FindAll("[data-testid='selecteur-acteur-edition']"));
        Assert.Empty(config.FindAll("[data-testid='champ-nom']"));
        Assert.Empty(config.FindAll("[data-testid='champ-nom-ajout']"));
        Assert.Empty(config.FindAll("[data-testid='selecteur-role-acteur']"));
        Assert.Empty(config.FindAll("[data-testid='champ-email-compte']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer']"));
        // Swap s33 Sc.4 : l'admin est un TOGGLE de la MODAL (plus un bouton) — absent tant que la modal est fermée.
        Assert.Empty(config.FindAll("[data-testid='toggle-admin']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));

        // Then (crayon + ajouter) — une colonne Actions porte un crayon PAR ligne, et un bouton
        // « Ajouter un acteur » est présent au bas du tableau.
        var crayons = config.FindAll("[data-testid='crayon-acteur']");
        Assert.Equal(config.FindAll("[data-testid='acteur-foyer']").Count, crayons.Count);
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
    }
}
