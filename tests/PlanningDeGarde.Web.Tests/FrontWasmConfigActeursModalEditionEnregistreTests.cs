using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 32 — Sc.3 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la modal d'édition ouverte sur un
/// acteur (Parent, écran réellement câblé, API distante réelle), modifier son nom et sa couleur puis
/// cliquer « Enregistrer » émet les commandes CRUD acteurs EXISTANTES via le canal HTTP réel (aucun
/// handler neuf). En succès, la modal se ferme, le tableau est relu depuis le store et reflète le
/// changement, l'identifiant stable restant inchangé (renommage, pas recréation).
/// </summary>
public sealed class FrontWasmConfigActeursModalEditionEnregistreTests : TestContext
{
    [Fact]
    public void Enregistrer_depuis_la_modal_ferme_la_modal_et_la_table_relue_reflete_le_changement_sur_le_meme_identifiant()
    {
        // Given — l'écran de configuration réellement câblé, identité Parent, modal d'édition ouverte sur
        // parent-a (Alice).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // When — je renomme « Alice » en « Alicia », la recolorie en vert, puis j'enregistre (POST réel
        // /api/canal/editer-acteur).
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Alicia"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-vert']").Click()); // palette (Sc.6)
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal se ferme et le tableau relu reflète « Alicia » (plus d'« Alice »), sur le MÊME
        // identifiant stable parent-a (renommage, pas recréation).
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                var alicia = ConfigActeursModalHarness.LigneParNom(config, "Alicia");
                Assert.Equal("parent-a", alicia.GetAttribute("data-acteur-id"));
                Assert.Equal("vert", alicia.GetAttribute("data-couleur"));
                Assert.DoesNotContain(
                    config.FindAll("[data-testid='acteur-foyer']"),
                    li => ConfigActeursModalHarness.NomLigne(li) == "Alice");
            },
            TimeSpan.FromSeconds(10));
    }
}
