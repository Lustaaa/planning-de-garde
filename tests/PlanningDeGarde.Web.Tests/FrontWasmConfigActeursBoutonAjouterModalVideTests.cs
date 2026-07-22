using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 32 — Sc.4 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la table des acteurs (Parent,
/// écran réellement câblé, API distante réelle), cliquer « Ajouter un acteur » ouvre la MÊME modal avec
/// tous les champs VIDES (mode création, aucun acteur pré-sélectionné). Saisir un nom (et une couleur)
/// puis « Enregistrer » crée un acteur via la commande d'ajout EXISTANTE avec un identifiant stable NEUF
/// (jamais le libellé) ; le nouvel acteur apparaît aussitôt dans le tableau (relecture du store), sans
/// recharger la page.
/// </summary>
public sealed class FrontWasmConfigActeursBoutonAjouterModalVideTests : TestContext
{
    [Fact]
    public void Le_bouton_ajouter_ouvre_la_modal_vide_et_enregistrer_cree_un_acteur_avec_un_identifiant_stable_neuf()
    {
        // Given — l'écran de configuration réellement câblé, identité Parent. « Carla » n'existe pas encore.
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.AttendreLignes(config);
        Assert.DoesNotContain(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => ConfigActeursModalHarness.NomLigne(li) == "Carla");

        // When — j'ouvre la modal via « Ajouter un acteur » : elle s'ouvre en mode création, champs VIDES.
        ConfigActeursModalHarness.OuvrirAjout(this, config);
        Assert.Equal("", config.Find("[data-testid='champ-nom-ajout']").GetAttribute("value") ?? "");

        // When — je saisis « Carla » en rose et j'enregistre (POST réel /api/foyer/acteurs).
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom-ajout']").Change("Carla"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-ajout-rose']").Click()); // palette (Sc.6)
        this.SurDispatcher(() => config.Find("#form-ajout").Submit());

        // Then — la modal se ferme et « Carla » apparaît dans le tableau relu depuis le store, avec un
        // identifiant stable NEUF (ni vide, ni dérivé du libellé « Carla »).
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var carla = ConfigActeursModalHarness.LigneParNom(config, "Carla");
                var id = carla.GetAttribute("data-acteur-id");
                Assert.False(string.IsNullOrWhiteSpace(id));
                Assert.NotEqual("Carla", id);
            },
            TimeSpan.FromSeconds(10));
    }
}
