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
/// Sprint 32 — Sc.2 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la table des acteurs en lecture
/// seule (Parent, écran réellement câblé), cliquer le crayon d'un acteur OUVRE une modal d'édition
/// PRÉ-REMPLIE avec ses champs courants (nom, couleur, rôle borné au référentiel : exactement les rôles
/// du foyer + « sans rôle »). L'identifiant stable est porté par la modal SANS champ éditable (jamais
/// dérivé du libellé). Fermer la modal (annuler) n'émet AUCUNE commande et laisse le tableau inchangé.
/// </summary>
public sealed class FrontWasmConfigActeursCrayonModalPreremplieTests : TestContext
{
    [Fact]
    public void Le_crayon_ouvre_une_modal_preremplie_avec_les_champs_courants_et_annuler_ne_change_rien()
    {
        // Given — l'écran de configuration réellement câblé, identité Parent. On sème un rôle « Nounou »
        // au référentiel réel : le sélecteur de rôle de la modal devra proposer exactement ce rôle + « sans rôle ».
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-nounou", "Nounou");

        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.AttendreLignes(config);

        // When — je clique le crayon de parent-a (Alice).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // Then (pré-remplie) — la modal porte le nom courant « Alice », un sélecteur de couleur et un
        // sélecteur de rôle BORNÉ au référentiel (Nounou + « sans rôle », rien en dur).
        Assert.Equal("Alice", config.Find("[data-testid='champ-nom']").GetAttribute("value"));
        Assert.NotNull(config.Find("[data-testid='palette-couleur']")); // picker minimal (Sc.6)
        var optionsRole = config.Find("[data-testid='selecteur-role-acteur']")
            .QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("Nounou", optionsRole);
        Assert.Contains("sans rôle", optionsRole);
        Assert.Equal(2, optionsRole.Count);

        // Then (id porté, non éditable) — l'identifiant stable de l'acteur est porté par la modal
        // (data-acteur-id = parent-a) sans champ de saisie qui le laisserait modifier.
        Assert.Equal("parent-a", config.Find("[data-testid='dialog-acteur-id']").GetAttribute("data-acteur-id"));

        // When — j'annule.
        this.SurDispatcher(() => config.Find("[data-testid='dialog-acteur-annuler']").Click());

        // Then (aucune commande, tableau inchangé) — la modal se ferme et Alice reste Alice.
        Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
        Assert.Equal("Alice", ConfigActeursModalHarness.NomLigne(
            ConfigActeursModalHarness.LigneParNom(config, "Alice")));
    }
}
