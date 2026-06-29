using System.Net.Http;
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
/// supprimé ne doit plus être énuméré (ni en session, ni après redémarrage), tandis que les acteurs
/// témoins restent présents — preuve la plus forte que le retrait a bien atteint le store durable
/// (write-through, suppression ciblée), pas seulement un cache de session ni un vidage global.
///
/// <para><b>s15 (Sc.8) — aucun seed Mongo</b> : depuis le retrait du seed-once, Mongo ne s'amorce
/// plus jamais. Les acteurs témoins (« Parent A » et « Parent B », contrôle positif) sont donc
/// <b>AJOUTÉS explicitement</b> avant la suppression — ils doivent survivre à la suppression ciblée
/// de Nounou ET au redémarrage, sans appui sur un seed.</para>
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
    public async Task Acceptation_Should_Ne_plus_lister_Nounou_dans_le_store_relu_et_apres_redemarrage_tout_en_conservant_les_acteurs_temoins_When_un_parent_supprime_Nounou_par_son_identifiant_stable()
    {
        // --- Serveur #1 : deux acteurs témoins, puis Nounou (vert) ajoutée et supprimée par son id ---
        // (s15 : Mongo ne seede plus — les témoins « Parent A »/« Parent B » sont AJOUTÉS explicitement,
        // contrôle positif : la suppression est ciblée, jamais un vidage global du store.)
        string nounouId;
        string parentAId;
        string parentBId;
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();

            await AjouterActeur(c1, "Parent A", "bleu");
            await AjouterActeur(c1, "Parent B", "orange");

            var ajout = await c1.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Nounou", Couleur = "vert" });
            Assert.True(ajout.IsSuccessStatusCode, $"l'ajout de Nounou doit aboutir, statut {(int)ajout.StatusCode}.");

            // Les ids stables sont générés côté serveur : on les relit via l'énumération du store.
            var apresAjout = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            Assert.NotNull(apresAjout);
            nounouId = apresAjout.Single(a => a.Nom == "Nounou").Id;
            parentAId = apresAjout.Single(a => a.Nom == "Parent A").Id;
            parentBId = apresAjout.Single(a => a.Nom == "Parent B").Id;

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
        // mais les acteurs témoins (Parent A, Parent B) sont toujours présents.
        var acteurs = await c2.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        Assert.DoesNotContain(acteurs!, a => a.Id == nounouId);
        Assert.DoesNotContain(acteurs!, a => a.Nom == "Nounou");
        Assert.Contains(acteurs!, a => a.Id == parentAId);
        Assert.Contains(acteurs!, a => a.Id == parentBId);
    }

    /// <summary>Ajoute un acteur du foyer via le canal d'écriture et vérifie l'aboutissement.</summary>
    private static async Task AjouterActeur(HttpClient client, string nom, string couleur)
    {
        var reponse = await client.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = nom, Couleur = couleur });
        Assert.True(reponse.IsSuccessStatusCode, $"l'ajout de {nom} doit aboutir, statut {(int)reponse.StatusCode}.");
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
