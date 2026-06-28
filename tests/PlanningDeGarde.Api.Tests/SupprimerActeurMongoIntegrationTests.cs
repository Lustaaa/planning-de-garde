using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 13 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). Un parent supprime « Nounou » via
/// <c>POST /api/canal/supprimer-acteur</c> ; le redémarrage du serveur est matérialisé par une
/// <b>nouvelle instance d'hôte API</b> câblée sur la <b>même base Mongo</b> persistée : l'acteur
/// supprimé ne doit plus être énuméré (ni en session, ni après redémarrage), tandis que « Parent A »
/// et « Parent B » (seeds) restent présents — preuve la plus forte que le retrait a bien atteint le
/// store durable (write-through), pas seulement un cache de session.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// plutôt qu'un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class SupprimerActeurMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    /// <summary>Hôte API détaché câblé sur le store Mongo durable, base isolée par test.</summary>
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
    public async Task Acceptation_Should_Ne_plus_lister_Nounou_dans_le_store_relu_et_apres_redemarrage_tout_en_conservant_Parent_A_et_Parent_B_When_un_parent_supprime_Nounou_par_son_identifiant_stable()
    {
        // --- Serveur #1 : Nounou est ajoutée (vert) puis supprimée par son identifiant stable ---
        string nounouId;
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();

            var ajout = await c1.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Nounou", Couleur = "vert" });
            Assert.True(ajout.IsSuccessStatusCode, $"l'ajout de Nounou doit aboutir, statut {(int)ajout.StatusCode}.");

            // L'id stable de Nounou est généré côté serveur : on le relit via l'énumération du store.
            var apresAjout = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            nounouId = apresAjout!.Single(a => a.Nom == "Nounou").Id;

            var suppression = await c1.PostAsJsonAsync("/api/canal/supprimer-acteur", new { ActeurId = nounouId });
            Assert.True(suppression.IsSuccessStatusCode, $"la suppression de Nounou doit aboutir, statut {(int)suppression.StatusCode}.");

            // En session : Nounou a déjà quitté le store relu.
            var apresSuppression = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            Assert.DoesNotContain(apresSuppression!, a => a.Id == nounouId);
        }

        // --- Le serveur est redémarré : NOUVELLE instance d'hôte sur la MÊME base Mongo persistée ---
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — après redémarrage, le store durable relu ne comporte toujours pas Nounou,
        // mais Parent A (Alice) et Parent B (Bruno), seeds, sont toujours présents.
        var acteurs = await c2.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        Assert.DoesNotContain(acteurs!, a => a.Id == nounouId);
        Assert.DoesNotContain(acteurs!, a => a.Nom == "Nounou");
        Assert.Contains(acteurs!, a => a.Id == "parent-a");
        Assert.Contains(acteurs!, a => a.Id == "parent-b");
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
