using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Pivot de durabilité (s09, Sc.3) — acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker,
/// jamais une doublure : une doublure « mentirait au vert », R4). Le redémarrage du serveur est
/// matérialisé par une <b>nouvelle instance d'hôte API</b> (<see cref="ApiHoteFactory"/>) câblée sur
/// la <b>même base Mongo</b> persistée : un acteur ajouté (Carla) et une édition (Alice → Alicia)
/// doivent toujours être là, sans ressaisie, et la grille réellement câblée (canal de lecture HTTP,
/// comme le front WASM) doit nommer + colorer les cases — case comme légende.
///
/// <para><b>s15 (Sc.8) — aucun seed Mongo</b> : depuis le retrait du seed-once, Mongo ne s'amorce plus
/// jamais. Les acteurs de preuve sont donc <b>AJOUTÉS explicitement</b> avant l'assertion (Alice/bleu
/// renommée Alicia, Carla/rose) — la durabilité de l'<b>ajout</b> ET de l'<b>édition</b> reste prouvée
/// sur le store réel, sans appui sur un seed. Borne anti-cliquet (règle 30) : SEULE la config foyer
/// (acteurs) est durable ; les périodes restent InMemory (volatiles), donc ré-affectées après le
/// redémarrage — elles prouvent que le store durable nomme/colore les cases.</para>
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
        // --- Serveur #1 : Alice (bleu) est ajoutée puis renommée « Alicia » ; Carla (rose) est ajoutée ---
        // (s15 : Mongo ne seede plus — les acteurs de preuve sont AJOUTÉS explicitement avant l'assertion.)
        string aliciaId;
        string carlaId;
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();

            var ajoutAlice = await c1.PostAsJsonAsync("/api/foyer/acteurs", new { Nom = "Alice", Couleur = "bleu" });
            Assert.True(ajoutAlice.IsSuccessStatusCode, $"l'ajout d'Alice doit aboutir, statut {(int)ajoutAlice.StatusCode}.");

            // L'id stable est généré côté serveur : on le relit via l'énumération du store.
            var apresAlice = await c1.GetFromJsonAsync<List<ActeurFoyerVue>>("/api/foyer/acteurs");
            aliciaId = apresAlice!.Single(a => a.Nom == "Alice").Id;

            // Édition durable : le renommage Alice → Alicia doit survivre au redémarrage.
            var edition = await c1.PutAsJsonAsync($"/api/foyer/acteurs/{aliciaId}", new { Nom = "Alicia" });
            Assert.True(edition.IsSuccessStatusCode, $"le renommage Alice → Alicia doit aboutir, statut {(int)edition.StatusCode}.");

            var ajoutCarla = await c1.PostAsJsonAsync("/api/foyer/acteurs", new { Nom = "Carla", Couleur = "rose" });
            Assert.True(ajoutCarla.IsSuccessStatusCode, $"l'ajout de Carla doit aboutir, statut {(int)ajoutCarla.StatusCode}.");
            var apresCarla = await c1.GetFromJsonAsync<List<ActeurFoyerVue>>("/api/foyer/acteurs");
            carlaId = apresCarla!.Single(a => a.Nom == "Carla").Id;
        }

        // --- Le serveur est redémarré : NOUVELLE instance d'hôte sur la MÊME base Mongo persistée ---
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — l'écran de configuration liste toujours Alicia (édition) et Carla (ajout), sans ressaisie.
        var acteurs = await c2.GetFromJsonAsync<List<ActeurFoyerVue>>("/api/foyer/acteurs");
        Assert.Contains(acteurs!, a => a.Id == aliciaId && a.Nom == "Alicia");
        Assert.Contains(acteurs!, a => a.Id == carlaId && a.Nom == "Carla");

        // Les périodes sont InMemory (volatiles, règle 30) : ré-affectées sur le serveur redémarré.
        // Les ids ont survécu → ré-affecter dessus résout nom + couleur depuis le store durable.
        var pAlicia = await c2.PostAsJsonAsync("/api/periodes",
            new { ResponsableId = aliciaId, Debut = new DateTime(2026, 6, 1), Fin = new DateTime(2026, 6, 5) });
        Assert.True(pAlicia.IsSuccessStatusCode, $"affectation Alicia 1-5 juin, statut {(int)pAlicia.StatusCode}.");
        var pCarla = await c2.PostAsJsonAsync("/api/periodes",
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
