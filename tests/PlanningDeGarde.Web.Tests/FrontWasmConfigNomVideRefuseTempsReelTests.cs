using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ IHM, <c>@erreur</c>) — VOLET RUNTIME : la grille réelle
/// affiche parent-b (« Bruno », orange) sur la case du 15/07/2026. Depuis l'<b>écran de configuration
/// réellement câblé</b> (<see cref="ConfigurationFoyer"/>), on tente d'enregistrer parent-b avec un nom
/// <b>tout-espaces</b> (vide utile). L'édition est <b>refusée</b> : le front émet la commande via le
/// <b>canal d'écriture HTTP réel</b> (<c>POST /api/canal/editer-acteur</c>), le handler applique sa garde
/// « nom non vide » et renvoie un <c>Result.Echec</c> dont le <b>motif métier</b> (« le nom ne peut pas
/// être vide ») est <b>surfacé à l'écran</b> ; le store n'est pas muté et aucune diffusion n'est émise →
/// la case du 15/07 et l'entrée de légende <b>conservent « Bruno »</b> (inchangé).
///
/// Anti « vert qui ment » : le baseline « Bruno » est asserté avant la tentative, et le message d'erreur
/// est observé sur le DOM réellement rendu (pas un état interne). Si l'écran ne surfaçait pas le motif
/// renvoyé par l'API (ou si le front avalait le nom vide en null → succès silencieux), le test serait
/// rouge. Un bUnit à doublure ne prouverait ni le refus via le canal HTTP réel, ni le rendu du message.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigNomVideRefuseTempsReelTests : TestContext
{
    [Fact]
    public void Should_Afficher_un_message_clair_le_nom_ne_peut_pas_etre_vide_et_conserver_Bruno_dans_la_case_du_15_07_2026_et_en_legende_When_on_tente_d_enregistrer_parent_b_avec_un_nom_vide()
    {
        // Given — la grille réellement câblée affiche, à la semaine du lundi 13/07/2026, une période
        // affectée à parent-b (« Bruno », orange) : la case du mercredi 15/07 porte « Bruno ».
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 7, 15), new DateTime(2026, 7, 15));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la case du 15/07 et la légende portent « Bruno ».
        Assert.Equal("Bruno", GrilleRuntimeHarness.CaseDuJour(grille, "15/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var entreeInitiale = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Bruno", entreeInitiale.QuerySelector(".legende-nom")!.TextContent.Trim());

        // When — depuis l'écran de configuration réellement câblé, je tente d'enregistrer parent-b avec un
        // nom tout-espaces (vide utile), sans recoloriage (émission via le canal d'écriture HTTP réel).
        var config = RenderComponent<ConfigurationFoyer>();

        // … garde déterministe : attendre la fin de l'énumération asynchrone des acteurs (GET HTTP réel)
        // avant d'interagir avec le select, sinon un re-render intercalé invalide le handler d'événement
        // (UnknownEventHandlerId) — standard anti-flake *TempsReel* déjà appliqué par les tests config frères.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon (plus de sélecteur d'acteur inline).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-b");
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("   "));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — l'édition est refusée : le motif métier renvoyé par l'API est affiché clairement DANS la
        // modal, qui reste ouverte (Sc.5), tandis que la grille ne bouge pas.
        config.WaitForAssertion(
            () => Assert.Equal(
                "le nom ne peut pas être vide",
                config.Find("[data-testid='motif-echec']").TextContent.Trim()),
            TimeSpan.FromSeconds(15));

        // … et la grille n'a pas bougé : aucune diffusion sur refus, la case du 15/07 et la légende
        // conservent « Bruno » (store non muté, ancien nom conservé).
        Assert.Equal("Bruno", GrilleRuntimeHarness.CaseDuJour(grille, "15/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Bruno", entree.QuerySelector(".legende-nom")!.TextContent.Trim());
    }
}
