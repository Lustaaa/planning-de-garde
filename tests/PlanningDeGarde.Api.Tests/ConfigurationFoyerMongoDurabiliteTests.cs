using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Pivot de durabilité (Sc.3) — acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker,
/// jamais une doublure : une doublure « mentirait au vert », R4). Le redémarrage du serveur est
/// matérialisé par une <b>nouvelle instance d'hôte API</b> (<see cref="ApiHoteFactory"/>) câblée sur
/// la <b>même base Mongo</b> persistée : l'acteur ajouté (Carla) et l'édition (Alice → Alicia)
/// doivent toujours être là, sans ressaisie, et la grille réellement câblée (canal de lecture HTTP,
/// comme le front WASM) doit nommer + colorer les cases — case comme légende.
///
/// La logique <b>seed-once</b> (seed si la base est vide, sinon relire l'état persisté SANS
/// re-seeder par-dessus les éditions) est la principale surface de bug — c'est l'inversion exacte
/// de la volatilité assumée jusqu'ici (re-seed au démarrage). Borne anti-cliquet (règle 30) :
/// SEULE la config foyer (acteurs) est durable ; les périodes restent InMemory (volatiles), donc
/// ré-affectées après le redémarrage — elles prouvent que le store durable nomme/colore les cases.
///
/// <b>Skip propre</b> (Assert.Skip) si Docker / Mongo est indisponible, plutôt qu'un faux vert.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class ConfigurationFoyerMongoDurabiliteTests : IDisposable
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
    public async Task Should_Lister_toujours_Alicia_et_Carla_et_afficher_leurs_cases_nommees_et_colorees_en_case_comme_en_legende_apres_un_redemarrage_du_serveur_sans_ressaisie_When_l_etat_a_ete_persiste_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : Carla est ajoutée (rose) et Alice est renommée « Alicia » ---
        string carlaId;
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();

            var ajout = await c1.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Carla", Couleur = "rose" });
            Assert.True(ajout.IsSuccessStatusCode, $"l'ajout de Carla doit aboutir, statut {(int)ajout.StatusCode}.");

            // L'id stable de Carla est généré côté serveur : on le relit via l'énumération du store.
            var apresAjout = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            carlaId = apresAjout!.Single(a => a.Nom == "Carla").Id;

            var edition = await c1.PostAsJsonAsync("/api/canal/editer-acteur", new { ActeurId = "parent-a", Nom = "Alicia" });
            Assert.True(edition.IsSuccessStatusCode, $"le renommage Alice → Alicia doit aboutir, statut {(int)edition.StatusCode}.");
        }

        // --- Le serveur est redémarré : NOUVELLE instance d'hôte sur la MÊME base Mongo persistée ---
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — l'écran de configuration liste toujours Alicia et Carla, sans ressaisie (Observable 1).
        var acteurs = await c2.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        Assert.Contains(acteurs!, a => a.Id == "parent-a" && a.Nom == "Alicia");
        Assert.Contains(acteurs!, a => a.Id == carlaId && a.Nom == "Carla");

        // Les périodes sont InMemory (volatiles, règle 30) : ré-affectées sur le serveur redémarré.
        // L'id de Carla a survécu → ré-affecter sur cet id résout « Carla » / rose depuis le store durable.
        var pAlicia = await c2.PostAsJsonAsync("/api/canal/affecter-periode",
            new { ResponsableId = "parent-a", Debut = new DateTime(2026, 6, 1), Fin = new DateTime(2026, 6, 5) });
        Assert.True(pAlicia.IsSuccessStatusCode, $"affectation Alicia 1-5 juin, statut {(int)pAlicia.StatusCode}.");
        var pCarla = await c2.PostAsJsonAsync("/api/canal/affecter-periode",
            new { ResponsableId = carlaId, Debut = new DateTime(2026, 6, 8), Fin = new DateTime(2026, 6, 12) });
        Assert.True(pCarla.IsSuccessStatusCode, $"affectation Carla 8-12 juin, statut {(int)pCarla.StatusCode}.");

        // Then — la grille relue via le canal de LECTURE HTTP (comme le front WASM) : case comme légende.
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/1");
        Assert.NotNull(grille);

        // Observable 2a : les cases du 1er au 5 juin affichent « Alicia » en bleu.
        var casesAlicia = grille!.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 1) && j.Date <= new DateOnly(2026, 6, 5))
            .ToList();
        Assert.Equal(5, casesAlicia.Count);
        Assert.All(casesAlicia, j =>
        {
            Assert.Equal("Alicia", j.NomResponsable);
            Assert.Equal("bleu", j.CouleurResponsable);
        });

        // Observable 2b : les cases du 8 au 12 juin affichent « Carla » en rose.
        var casesCarla = grille.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 8) && j.Date <= new DateOnly(2026, 6, 12))
            .ToList();
        Assert.Equal(5, casesCarla.Count);
        Assert.All(casesCarla, j =>
        {
            Assert.Equal("Carla", j.NomResponsable);
            Assert.Equal("rose", j.CouleurResponsable);
        });

        // Case COMME légende : la légende porte les mêmes nom + couleur durables.
        Assert.Contains(grille.Légende, e => e.Nom == "Alicia" && e.Couleur == "bleu");
        Assert.Contains(grille.Légende, e => e.Nom == "Carla" && e.Couleur == "rose");
    }

    /// <summary>
    /// Skip propre plutôt qu'un faux vert : tente un ping Mongo borné dans le temps. Renvoie
    /// <c>true</c> (et le motif) si Docker / Mongo est injoignable.
    /// </summary>
    internal static bool MongoIndisponible(out string motif)
    {
        try
        {
            var parametres = MongoClientSettings.FromConnectionString(ConnectionString);
            parametres.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            var client = new MongoClient(parametres);
            client.GetDatabase("admin").RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
            motif = string.Empty;
            return false;
        }
        catch (Exception ex)
        {
            motif = $"Mongo indisponible (Docker non démarré ?) — scénario de durabilité ignoré : {ex.Message}";
            return true;
        }
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

/// <summary>
/// Fait conditionné à la disponibilité de Mongo (Docker) : pose <see cref="FactAttribute.Skip"/>
/// à la découverte si le store réel est injoignable — <b>skip propre</b>, jamais un faux vert
/// (xunit 2.9.3 n'expose pas de skip dynamique à l'exécution).
/// </summary>
public sealed class MongoRequisFactAttribute : FactAttribute
{
    public MongoRequisFactAttribute()
    {
        if (ConfigurationFoyerMongoDurabiliteTests.MongoIndisponible(out var motif))
            Skip = motif;
    }
}
