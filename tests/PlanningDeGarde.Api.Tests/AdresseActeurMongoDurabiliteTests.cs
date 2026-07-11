using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 33 — Sc.1 (frontière Application + durabilité) — acceptation runtime sur Mongo RÉEL
/// (conteneur Docker, jamais une doublure : R4). Le champ de modèle neuf « adresse de résidence »
/// est édité via le canal d'écriture HTTP, PERSISTÉ, et doit survivre à un redémarrage du serveur
/// (nouvelle instance d'hôte sur la MÊME base) sans ressaisie, puis être RELU tel quel par la query
/// de configuration (<c>/api/foyer/acteurs</c>). L'identifiant stable est inchangé (édition, pas
/// recréation) et une adresse VIDE est acceptée sans écraser les autres champs.
///
/// <b>Skip propre</b> (MongoRequisFact) si Docker / Mongo est indisponible. Base isolée par test (Guid).
/// </summary>
public sealed class AdresseActeurMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

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
    public async Task Should_Relire_l_adresse_persistee_apres_un_redemarrage_sans_ressaisie_et_sans_toucher_les_autres_champs_When_l_adresse_a_ete_editee_via_le_canal_HTTP_sur_le_store_Mongo_reel()
    {
        const string adresse = "12 rue des Lilas, 69000 Lyon";
        string aliceId;

        // --- Serveur #1 : Alice (bleu) ajoutée, puis on lui édite une adresse de résidence ---
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();

            var ajout = await c1.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Alice", Couleur = "bleu" });
            Assert.True(ajout.IsSuccessStatusCode, $"l'ajout d'Alice doit aboutir, statut {(int)ajout.StatusCode}.");

            var apresAjout = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            aliceId = apresAjout!.Single(a => a.Nom == "Alice").Id;
            Assert.Null(apresAjout.Single(a => a.Id == aliceId).Adresse); // aucune adresse au départ (optionnelle)

            var edition = await c1.PostAsJsonAsync("/api/canal/editer-acteur", new { ActeurId = aliceId, Adresse = adresse });
            Assert.True(edition.IsSuccessStatusCode, $"l'édition de l'adresse doit aboutir, statut {(int)edition.StatusCode}.");
        }

        // --- Redémarrage : NOUVELLE instance d'hôte sur la MÊME base Mongo persistée ---
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — l'adresse est relue telle quelle, l'id stable inchangé, nom + couleur intacts (pas de recréation).
        var acteurs = await c2.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        var alice = acteurs!.Single(a => a.Id == aliceId);
        Assert.Equal(adresse, alice.Adresse);
        Assert.Equal("Alice", alice.Nom);
        Assert.Equal("bleu", alice.Couleur);

        // Et une adresse VIDE est acceptée (champ optionnel) sans écriture partielle des autres champs.
        var vidage = await c2.PostAsJsonAsync("/api/canal/editer-acteur", new { ActeurId = aliceId, Adresse = "" });
        Assert.True(vidage.IsSuccessStatusCode, $"vider l'adresse doit aboutir, statut {(int)vidage.StatusCode}.");

        var apresVidage = await c2.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        var aliceVidee = apresVidage!.Single(a => a.Id == aliceId);
        Assert.Equal(string.Empty, aliceVidee.Adresse); // adresse vide relue telle quelle
        Assert.Equal("Alice", aliceVidee.Nom);          // nom intact (aucune écriture partielle)
        Assert.Equal("bleu", aliceVidee.Couleur);       // couleur intacte
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
