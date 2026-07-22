using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ IHM, <c>@nominal</c>) — VOLET RUNTIME : depuis
/// l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent
/// ajoute l'actrice « Carla » en rose et valide. <b>Sans rechargement</b>, l'écran de configuration
/// — câblé à l'<b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel
/// <c>ConfigurationFoyerEnMemoire</c>, énumération réelle <c>IEnumerationActeursFoyer</c>) — liste
/// « Carla » parmi les acteurs du foyer.
///
/// Chemin réel exercé : l'écran <b>énumère les acteurs depuis le store</b> via le canal de lecture
/// HTTP réel (<c>GET /api/foyer/acteurs</c>), puis émet l'ajout via le <b>canal d'écriture HTTP
/// réel</b> (<c>POST /api/foyer/acteurs</c>, règle 27 — aucune vue n'écrit le domaine en
/// direct) → le handler génère un identifiant stable neuf et persiste l'acteur → l'écran ré-énumère
/// le store et fait apparaître Carla, sans recharger la page.
///
/// Anti « vert qui ment » : si l'écran énumérait encore la <b>liste statique front</b>
/// (<c>Foyer.ActeursEditables</c>), ou si l'endpoint d'ajout / d'énumération manquait, Carla
/// n'apparaîtrait jamais → rouge. Un bUnit à doublure ne prouverait ni la DI réelle, ni le chemin
/// HTTP d'écriture, ni l'énumération depuis le store durable.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigAjouterActeurTempsReelTests : TestContext
{
    [Fact]
    public void Should_Afficher_Carla_dans_la_liste_des_acteurs_de_l_ecran_de_configuration_sans_recharger_la_page_When_un_parent_ajoute_l_actrice_Carla_en_rose_depuis_l_ecran_de_configuration()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé,
        // énumération réelle). Le client HTTP du front pointe le transport réel de l'API in-test.
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning()); // contexte rôle réel (Parent par défaut) requis par l'écran (gating Sc.7)

        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran énumère les acteurs DEPUIS LE STORE (GET HTTP réel) : la liste se peuple, sans Carla.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        Assert.DoesNotContain(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla");

        // When — un parent ajoute « Carla » en rose et valide (émission via le canal d'écriture HTTP réel).
        // Refonte s32 : l'ajout passe par la MODAL ouverte au bouton « Ajouter un acteur » (mode création).
        ConfigActeursModalHarness.OuvrirAjout(this, config);
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom-ajout']").Change("Carla"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-ajout-rose']").Click()); // palette (Sc.6)
        this.SurDispatcher(() => config.Find("#form-ajout").Submit());

        // Then — sans rechargement (même instance rendue), la liste de l'écran de configuration contient
        // désormais « Carla » : preuve qu'elle est énumérée depuis le store durable après l'ajout HTTP.
        config.WaitForAssertion(
            () => Assert.Contains(
                config.FindAll("[data-testid='acteur-foyer']"),
                li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla"),
            TimeSpan.FromSeconds(10));
    }
}
