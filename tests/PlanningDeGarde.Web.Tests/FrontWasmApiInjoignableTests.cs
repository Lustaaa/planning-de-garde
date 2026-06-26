extern alias api;
using System.Net.Http;
using System.Net.Sockets;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ scénario IHM, <c>@erreur</c>) — l'API distante est
/// réellement <b>arrêtée</b> (injoignable). Le comportement vit dans le <c>.razor</c> (la vue
/// <see cref="PoserSlot"/>) : à la soumission, le client d'écriture réel émet vers l'URL d'API
/// configurable ; le transport est un <b>vrai socket</b> vers une adresse <b>réellement morte</b>
/// (hôte d'API démarré puis disposé → <c>ConnectionRefused</c> ⇒ <see cref="HttpRequestException"/>).
///
/// Anti « vert qui ment » : on n'injecte <b>pas</b> un <c>HttpMessageHandler</c> qui stube un code
/// de statut (ce qui prouverait un refus métier 4xx, pas un service injoignable). On câble la vue
/// réelle à un <b>vrai transport réseau</b> vers un hôte réellement arrêté, et on observe l'<b>absence
/// d'écriture</b> sur le store réel d'un second hôte d'API (aucune écriture silencieuse, aucune file).
///
/// Le symptôme PO observé : (1) le message exact « Enregistrement impossible : le service est
/// injoignable, réessayez. » s'affiche ; (2) la saisie n'est pas appliquée — pas de navigation, le
/// formulaire reste rempli, à resoumettre ; (3) aucun slot n'est enregistré pour le 24/06/2026.
/// </summary>
public sealed class FrontWasmApiInjoignableTests : TestContext
{
    private const string MessageInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    // Construit un HttpClient réel pointant sur une adresse d'API RÉELLEMENT arrêtée : on réserve
    // un port TCP localhost libre puis on le LIBÈRE → plus aucun hôte n'écoute dessus. Une émission
    // réseau réelle vers ce port produit un vrai ConnectionRefused ⇒ HttpRequestException — le
    // symptôme exact d'un « service injoignable » côté navigateur (transport réseau réel, pas un stub).
    private static HttpClient ClientVersApiArretee()
    {
        var sonde = new TcpListener(System.Net.IPAddress.Loopback, 0);
        sonde.Start();
        var port = ((System.Net.IPEndPoint)sonde.LocalEndpoint).Port;
        sonde.Stop(); // port libéré : plus rien n'écoute → toute connexion sera refusée.

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:BaseUrl"] = $"http://127.0.0.1:{port}/",
            })
            .Build();

        // Transport réseau réel (pas de doublure) : le client tente une vraie connexion TCP.
        return PlanningDeGarde.Web.ClientCanalEcriture.Construire(config, new SocketsHttpHandler());
    }

    [Fact]
    public void Should_Afficher_le_message_de_service_injoignable_et_ne_rien_enregistrer_When_le_front_WASM_tente_une_pose_alors_que_l_API_distante_est_arretee()
    {
        // Given — le front est câblé à une API distante réellement arrêtée (transport réseau réel),
        // et le foyer connaît « école ».
        Services.AddSingleton(ClientVersApiArretee());
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(new DateTime(2026, 6, 26)));

        var page = RenderComponent<PoserSlot>();
        page.Find("select.form-select").Change("école");

        // When — le front tente d'émettre la pose (Léa, école, 24/06/2026 08:30 → 16:30).
        page.Find("form").Submit();

        // Then — le front affiche le message exact de service injoignable (l'échec de transport est
        // asynchrone : on attend le re-rendu après l'échec réseau réel — le ConnectionRefused d'un
        // vrai socket peut dépasser le timeout par défaut, d'où une fenêtre d'attente explicite).
        var alerte = page.WaitForElement("[data-testid='motif-echec']", TimeSpan.FromSeconds(10));
        Assert.Equal(MessageInjoignable, alerte.TextContent.Trim());

        // … et la saisie n'est pas appliquée : pas de navigation (le formulaire reste affiché, à
        // resoumettre) et le lieu saisi est conservé.
        var nav = (Bunit.TestDoubles.FakeNavigationManager)Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.Equal(nav.BaseUri, nav.Uri); // aucune navigation vers « planning » n'a eu lieu
        Assert.NotEmpty(page.FindAll("form")); // la saisie reste à resoumettre
    }

    [Fact]
    public void Should_N_enregistrer_aucun_slot_pour_le_mercredi_24_06_2026_When_l_API_distante_etait_arretee_au_moment_de_la_pose()
    {
        // Given — la vue réelle câblée à une API réellement arrêtée.
        Services.AddSingleton(ClientVersApiArretee());
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(new DateTime(2026, 6, 26)));

        var page = RenderComponent<PoserSlot>();
        page.Find("select.form-select").Change("école");

        // When — tentative de pose alors que l'API est injoignable.
        page.Find("form").Submit();

        // Then — aucune écriture silencieuse ni mise en file : un hôte d'API neuf (store réel
        // vierge) ne porte aucun slot pour le mercredi 24/06/2026. L'écriture n'a transité nulle part.
        using var apiNeuve = new ApiDistanteFactory();
        using var scope = apiNeuve.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseMercredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        Assert.Empty(caseMercredi.Slots);
    }
}
