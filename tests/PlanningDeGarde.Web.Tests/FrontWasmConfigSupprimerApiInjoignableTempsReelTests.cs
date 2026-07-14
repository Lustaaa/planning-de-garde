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
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ IHM, <c>@erreur</c>) — CARACTÉRISATION early-green : depuis
/// l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>, DI réelle, API
/// distante réelle <see cref="ApiDistanteFactory"/>, store réel), un Parent clique le bouton supprimer de
/// grand-père alors que le <b>canal d'écriture de suppression est injoignable</b> (échec de <b>transport</b>,
/// <see cref="System.Net.Http.HttpRequestException"/> — PAS un refus métier 4xx) : le handler
/// <c>SupprimerActeur</c> ne s'exécute jamais. L'écran doit surfacer un <b>message d'échec clair</b>
/// (« Enregistrement impossible : le service est injoignable, réessayez. »), grand-père reste <b>présent</b>
/// dans la liste (aucune <b>fausse confirmation</b> « Acteur supprimé »), et <b>aucune écriture ne transite</b>
/// (le store réel énumère toujours grand-père) — sans mise en file ni rejeu (règle 28).
///
/// Caractérisation (early green ATTENDU) : l'issue d'échec transport (<c>catch (HttpRequestException)</c> →
/// <c>MessagesEcriture.ServiceInjoignable</c>, surface distincte de l'accusé succès) est posée au Sc.6, sur le
/// patron acquis s09 (ajout / édition / cycle injoignables). Ce test ne force aucun code neuf ; il prouve le
/// même contrat sur le geste de suppression.
///
/// Anti « vert qui ment » : l'écran est câblé à l'<b>API distante réelle</b> ; SEUL le POST de suppression subit
/// un échec de transport déterministe (handler qui lève <see cref="System.Net.Http.HttpRequestException"/>,
/// indépendant du proxy loopback Docker — patron s09). L'observable cardinal « rien n'est supprimé » est vérifié
/// sur le <b>store réel</b> de l'API (énumération inchangée), pas par une doublure. Sans le catch, le clic
/// planterait sans message ou la liste muterait à tort → rouge.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigSupprimerApiInjoignableTempsReelTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    [Fact]
    public void Should_Afficher_un_message_d_echec_clair_garder_grand_pere_dans_la_liste_sans_fausse_confirmation_ni_suppression_When_un_parent_clique_supprimer_alors_que_l_API_distante_est_injoignable()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle. L'énumération initiale
        // (GET HTTP réel) transite normalement et peuple la liste ; SEUL le canal de suppression est injoignable
        // (échec de transport déterministe sur le POST /api/canal/supprimer-acteur).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(
            GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable(api, "supprimer-acteur"));
        Services.AddSingleton(new SessionPlanning()); // contexte rôle réel (Parent par défaut) — le bouton est rendu

        var config = RenderComponent<ConfigurationFoyer>();

        // … la liste se peuple depuis le store réel ; grand-père y figure (baseline).
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        var nombreInitial = config.FindAll("[data-testid='acteur-foyer']").Count;
        Assert.Contains(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "grand-père");

        // When — je clique le bouton supprimer de grand-père alors que le canal de suppression est injoignable
        // (l'émission HTTP réelle se heurte à une HttpRequestException).
        // Refonte s32 : la suppression se fait dans la MODAL ouverte au crayon de grand-père (qui reste
        // ouverte sur échec de transport, le motif surfacé à part).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "grand-pere");
        this.SurDispatcher(() => config.Find("[data-testid='bouton-supprimer']").Click());

        // Then — un message d'échec clair s'affiche (surface distincte de l'accusé succès).
        var alerte = config.WaitForElement("[data-testid='motif-echec-suppression']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageInjoignable, alerte.TextContent.Trim());

        // … grand-père reste présent dans la liste, qui est inchangée (même nombre d'acteurs).
        Assert.Equal(nombreInitial, config.FindAll("[data-testid='acteur-foyer']").Count);
        Assert.Contains(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => li.QuerySelector(".acteur-nom")!.TextContent.Trim() == "grand-père");

        // … aucune fausse confirmation « Acteur supprimé » n'est affichée.
        Assert.Empty(config.FindAll("[data-testid='accuse-suppression']"));

        // … et observable cardinal : aucune écriture n'a transité — le STORE RÉEL de l'API énumère toujours
        // grand-père (rien n'a été supprimé ni mis en file).
        var acteurs = api.Services.GetRequiredService<IEnumerationActeursFoyer>().EnumererActeurs();
        Assert.Contains("grand-pere", acteurs);
    }
}
