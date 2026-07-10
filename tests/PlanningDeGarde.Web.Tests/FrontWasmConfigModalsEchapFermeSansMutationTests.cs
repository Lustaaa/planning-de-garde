using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 33 — finition PO (rework in-goal, aucun handler/commande/invariant neuf) : les modals de la Config
/// foyer se ferment à la touche <b>Échap</b>, strictement comme le bouton « Annuler » — fermeture SANS aucune
/// mutation (aucune commande émise, saisie abandonnée), jamais confondu avec « Enregistrer ». Prouvé sur
/// l'écran réellement câblé (API distante réelle, store réel, DI réelle) pour les TROIS onglets livrés ce
/// sprint : Acteurs (modal crayon), Rôles (modal crayon/Ajouter), Cycle (modal « Éditer le cycle »). Le
/// comportement Échap est factorisé dans le composant modal commun <c>ModalConfig</c>. On presse Échap sur la
/// modal ouverte (après avoir modifié un champ) puis on vérifie : (1) la modal est fermée ; (2) le store réel
/// n'a PAS bougé (aucune écriture n'a transité). Un cas supplémentaire prouve l'invariant : même en état de
/// REFUS (motif affiché, saisie conservée), Échap ferme quand même sans rien réémettre.
/// </summary>
public sealed class FrontWasmConfigModalsEchapFermeSansMutationTests : TestContext
{
    private IRenderedComponent<ConfigurationFoyer> RendreConfig(ApiDistanteFactory api)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    private void PresserEchap(IRenderedComponent<ConfigurationFoyer> config, string dialogTestId)
        => this.SurDispatcher(() => config.Find($"[data-testid='{dialogTestId}']")
            .KeyDown(new KeyboardEventArgs { Key = "Escape" }));

    [Fact]
    public void Echap_sur_la_modal_Acteur_la_ferme_sans_muter_le_store()
    {
        // Given — écran Parent réellement câblé, modal d'édition ouverte sur parent-a (Alice) ; je modifie le nom.
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(api);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("ZZZ modifié non enregistré"));

        // When — je presse Échap sur la modal (équivalent « Annuler »).
        PresserEchap(config, "dialog-acteur");

        // Then — la modal se ferme et le store réel porte toujours « Alice » (aucune écriture n'a transité).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-acteur']")),
            TimeSpan.FromSeconds(10));
        var nom = api.Services.GetRequiredService<IReferentielResponsables>().NomDe("parent-a");
        Assert.Equal("Alice", nom);
    }

    [Fact]
    public void Echap_sur_la_modal_Role_la_ferme_sans_muter_le_referentiel()
    {
        // Given — un rôle « Nounou » est semé ; modal d'édition ouverte dessus ; je modifie le libellé.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        var config = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count == 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-role']")
            .Single(b => b.GetAttribute("data-role-id") == "role-nounou").Click());
        config.WaitForElement("[data-testid='dialog-role']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Renommage non enregistré"));

        // When — Échap sur la modal.
        PresserEchap(config, "dialog-role");

        // Then — la modal se ferme et le référentiel porte toujours « Nounou » (aucune écriture).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-role']")),
            TimeSpan.FromSeconds(10));
        var roles = api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles();
        Assert.Equal("Nounou", roles.Single(r => r.Id == "role-nounou").Libelle);
    }

    [Fact]
    public void Echap_sur_la_modal_Cycle_la_ferme_sans_muter_le_cycle()
    {
        // Given — un cycle N=2 est déclaré (parent-a semaine 0, parent-b semaine 1) ; modal d'édition ouverte ;
        // je réaffecte la semaine 1 dans la saisie (non enregistrée).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IReferentielCycleDeFond>()
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        var config = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='cycle-foyer']").Count == 2, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='dialog-cycle']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-a"));

        // When — Échap sur la modal.
        PresserEchap(config, "dialog-cycle");

        // Then — la modal se ferme et le store cycle porte toujours parent-b en semaine 1 (aucune écriture).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-cycle']")),
            TimeSpan.FromSeconds(10));
        var cycle = api.Services.GetRequiredService<IReferentielCycleDeFond>().CycleCourant()!;
        Assert.Equal("parent-b", cycle.Affectations[1]);
    }

    [Fact]
    public void Echap_ferme_meme_une_modal_en_etat_de_refus_sans_rien_reemettre()
    {
        // Given — invariant : « Nounou » existe ; on ouvre l'ajout et on tente le doublon « Nounou » → REFUS
        // (motif affiché, saisie conservée, modal restée ouverte).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");
        var config = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count == 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Nounou"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());
        config.WaitForElement("[data-testid='motif-echec-role']", TimeSpan.FromSeconds(10));

        // When — Échap sur la modal EN ÉTAT DE REFUS.
        PresserEchap(config, "dialog-role");

        // Then — Échap ferme quand même (= annuler) et n'a rien réémis : toujours un seul rôle « Nounou ».
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-role']")),
            TimeSpan.FromSeconds(10));
        Assert.Single(api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles());
    }
}
