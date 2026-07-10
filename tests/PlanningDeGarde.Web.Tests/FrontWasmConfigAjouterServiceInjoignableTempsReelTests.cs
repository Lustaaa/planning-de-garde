using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.9 (🖥️ IHM, <c>@erreur</c>) — VOLET 100 % RUNTIME (backend néant) :
/// depuis l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent a
/// saisi « Carla » (rose) dans le formulaire d'AJOUT et valide alors que le <b>service de configuration est
/// injoignable</b>. L'échec est de <b>transport</b> (le canal d'ajout <c>POST /api/canal/ajouter-acteur</c>
/// n'aboutit pas : <see cref="System.Net.Http.HttpRequestException"/>), PAS un refus métier 4xx comme Sc.8 :
/// le handler <c>AjouterActeur</c> ne s'exécute jamais. L'écran doit surfacer un <b>message d'échec clair</b>
/// (« Enregistrement impossible : le service est injoignable, réessayez. »), <b>conserver</b> la saisie « Carla /
/// rose » à resoumettre, et <b>n'enregistrer aucun acteur</b> (la liste reste inchangée), sans crash.
///
/// Anti « vert qui ment » : l'écran est câblé à l'<b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>) ;
/// l'énumération initiale (GET) transite réellement et peuple la liste ; SEUL le POST d'ajout subit un échec de
/// transport déterministe (handler qui lève <see cref="System.Net.Http.HttpRequestException"/>, indépendant du
/// proxy loopback Docker). Si l'écran avalait l'échec (cas avant fix : <c>Ajouter</c> sans <c>catch</c>), aucun
/// message n'apparaîtrait et l'exception ferait crasher le rendu → rouge. Un bUnit à pure doublure ne prouverait
/// ni l'échec via le canal HTTP réel ni le rendu du message.
/// </summary>
public sealed class FrontWasmConfigAjouterServiceInjoignableTempsReelTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    [Fact]
    public void Should_Afficher_un_message_d_echec_clair_et_conserver_la_saisie_Carla_rose_a_resoumettre_sans_enregistrer_aucun_acteur_When_un_parent_valide_l_ajout_alors_que_le_service_de_configuration_est_injoignable()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle. L'énumération initiale
        // (GET HTTP réel) transite normalement et peuple la liste ; SEUL le canal d'ajout est injoignable
        // (échec de transport déterministe sur le POST /api/canal/ajouter-acteur).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(
            GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "ajouter-acteur"));
        Services.AddSingleton(new SessionPlanning()); // contexte rôle réel (Parent par défaut) requis par l'écran (gating Sc.7)

        var config = RenderComponent<ConfigurationFoyer>();

        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // … baseline : la liste compte N acteurs et ne contient pas encore « Carla ».
        var nombreInitial = config.FindAll("[data-testid='acteur-foyer']").Count;
        Assert.DoesNotContain(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla");

        // … un parent ouvre la modal d'ajout (refonte s32) et saisit « Carla » (rose) dans le formulaire.
        ConfigActeursModalHarness.OuvrirAjout(this, config);
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom-ajout']").Change("Carla"));
        this.SurDispatcher(() => config.Find("[data-testid='pastille-couleur-ajout-rose']").Click()); // palette (Sc.6)

        // When — il valide l'ajout alors que le service de configuration est injoignable
        // (l'émission HTTP réelle se heurte à un échec de transport).
        this.SurDispatcher(() => config.Find("#form-ajout").Submit());

        // Then — un message d'échec clair s'affiche.
        var alerte = config.WaitForElement("[data-testid='motif-echec-ajout']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageInjoignable, alerte.TextContent.Trim());

        // … la saisie « Carla / rose » reste à l'écran à resoumettre (rien n'est effacé) : le nom est conservé
        // et la pastille « rose » du picker (Sc.6) demeure sélectionnée.
        Assert.Equal("Carla", config.Find("[data-testid='champ-nom-ajout']").GetAttribute("value"));
        Assert.Contains("selectionnee", config.Find("[data-testid='pastille-couleur-ajout-rose']").GetAttribute("class"));

        // … et aucun acteur n'est enregistré : la liste reste inchangée, sans « Carla » fantôme.
        Assert.Equal(nombreInitial, config.FindAll("[data-testid='acteur-foyer']").Count);
        Assert.DoesNotContain(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "Carla");
    }
}
