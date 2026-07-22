namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du Sc.4 — l'hôte d'API démarre en mode
/// <b>headless</b> (environnement « Testing » : aucun front déployé ni référencé, par
/// construction <see cref="ApiHoteFactory"/> ne charge jamais le projet front) et <b>sert quand
/// même</b> sa description OpenAPI (#1) et sa page d'exploration interactive (#2).
/// <para>
/// Driver propre : la <b>servabilité de la description en environnement nu</b>. Le pouvoir
/// discriminant tient à l'environnement non-développement de la fabrique : conditionner
/// <c>MapOpenApi</c>/<c>MapScalarApiReference</c> à <c>IsDevelopment</c> (comme l'ancien hôte Web)
/// les rendrait indisponibles ici → ces tests passent au rouge. Filet de non-régression headless ;
/// #2 est une caractérisation (l'exploration est déjà servie par le Sc.3, ici on confirme qu'elle
/// reste accessible sans front au démarrage).
/// </para>
/// </summary>
public sealed class HoteHeadlessDescriptionTests
{
    // Test #1 — driver headless : l'hôte d'API démarré seul dans un environnement sans front sert
    // son document de description OpenAPI du canal d'écriture.
    [Fact]
    public async Task Should_Servir_le_document_de_description_OpenAPI_du_canal_d_ecriture_When_l_hote_d_API_demarre_sans_front()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.GetAsync("/openapi/v1.json");

        Assert.True(reponse.IsSuccessStatusCode,
            $"l'hôte d'API headless doit servir son document de description OpenAPI, statut {(int)reponse.StatusCode}.");

        var document = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("/api/slots", document);
        Assert.Contains("/api/periodes", document);
    }

    // Test #2 (= acceptation, And) — caractérisation headless : la page d'exploration interactive
    // (Scalar) reste accessible alors qu'aucun front n'est déployé ni référencé.
    [Fact]
    public async Task Should_Rendre_la_page_d_exploration_interactive_accessible_When_l_hote_d_API_demarre_sans_front()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.GetAsync("/scalar/v1");

        Assert.True(reponse.IsSuccessStatusCode,
            $"la page d'exploration interactive doit rester accessible en environnement headless, statut {(int)reponse.StatusCode}.");
    }
}
