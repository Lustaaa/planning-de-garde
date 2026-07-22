using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 49 — Sc.1 — <b>FILET de non-régression sur Mongo DURABLE</b> (volet « identique InMemory
/// et Mongo durable »). Le chemin d'écriture que la sélection de plage (drag J1→J3) réemploie
/// STRICTEMENT — une période EST un intervalle <c>[début..fin]</c> (s06) — pose la surcharge sur
/// CHAQUE jour de l'intervalle et la résolution (surcharge &gt; fond) rend le responsable affecté sur
/// les trois jours, le fond reprenant hors intervalle, jusque sur le store Mongo réel relu.
///
/// <para>Deux acteurs réels créés (Alice/bleu, Bruno/orange) puis cycle de fond {0→Bruno, 1→Alice}
/// déclaré via le canal d'écriture : ISO 28 (06–12/07/2026) PAIRE → fond Bruno. Surcharge Alice
/// (bleu) sur [07..09/07] → chaque jour couvert résout bleu (surcharge prime) ; le 10/07 (même
/// semaine, non couvert) reste sur le fond Bruno (orange). <b>Skip propre</b> si Mongo indisponible.</para>
/// </summary>
public sealed class AffecterPeriodeIntervalleMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_affecter_intervalle_{Guid.NewGuid():N}";

    private sealed class HoteMongo(string connectionString, string database) : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Foyer:Persistance", "Mongo");
            builder.UseSetting("Foyer:Mongo:ConnectionString", connectionString);
            builder.UseSetting("Foyer:Mongo:Database", database);
        }
    }

    private HoteMongo NouveauServeur() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public async Task Should_poser_la_surcharge_sur_chaque_jour_de_l_intervalle_dans_la_grille_Mongo_relue_When_une_periode_est_affectee_sur_J1_a_J3()
    {
        var j1 = new DateTime(2026, 7, 7);  // Mardi   — ISO 28 paire
        var j3 = new DateTime(2026, 7, 9);  // Jeudi

        using var serveur = NouveauServeur();
        var client = serveur.CreateClient();

        // Given — deux acteurs réels du foyer (identifiants stables attribués par le store durable).
        var ajoutAlice = await client.PostAsJsonAsync("/api/foyer/acteurs", new { Nom = "Alice", Couleur = "bleu" });
        var ajoutBruno = await client.PostAsJsonAsync("/api/foyer/acteurs", new { Nom = "Bruno", Couleur = "orange" });
        Assert.True(ajoutAlice.IsSuccessStatusCode && ajoutBruno.IsSuccessStatusCode, "l'ajout des acteurs doit aboutir.");
        var acteurs = await client.GetFromJsonAsync<List<ActeurFoyerVue>>("/api/foyer/acteurs");
        var aliceId = acteurs!.Single(a => a.Nom == "Alice").Id;
        var brunoId = acteurs!.Single(a => a.Nom == "Bruno").Id;

        // And — cycle de fond de 2 semaines (fond Bruno sur la semaine ISO 28 paire, index 0).
        var cycle = await client.PutAsJsonAsync("/api/foyer/cycles", new
        {
            NombreSemaines = 2,
            Affectations = new Dictionary<int, string> { [0] = brunoId, [1] = aliceId },
        });
        Assert.True(cycle.IsSuccessStatusCode, $"la définition du cycle doit aboutir, statut {(int)cycle.StatusCode}.");

        // When — une période affectant Alice est écrite sur l'intervalle [07..09/07/2026].
        var periode = await client.PostAsJsonAsync("/api/periodes",
            new { ResponsableId = aliceId, Debut = j1, Fin = j3 });
        Assert.True(periode.IsSuccessStatusCode, $"l'affectation de la période doit aboutir, statut {(int)periode.StatusCode}.");

        // Then — la grille relue depuis Mongo durable rend Alice (bleu) sur J1, J2 et J3.
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/29");
        Assert.NotNull(grille);
        foreach (var jour in new[] { new DateOnly(2026, 7, 7), new DateOnly(2026, 7, 8), new DateOnly(2026, 7, 9) })
        {
            var caseCouverte = grille!.Jours.Single(j => j.Date == jour);
            Assert.Equal("bleu", caseCouverte.CouleurResponsable); // surcharge Alice prime sur le fond
            Assert.Equal("Alice", caseCouverte.NomResponsable);
        }

        // And — hors intervalle (10/07, même semaine, non couvert), le fond Bruno (orange) reprend.
        var caseFond = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 7, 10));
        Assert.Equal("orange", caseFond.CouleurResponsable);
        Assert.Equal("Bruno", caseFond.NomResponsable);
    }

    public void Dispose()
    {
        try
        {
            new MongoClient(ConnectionString).DropDatabase(_baseDeTest);
        }
        catch
        {
            // Best effort : si Mongo est injoignable au teardown, rien à nettoyer.
        }
    }
}
