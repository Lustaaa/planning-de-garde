using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ IHM, <c>@erreur</c>) — VOLET 100 % RUNTIME (backend néant) :
/// depuis l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent a
/// saisi un cycle de fond de 2 semaines (index 0 → <c>parent-a</c>, index 1 → <c>parent-b</c>) dans le
/// formulaire de définition du cycle et valide alors que le <b>service de configuration est injoignable</b>.
/// L'échec est de <b>transport</b> (le canal <c>POST /api/canal/definir-cycle</c> n'aboutit pas :
/// <see cref="System.Net.Http.HttpRequestException"/>) — PAS un refus métier 4xx (Sc.7, N &lt; 1) : le handler
/// <c>DefinirCycle</c> ne s'exécute jamais. L'écran doit surfacer un <b>message d'échec clair</b>
/// (« Enregistrement impossible : le service est injoignable, réessayez. »), <b>conserver</b> la saisie du
/// cycle (N = 2 + mapping) à resoumettre, et <b>n'enregistrer aucun cycle</b> (le store cycle reste vierge).
///
/// Anti « vert qui ment » : l'écran est câblé à l'<b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>) ;
/// l'énumération initiale (GET acteurs) transite réellement ; SEUL le POST de définition du cycle subit un échec
/// de transport déterministe (handler qui lève <see cref="System.Net.Http.HttpRequestException"/>, indépendant du
/// proxy loopback Docker — patron s09 Sc.9). L'observable cardinal « aucun cycle enregistré » est vérifié sur le
/// <b>store réel</b> de l'API (<see cref="IReferentielCycleDeFond.CycleCourant"/> reste <c>null</c>), pas par une
/// doublure. Sans le <c>catch (HttpRequestException)</c> côté écran, la validation planterait sans message → rouge.
/// </summary>
public sealed class FrontWasmConfigCycleServiceInjoignableTempsReelTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    [Fact]
    public void Should_Afficher_un_message_d_echec_clair_et_conserver_la_saisie_du_cycle_a_resoumettre_sans_rien_enregistrer_When_le_service_de_configuration_est_injoignable_a_la_validation_du_cycle()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle. L'énumération initiale
        // (GET HTTP réel) transite normalement ; SEUL le canal de définition du cycle est injoignable
        // (échec de transport déterministe sur le POST /api/canal/definir-cycle).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(
            GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "definir-cycle"));

        var config = RenderComponent<ConfigurationFoyer>();

        // … baseline : aucun cycle n'est encore défini dans le store réel de l'API.
        var storeCycle = api.Services.GetRequiredService<IReferentielCycleDeFond>();
        Assert.Null(storeCycle.CycleCourant());

        // … le formulaire de cycle est présent et propose deux index de semaine (N = 2 par défaut).
        config.WaitForElement("[data-testid='champ-cycle-index-1']", TimeSpan.FromSeconds(10));

        // … un parent saisit un cycle de 2 semaines : index 0 → parent-a, index 1 → parent-b.
        config.Find("[data-testid='champ-nombre-semaines']").Change("2");
        config.Find("[data-testid='champ-cycle-index-0']").Change("parent-a");
        config.Find("[data-testid='champ-cycle-index-1']").Change("parent-b");

        // When — il valide la définition du cycle alors que le service de configuration est injoignable
        // (l'émission HTTP réelle se heurte à un échec de transport).
        config.Find("#form-cycle").Submit();

        // Then — un message d'échec clair s'affiche.
        var alerte = config.WaitForElement("[data-testid='motif-echec-cycle']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageInjoignable, alerte.TextContent.Trim());

        // … la saisie du cycle reste à l'écran à resoumettre (N = 2 + mapping conservés, rien n'est effacé).
        Assert.Equal("2", config.Find("[data-testid='champ-nombre-semaines']").GetAttribute("value"));
        Assert.Equal("parent-a", config.Find("[data-testid='champ-cycle-index-0']").GetAttribute("value"));
        Assert.Equal("parent-b", config.Find("[data-testid='champ-cycle-index-1']").GetAttribute("value"));

        // … et aucun cycle n'est enregistré : le store réel reste vierge (aucune mise en file ni rejeu, règle 28).
        Assert.Null(storeCycle.CycleCourant());
    }
}
