namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du Sc.3 — l'UI d'exploration interactive de l'API
/// liste les endpoints du canal d'écriture. Hôte d'API détaché réel (<see cref="ApiHoteFactory"/>) :
/// aucune doublure. L'UI d'exploration (Scalar) est un confort d'outillage sans observable métier,
/// pilotée par sa <b>servabilité HTTP</b> : la route d'exploration répond (#1) et le document
/// OpenAPI qu'elle référence <b>liste</b> les endpoints du canal d'écriture (#2). Première
/// exposition de ce document sur l'hôte API détaché (caractérisation du portage du sprint 04).
/// </summary>
public sealed class ExplorationApiTests
{
    // Test #1 — driver de servabilité : la route d'exploration interactive (Scalar) est servie
    // par l'hôte d'API détaché et répond en succès.
    [Fact]
    public async Task Should_Servir_la_page_d_exploration_interactive_When_on_ouvre_la_route_d_exploration_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.GetAsync("/scalar/v1");

        Assert.True(reponse.IsSuccessStatusCode,
            $"la route d'exploration de l'hôte d'API doit répondre en succès, statut {(int)reponse.StatusCode}.");
    }

    // Test #2 (= acceptation) — driver de complétude (anti early-green) : le document OpenAPI que
    // l'exploration référence liste les endpoints du canal d'écriture (« poser un slot »,
    // « affecter une période »). Une UI servie mais branchée sur un document vide passerait #1 et
    // échouerait ici.
    [Fact]
    public async Task Should_Lister_les_endpoints_poser_un_slot_et_affecter_une_periode_dans_la_description_servie_par_l_hote_d_API_When_on_recupere_le_document_OpenAPI_de_l_exploration()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.GetAsync("/openapi/v1.json");
        Assert.True(reponse.IsSuccessStatusCode,
            $"le document OpenAPI référencé par l'exploration doit être servi, statut {(int)reponse.StatusCode}.");

        var document = await reponse.Content.ReadAsStringAsync();

        Assert.Contains("/api/slots", document);
        Assert.Contains("/api/periodes", document);
    }
}
