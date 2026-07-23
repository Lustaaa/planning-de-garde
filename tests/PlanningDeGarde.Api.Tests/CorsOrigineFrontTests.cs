using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du Sc.5 — l'hôte d'API détaché <b>autorise</b>, via
/// son CORS, l'origine du front WASM exécuté depuis une origine distincte. La fabrique configure
/// l'origine autorisée du front (clé « Front:Origine » = <c>https://app.planning.local</c>,
/// origine de config, non figée) ; les requêtes du canal d'écriture portent l'en-tête
/// <c>Origin</c> du front comme le ferait le navigateur en cross-origin.
/// <para>
/// Driver CORS (#1) : ASP.NET Core n'émet l'en-tête <c>Access-Control-Allow-Origin</c> que si la
/// requête porte un <c>Origin</c> couvert par la politique. Le pouvoir discriminant tient à
/// l'existence de cette politique : sans <c>AddCors</c>/<c>UseCors</c> autorisant l'origine du
/// front, l'en-tête d'autorisation cross-origin n'apparaît pas → rouge.
/// </para>
/// <para>
/// Driver de bout en bout (#2, anti early-green) : une autorisation CORS qui n'aboutirait pas à
/// l'écriture passe #1 mais échoue ici. Confirme que la pose cross-origin transite réellement
/// jusqu'au store réel lu par la projection (caractérisation du chemin sous contrainte CORS).
/// </para>
/// </summary>
public sealed class CorsOrigineFrontTests
{
    private const string OrigineFront = "https://app.planning.local";

    // Mercredi 24/06/2026, école, 08:30 → 16:30 (pose de Léa émise par le front cross-origin).
    private static readonly object PoseLea = new
    {
        LieuId = "école",
        Debut = new DateTime(2026, 6, 24, 8, 30, 0),
        Fin = new DateTime(2026, 6, 24, 16, 30, 0),
    };

    /// <summary>
    /// Hôte d'API réel dont le CORS autorise l'origine du front (config « Front:Origine »).
    /// Reprend l'environnement « Testing » de <see cref="ApiHoteFactory"/> (store vierge).
    /// </summary>
    private sealed class ApiCorsFrontFactory : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Front:Origine"] = OrigineFront,
                }));
        }
    }

    // Test #1 — driver CORS : une requête cross-origin du front (en-tête Origin) atteignant le
    // canal d'écriture obtient l'en-tête d'autorisation cross-origin de l'API.
    [Fact]
    public async Task Should_Autoriser_l_origine_du_front_When_une_requete_cross_origin_du_front_atteint_le_canal_d_ecriture_de_l_API()
    {
        using var hote = new ApiCorsFrontFactory();
        var client = hote.CreateClient();

        var requete = new HttpRequestMessage(HttpMethod.Post, "/api/enfants/Léa/activites")
        {
            Content = JsonContent.Create(PoseLea),
        };
        // Émise comme par le navigateur en cross-origin : porte l'Origin du front.
        requete.Headers.Add("Origin", OrigineFront);

        var reponse = await client.SendAsync(requete);

        Assert.True(reponse.Headers.TryGetValues("Access-Control-Allow-Origin", out var origines),
            "la réponse de l'API doit porter l'en-tête CORS autorisant l'origine du front.");
        Assert.Contains(OrigineFront, origines!);
    }

    // Test #2 — driver de bout en bout (anti early-green) : la pose cross-origin (en-tête Origin du
    // front) transite réellement jusqu'au store réel singleton de l'hôte API, observé via la
    // projection réelle. Caractérisation : la pose « slot dans la case » est déjà verte ailleurs ;
    // ici on confirme qu'elle aboutit sous contrainte cross-origin (filet de non-régression du
    // chemin CORS → écriture).
    [Fact]
    public async Task Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_dans_la_projection_reelle_When_la_pose_cross_origin_du_front_a_abouti_via_l_API()
    {
        using var hote = new ApiCorsFrontFactory();
        var client = hote.CreateClient();

        var requete = new HttpRequestMessage(HttpMethod.Post, "/api/enfants/Léa/activites")
        {
            Content = JsonContent.Create(PoseLea),
        };
        requete.Headers.Add("Origin", OrigineFront);

        var reponse = await client.SendAsync(requete);
        Assert.True(reponse.IsSuccessStatusCode,
            $"la pose cross-origin doit aboutir, statut {(int)reponse.StatusCode}.");

        // Observable de bout en bout : le store réel singleton de l'hôte API, lu par la projection
        // réelle. Date de référence injectée = lundi 22/06/2026.
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseMercredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        var slot = Assert.Single(caseMercredi.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slot.Debut);
        Assert.Equal(new TimeOnly(16, 30), slot.Fin);
    }
}
