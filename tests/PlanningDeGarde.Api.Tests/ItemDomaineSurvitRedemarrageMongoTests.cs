using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 15 — Sc.9 — <b>Boucle externe</b> (Scenario Outline, une ligne par item) pilotant la pose des
/// <b>4 adaptateurs de droite Mongo</b> (slots / périodes / transferts / cycle de fond), derrière les
/// ports existants. Acceptation d'<b>intégration sur Mongo RÉEL</b> (Docker, jamais une doublure : une
/// doublure « mentirait au vert », R4).
///
/// <para>Chaque item saisi en mode Mongo (sur store vierge) doit <b>survivre au redémarrage</b> de
/// l'hôte d'API — matérialisé par une <b>nouvelle instance d'hôte</b> sur la <b>même base Mongo</b>
/// (façon <see cref="ConfigurationFoyerMongoDurabiliteTests"/>) — et être re-projeté dans la grille.
/// Tant que l'item vit dans un dépôt InMemory volatil, il est perdu au redémarrage (RED) ; l'adaptateur
/// Mongo write-through le rend durable (GREEN).</para>
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible, jamais
/// un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class ItemDomaineSurvitRedemarrageMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_item_{Guid.NewGuid():N}";

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

    /// <summary>Ajoute l'acteur « Alice » au store durable et renvoie son identifiant stable (généré
    /// côté serveur, relu via l'énumération du store).</summary>
    private static async Task<string> AjouterAlice(HttpClient client)
    {
        var ajout = await client.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Alice", Couleur = "bleu" });
        Assert.True(ajout.IsSuccessStatusCode, $"l'ajout d'Alice doit aboutir, statut {(int)ajout.StatusCode}.");
        var acteurs = await client.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        return acteurs!.Single(a => a.Nom == "Alice").Id;
    }

    private static async Task<bool> AliceEstListee(HttpClient client)
    {
        var acteurs = await client.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        return acteurs!.Any(a => a.Nom == "Alice");
    }

    // --- Item 1 : un slot (enfant → lieu, date) — driver MongoSlotRepository -----------------------

    [MongoRequisFact]
    public async Task Should_Conserver_un_slot_enregistre_enfant_lieu_date_apres_le_redemarrage_de_l_hote_When_les_slots_sont_persistes_sur_le_store_Mongo_reel()
    {
        // Given — store vierge ; on enregistre un slot (Léa à l'école le mercredi 10/06/2026).
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            // Mongo config foyer sans seed (s27/s30) : le lieu « école » ET l'enfant « Léa » n'existent pas
            // par défaut, on les établit explicitement — parité avec l'établissement des acteurs (AjouterAlice)
            // déjà pratiqué ici. La pose valide désormais l'existence de l'enfant (s30 S7), plus un fantôme.
            serveur1.Services.GetRequiredService<IEditeurLieux>().Ajouter("école", "école");
            serveur1.Services.GetRequiredService<IEditeurEnfants>().Ajouter("Léa", "Léa");
            var pose = await c1.PostAsJsonAsync("/api/canal/poser-slot",
                new { EnfantId = "Léa", LieuId = "école", Debut = new DateTime(2026, 6, 10, 8, 0, 0), Fin = new DateTime(2026, 6, 10, 17, 0, 0) });
            Assert.True(pose.IsSuccessStatusCode, $"la pose du slot doit aboutir, statut {(int)pose.StatusCode}.");
        }

        // When — l'hôte d'API redémarre : NOUVELLE instance sur la MÊME base Mongo.
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — le slot est toujours projeté dans la grille (case du 10/06, créneau « école »).
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/10");
        Assert.NotNull(grille);
        var case10 = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 10));
        Assert.Contains(case10.Slots, s => s.Libelle == "école");
    }

    // --- Item 2 : une période affectée à Alice sur 2 jours — driver MongoPeriodeRepository ----------

    [MongoRequisFact]
    public async Task Should_Conserver_une_periode_affectee_a_Alice_sur_2_jours_apres_le_redemarrage_de_l_hote_When_les_periodes_sont_persistees_sur_le_store_Mongo_reel()
    {
        string aliceId;
        // Given — store vierge ; Alice créée, période affectée à Alice du 10 au 11/06/2026.
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            aliceId = await AjouterAlice(c1);
            var periode = await c1.PostAsJsonAsync("/api/canal/affecter-periode",
                new { ResponsableId = aliceId, Debut = new DateTime(2026, 6, 10), Fin = new DateTime(2026, 6, 11) });
            Assert.True(periode.IsSuccessStatusCode, $"l'affectation de la période doit aboutir, statut {(int)periode.StatusCode}.");
        }

        // When — redémarrage de l'hôte sur la même base Mongo.
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — la période est toujours projetée : les cases du 10 ET du 11/06 nomment « Alice ».
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/10");
        Assert.NotNull(grille);
        var couvertes = grille!.Jours.Where(j => j.Date == new DateOnly(2026, 6, 10) || j.Date == new DateOnly(2026, 6, 11)).ToList();
        Assert.Equal(2, couvertes.Count);
        Assert.All(couvertes, j => Assert.Equal("Alice", j.NomResponsable));

        // And — l'acteur « Alice » est toujours présent (config foyer durable).
        Assert.True(await AliceEstListee(c2), "Alice doit rester listée après le redémarrage.");
    }

    // --- Item 3 : un transfert — driver MongoTransfertRepository -----------------------------------

    [MongoRequisFact]
    public async Task Should_Conserver_un_transfert_depositaire_recuperateur_lieu_date_heure_apres_le_redemarrage_de_l_hote_When_les_transferts_sont_persistes_sur_le_store_Mongo_reel()
    {
        // Given — store vierge ; un transfert de bascule (Alice → Bruno, école, 8h30 le 10/06/2026).
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            var transfert = await c1.PostAsJsonAsync("/api/canal/definir-transfert",
                new { DeposeParId = "parent-a", RecupereParId = "parent-b", LieuId = "école", Heure = TimeSpan.FromHours(8.5), Date = new DateTime(2026, 6, 10) });
            Assert.True(transfert.IsSuccessStatusCode, $"la définition du transfert doit aboutir, statut {(int)transfert.StatusCode}.");
        }

        // When — redémarrage de l'hôte sur la même base Mongo.
        using var serveur2 = NouveauServeur();

        // Then — le transfert est toujours présent dans le store durable, relu par l'adaptateur réel
        // (le read model grille ne projette pas les transferts : on observe le port réel sur Mongo).
        var transferts = serveur2.Services.GetRequiredService<ITransfertRepository>().AllSnapshots();
        Assert.Contains(transferts, t =>
            t.DeposeParId == "parent-a" && t.RecupereParId == "parent-b" && t.LieuId == "école"
            && t.Heure == TimeSpan.FromHours(8.5) && DateOnly.FromDateTime(t.Date) == new DateOnly(2026, 6, 10));
    }

    // --- Item 4 : le cycle de fond de 2 semaines — driver CycleDeFondMongo -------------------------

    [MongoRequisFact]
    public async Task Should_Conserver_le_cycle_de_fond_de_2_semaines_apres_le_redemarrage_de_l_hote_When_le_cycle_est_persiste_sur_le_store_Mongo_reel()
    {
        string aliceId;
        // Given — store vierge ; Alice créée, cycle de fond N=2 mappé « index 0 → Alice ».
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            aliceId = await AjouterAlice(c1);
            var cycle = await c1.PostAsJsonAsync("/api/canal/definir-cycle",
                new { NombreSemaines = 2, Affectations = new Dictionary<int, string> { [0] = aliceId } });
            Assert.True(cycle.IsSuccessStatusCode, $"la définition du cycle doit aboutir, statut {(int)cycle.StatusCode}.");
        }

        // When — redémarrage de l'hôte sur la même base Mongo.
        using var serveur2 = NouveauServeur();
        var c2 = serveur2.CreateClient();

        // Then — le cycle de fond est toujours résolu : la légende de la grille nomme « Alice » comme
        // responsable de fond (présent en case comme en légende), preuve que le cycle a survécu.
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/10");
        Assert.NotNull(grille);
        Assert.Contains(grille!.Légende, e => e.Nom == "Alice");
        Assert.Contains(grille.Jours, j => j.NomResponsable == "Alice");

        // And — l'acteur « Alice » est toujours présent (config foyer durable).
        Assert.True(await AliceEstListee(c2), "Alice doit rester listée après le redémarrage.");
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
