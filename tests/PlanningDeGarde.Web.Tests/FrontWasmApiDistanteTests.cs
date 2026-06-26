extern alias api;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PoserSlotRequete = api::PlanningDeGarde.Api.CanalEcriture.PoserSlotRequete;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ scénario IHM) — le front WASM consomme l'API
/// <b>distante</b>. Deux hôtes réels câblés : l'hôte d'API détaché réel (<see cref="ApiDistanteFactory"/>,
/// store réel singleton, projection réelle <see cref="GrilleAgendaQuery"/>) joue l'<b>API distante</b> ;
/// le <b>client d'écriture réel du front WASM</b> (construit par <see cref="ClientCanalEcriture.Construire"/>
/// depuis une <b>URL d'API configurable</b> — clé « Api:BaseUrl », et non <c>nav.BaseUri</c>) émet sa pose
/// vers cette adresse distante. L'observable est le slot réellement enregistré dans le store de l'API
/// distante, lu par sa projection : l'écriture a réellement transité par HTTP distant.
///
/// Anti « vert qui ment » : si le client du front pointe sur son propre hôte (et non l'URL d'API
/// configurable) ou si l'écriture ne transite pas jusqu'au store distant, l'observable distant reste
/// vide → rouge. Un bUnit composant à doublures ne verrait jamais ce câblage distant.
/// </summary>
public sealed class FrontWasmApiDistanteTests
{
    // Mercredi 24/06/2026, école, 08:30 → 16:30 (pose de Léa émise par le front).
    private static readonly PoserSlotRequete PoseLea =
        new("Léa", "école", new DateTime(2026, 6, 24, 8, 30, 0), new DateTime(2026, 6, 24, 16, 30, 0));

    [Fact]
    public async Task Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_When_le_front_WASM_pose_un_slot_via_l_API_distante()
    {
        // Given — l'hôte d'API détaché réel démarré seul joue l'API distante.
        using var apiDistante = new ApiDistanteFactory();
        var adresseApiDistante = apiDistante.Server.BaseAddress; // l'URL de l'API distante in-test
        var transportVersApiDistante = apiDistante.Server.CreateHandler();

        // … et le front WASM est configuré pour émettre ses écritures vers cette API distante
        // (URL d'API configurable, clé « Api:BaseUrl » — PAS nav.BaseUri).
        var configFront = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:BaseUrl"] = adresseApiDistante.ToString(),
            })
            .Build();

        // Le client d'écriture réel du front, construit comme en WASM : BaseAddress = URL d'API
        // distante configurable. On lui greffe le transport réel vers l'hôte d'API in-test.
        var clientFront = ClientCanalEcriture.Construire(configFront, transportVersApiDistante);

        // When — le front émet, vers l'API distante, une pose de slot pour Léa.
        var reponse = await clientFront.PostAsJsonAsync("api/canal/poser-slot", PoseLea);

        // Then — l'API distante confirme l'effet par un succès.
        Assert.True(reponse.IsSuccessStatusCode,
            $"l'API distante doit confirmer la pose, statut {(int)reponse.StatusCode}.");

        // … et le slot est réellement enregistré dans le store de l'API DISTANTE (pas un accusé du
        // canal ni une grille statique) : observé via la projection réelle de l'hôte d'API.
        using var scope = apiDistante.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseMercredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        var slot = Assert.Single(caseMercredi.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slot.Debut);
        Assert.Equal(new TimeOnly(16, 30), slot.Fin);
    }

    [Fact]
    public void Should_Cibler_l_URL_d_API_distante_configurable_When_le_client_d_ecriture_du_front_WASM_est_construit()
    {
        // L'invariant runtime du Sc.2 : le client du front cible l'URL d'API CONFIGURABLE
        // (Api:BaseUrl), et non son propre hôte. Sans cette config, le front parlait à nav.BaseUri.
        const string urlApiDistante = "https://api.planning.local/";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = urlApiDistante })
            .Build();

        var client = ClientCanalEcriture.Construire(config);

        Assert.Equal(new Uri(urlApiDistante), client.BaseAddress);
    }
}
