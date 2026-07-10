using System;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.9 (🖥️ IHM, <c>@erreur</c>) — VOLET RUNTIME (backend néant) :
/// la grille réelle affiche parent-a (« Alice », bleu) le 14/07/2026, mais le <b>service de configuration
/// est injoignable</b> (échec de <b>transport</b>, pas un refus métier comme Sc.8). Depuis l'<b>écran de
/// configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>) dont le <b>canal d'écriture
/// d'édition</b> est injoignable, on renomme parent-a en « Alicia » et on enregistre : l'émission via le
/// canal d'écriture HTTP échoue (<see cref="HttpRequestException"/>), l'écran surface un <b>message clair</b>
/// (« Enregistrement impossible : le service est injoignable, réessayez. »), l'édition <b>n'est pas appliquée</b>
/// et reste à resoumettre — la grille (sur l'API live) conserve « Alice » dans la case du 14/07 et en légende ;
/// sans mise en file ni rejeu.
///
/// <para><b>Robustesse vs proxy loopback Docker (anti-flake).</b> On NE s'appuie PLUS sur un
/// <c>ConnectionRefused</c> vers un port loopback réellement libéré : quand Docker Desktop tourne, son proxy
/// loopback intercepte la connexion et altère la sémantique du refus (l'exception n'est plus une
/// <see cref="HttpRequestException"/> captée par l'écran, ou la connexion pend au-delà du délai d'attente) → ce
/// test flakait selon l'environnement. L'échec de transport est désormais <b>déterministe et indépendant de
/// Docker</b> : le canal d'écriture d'édition (POST .../editer-acteur) lève une <see cref="HttpRequestException"/>
/// au niveau du handler — le contrat exact que l'écran attrape — tandis que l'énumération en lecture transite
/// normalement vers l'API live (le rendu initial de l'écran ne crashe plus). Ce n'est pas une doublure de statut
/// 4xx (refus métier) : aucune réponse n'est fabriquée, l'échec est bien au transport.</para>
///
/// Anti « vert qui ment » : le baseline « Alice » est asserté avant ; la grille reste « Alice » car aucune
/// écriture n'a transité (store live non muté). Un bUnit à pure doublure ne prouverait ni l'échec via le canal
/// HTTP réel, ni le rendu du message.
/// </summary>
public sealed class FrontWasmConfigApiInjoignableTempsReelTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    [Fact]
    public void Should_Afficher_un_message_de_service_injoignable_et_conserver_Alice_dans_la_case_du_14_07_2026_et_en_legende_sans_appliquer_l_edition_When_on_renomme_parent_a_alors_que_l_API_distante_est_arretee()
    {
        // Given — la grille réellement câblée à l'API distante LIVE affiche, à la semaine du lundi
        // 13/07/2026, une période affectée à parent-a (« Alice », bleu) : la case du mardi 14/07 porte
        // « Alice ».
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 14), new DateTime(2026, 7, 14));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la case du 14/07 et la légende portent « Alice ».
        Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille, "14/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var entreeInitiale = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Alice", entreeInitiale.QuerySelector(".legende-nom")!.TextContent.Trim());

        // When — depuis l'écran de configuration câblé à la même API distante LIVE mais dont le CANAL
        // D'ÉCRITURE D'ÉDITION est injoignable (échec de transport déterministe), je renomme parent-a en
        // « Alicia » et j'enregistre (l'émission HTTP réelle se heurte à une HttpRequestException).
        using var ecranConfig = new TestContext();
        ecranConfig.Services.AddSingleton(
            GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "editer-acteur"));
        ecranConfig.Services.AddSingleton(new SessionPlanning()); // contexte rôle réel requis par l'écran (gating Sc.7)
        var config = ecranConfig.RenderComponent<ConfigurationFoyer>();

        // … l'énumération en lecture (GET HTTP réel vers l'API live) déclenche un re-render asynchrone : on
        // attend qu'elle se pose (liste peuplée) avant d'interagir, sinon les handlers d'événements sont
        // ré-attribués entre le Find et le Change (bUnit UnknownEventHandlerId).
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon (plus de sélecteur d'acteur inline).
        // Sur échec de transport, la modal RESTE OUVERTE (saisie à resoumettre), motif surfacé dedans.
        ConfigActeursModalHarness.OuvrirEdition(ecranConfig, config, "parent-a");
        config.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Alicia"));
        config.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — l'enregistrement échoue clairement : le message de service injoignable s'affiche.
        var alerte = config.WaitForElement("[data-testid='motif-echec']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageInjoignable, alerte.TextContent.Trim());

        // … l'édition n'est pas appliquée (sans mise en file ni rejeu) : aucune écriture n'a transité vers
        // l'API live, donc la grille conserve « Alice » dans la case du 14/07 et en légende.
        Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille, "14/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Alice", entree.QuerySelector(".legende-nom")!.TextContent.Trim());

        // … et la saisie reste à resoumettre (le formulaire est toujours affiché).
        Assert.NotEmpty(config.FindAll("form"));
    }
}
