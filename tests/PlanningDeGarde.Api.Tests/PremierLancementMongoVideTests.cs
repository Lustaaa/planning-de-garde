using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 15 — Sc.8 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (Docker, jamais une doublure :
/// une doublure « mentirait au vert », R4). Au <b>tout premier lancement</b> sur une base vierge,
/// l'application ouvre <b>totalement vide</b> : aucun acteur listé en configuration du foyer, aucune
/// case de grille porteuse d'un slot ni d'un responsable (aucune période, aucun fond car aucun cycle),
/// légende vide.
///
/// Drive le <b>retrait du seed runtime</b> : l'amorçage de démo (<c>AmorcerDonneesDemo</c>) ET le
/// <b>seed-once</b> des acteurs côté <c>ConfigurationFoyerMongo</c> sont supprimés. <b>Asymétrie seed
/// assumée</b> : Mongo ne seede <b>jamais</b> (vide → durable) ; l'InMemory <b>garde</b> son seed pour
/// que la suite de non-régression reste verte.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// jamais un faux vert. Base Mongo isolée par exécution (Guid), jamais peuplée, supprimée en fin de test.
/// </summary>
public sealed class PremierLancementMongoVideTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_vide_{Guid.NewGuid():N}";

    /// <summary>Hôte API détaché câblé sur un store Mongo durable, base vierge isolée par test.</summary>
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

    [MongoRequisFact]
    public async Task Should_Ouvrir_une_application_totalement_vide_acteurs_grille_cycle_When_le_store_Mongo_est_vierge_et_le_seed_runtime_est_retire()
    {
        // Given — un store Mongo vierge (jamais peuplé), l'application démarre en persistance « Mongo ».
        using var serveur = new HoteMongo(ConnectionString, _baseDeTest);
        var client = serveur.CreateClient();

        // Then — aucun acteur n'est listé dans la configuration du foyer (seed-once Mongo retiré).
        var acteurs = await client.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        Assert.NotNull(acteurs);
        Assert.Empty(acteurs!);

        // Then — la grille du jour (mercredi 10/06/2026) n'affiche aucun slot, aucune période ni aucun
        // fond : faute de cycle ET de période, chaque case retombe au neutre (aucun responsable nommé).
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/10");
        Assert.NotNull(grille);
        Assert.NotEmpty(grille!.Jours); // la fenêtre est bien projetée — mais toutes ses cases sont vides
        Assert.All(grille.Jours, j =>
        {
            Assert.Equal("", j.NomResponsable); // aucune période, aucun fond → pas de responsable
            Assert.Empty(j.Slots);              // aucun slot posé
        });

        // Then — aucun cycle de fond défini ET aucune période → légende vide (aucun présent dans la fenêtre).
        Assert.Empty(grille.Légende);
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
