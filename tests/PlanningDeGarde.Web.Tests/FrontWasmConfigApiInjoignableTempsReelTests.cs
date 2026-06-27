using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.9 (🖥️ IHM, <c>@erreur</c>) — VOLET RUNTIME (backend néant) :
/// la grille réelle affiche parent-a (« Alice », bleu) le 14/07/2026, mais l'<b>API distante est
/// injoignable</b> (échec de <b>transport</b>, pas un refus métier comme Sc.8). Depuis l'<b>écran de
/// configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>) connecté à une API <b>réellement
/// arrêtée</b>, on renomme parent-a en « Alicia » et on enregistre : l'émission via le canal d'écriture
/// HTTP réel échoue (<see cref="HttpRequestException"/> sur un vrai <c>ConnectionRefused</c>), l'écran
/// surface un <b>message clair</b> (« Enregistrement impossible : le service est injoignable, réessayez. »),
/// l'édition <b>n'est pas appliquée</b> et reste à resoumettre — la grille (sur l'API live) conserve
/// « Alice » dans la case du 14/07 et en légende ; sans mise en file ni rejeu.
///
/// Anti « vert qui ment » : le transport est un <b>vrai socket</b> vers un port réellement libéré (pas un
/// stub de statut 4xx — ce serait un refus métier, pas un service injoignable). Le baseline « Alice » est
/// asserté avant ; la grille reste « Alice » car aucune écriture n'a transité (store live non muté). Un
/// bUnit à doublure ne prouverait ni l'échec de transport réseau réel, ni le rendu du message.
/// </summary>
public sealed class FrontWasmConfigApiInjoignableTempsReelTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    // HttpClient réel pointant sur une adresse d'API RÉELLEMENT arrêtée : on réserve un port TCP
    // localhost libre puis on le LIBÈRE → plus rien n'écoute. Toute émission réseau réelle vers ce port
    // produit un vrai ConnectionRefused ⇒ HttpRequestException — le symptôme exact d'un service
    // injoignable côté navigateur (transport réseau réel, pas une doublure de statut).
    private static HttpClient ClientVersApiArretee()
    {
        var sonde = new TcpListener(IPAddress.Loopback, 0);
        sonde.Start();
        var port = ((IPEndPoint)sonde.LocalEndpoint).Port;
        sonde.Stop(); // port libéré : plus rien n'écoute → toute connexion sera refusée.

        return new HttpClient(new SocketsHttpHandler())
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
        };
    }

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

        // When — depuis l'écran de configuration câblé à une API distante RÉELLEMENT ARRÊTÉE, je renomme
        // parent-a en « Alicia » et j'enregistre (l'émission HTTP réelle se heurte à un ConnectionRefused).
        using var ecranConfig = new TestContext();
        ecranConfig.Services.AddSingleton(ClientVersApiArretee());
        var config = ecranConfig.RenderComponent<ConfigurationFoyer>();
        config.Find("select.form-select").Change("parent-a");
        config.Find("[data-testid='champ-nom']").Change("Alicia");
        config.Find("form").Submit();

        // Then — l'enregistrement échoue clairement : le message de service injoignable s'affiche (l'échec
        // de transport est asynchrone : un vrai ConnectionRefused peut dépasser le timeout par défaut,
        // d'où une fenêtre d'attente explicite).
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
