using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.3 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, iso-fonctionnel) : sous l'onglet
/// « Acteurs » (actif par défaut) de l'écran de configuration réellement câblé
/// (<see cref="ConfigurationFoyer"/>, API distante réelle, store réel), le CRUD acteurs existant —
/// <b>éditer</b> (renommer + recolorier), <b>ajouter</b>, <b>supprimer</b> — aboutit <b>exactement comme
/// avant la refonte</b> (aucun handler neuf), observé sur les <b>observables propres de l'écran</b>
/// (canal HTTP réel) : la liste relue reflète l'ajout puis la suppression, et l'édition est confirmée.
/// Rempart de non-régression du réagencement en onglets (Sc.2) : si le panneau « Acteurs » n'avait pas
/// correctement recâblé les formulaires d'édition / d'ajout / de suppression, l'une de ces écritures
/// échouerait → rouge.
///
/// <para>Ce test reste <b>déterministe</b> (aucun hub SignalR câblé ici) : la propagation « grille et
/// légende relues immédiatement » est prouvée — sur le même onglet Acteurs actif par défaut — par les
/// tests frères de niveau runtime <c>FrontWasmConfigRenommerActeurTempsReelTests</c> (édition → grille +
/// légende, SignalR) et <c>FrontWasmConfigSupprimerActeurTempsReelTests</c> (suppression → légende,
/// SignalR). On ne redouble donc pas ici la voie temps réel (dette *TempsReel* P2), on borne le CRUD
/// iso-fonctionnel accessible depuis l'onglet.</para>
/// </summary>
public sealed class FrontWasmConfigOngletActeursCrudIsoFonctionnelTempsReelTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    [Fact]
    public void Should_Editer_ajouter_et_supprimer_un_acteur_iso_fonctionnellement_depuis_l_onglet_Acteurs_actif_par_defaut()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé),
        // identité Parent. Aucun hub n'est câblé : l'écran reste consultable/éditable, déterministe.
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // … l'onglet « Acteurs » est actif par défaut : tout le CRUD s'y exerce sans changer d'onglet.
        // Fusion des sections (hors-sprint) : plus d'onglets — le CRUD acteurs s'exerce sur la page unique.

        // When (édition) — je renomme parent-a en « Alicia » ET le recolorie en vert, puis j'enregistre
        // (canal d'écriture HTTP réel : POST /api/canal/editer-acteur).
        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon.
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Alicia"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-vert']").Click()); // palette (Sc.6)
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then (édition) — sur succès la modal se ferme et la table relue reflète « Alicia » (canal accepté,
        // aucun handler neuf).
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                Assert.Contains(config.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Alicia");
            },
            TimeSpan.FromSeconds(10));

        // When (ajout) — j'ouvre la modal d'ajout et j'ajoute « Carla » en rose (POST /api/canal/ajouter-acteur).
        ConfigActeursModalHarness.OuvrirAjout(this, config);
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom-ajout']").Change("Carla"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-ajout-rose']").Click()); // palette (Sc.6)
        this.SurDispatcher(() => config.Find("#form-ajout").Submit());

        // Then (ajout) — sans rechargement, la liste relue de l'écran contient désormais « Carla ».
        config.WaitForAssertion(
            () => Assert.Contains(config.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Carla"),
            TimeSpan.FromSeconds(10));

        // When (suppression) — j'ouvre la modal de « Carla » (id stable neuf) et je la supprime
        // (POST /api/canal/supprimer-acteur).
        var carlaId = config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => NomLigne(li) == "Carla").GetAttribute("data-acteur-id")!;
        ConfigActeursModalHarness.OuvrirEdition(this, config, carlaId);
        this.SurDispatcher(() => config.Find("[data-testid='bouton-supprimer']").Click());

        // Then (suppression) — sans rechargement, « Carla » quitte la liste relue, et un accusé s'affiche.
        config.WaitForAssertion(
            () => Assert.DoesNotContain(config.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Carla"),
            TimeSpan.FromSeconds(10));
        Assert.Contains("Acteur supprimé", config.Find("[data-testid='accuse-suppression']").TextContent);
    }
}
