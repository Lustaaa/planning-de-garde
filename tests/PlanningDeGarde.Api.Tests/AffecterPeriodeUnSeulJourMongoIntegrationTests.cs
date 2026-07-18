using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 49 — Sc.2 — <b>FILET de non-régression sur Mongo DURABLE</b> (volet « identique InMemory et
/// Mongo durable »). La sélection d'UN seul jour (drag sans déplacement → Sc.4) retombe sur le
/// comportement PONCTUEL connu : affecter une période sur l'intervalle <c>[J..J]</c> écrit EXACTEMENT
/// le jour J (surcharge résolue ce jour-là), J-1 et J+1 restant sur le fond, sans écriture doublonnée
/// (un SEUL enregistrement dans le store Mongo relu — last-write-wins R11).
///
/// <para>Deux acteurs réels (Alice/bleu, Bruno/orange) + cycle {0→Bruno, 1→Alice} : ISO 28
/// (06–12/07/2026) PAIRE → fond Bruno. Surcharge Alice sur le SEUL 08/07. <b>Skip propre</b> si Mongo
/// indisponible ; base isolée par exécution (Guid), supprimée en fin de test.</para>
/// </summary>
public sealed class AffecterPeriodeUnSeulJourMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_affecter_un_jour_{Guid.NewGuid():N}";

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
    public async Task Should_ecrire_exactement_le_jour_J_sans_doublon_dans_la_grille_Mongo_relue_When_une_periode_est_affectee_sur_l_intervalle_J_a_J()
    {
        var j = new DateTime(2026, 7, 8); // Mercredi — ISO 28 paire, jour unique surchargé

        using var serveur = NouveauServeur();
        var client = serveur.CreateClient();

        // Given — deux acteurs réels du foyer (identifiants stables attribués par le store durable).
        var ajoutAlice = await client.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Alice", Couleur = "bleu" });
        var ajoutBruno = await client.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Bruno", Couleur = "orange" });
        Assert.True(ajoutAlice.IsSuccessStatusCode && ajoutBruno.IsSuccessStatusCode, "l'ajout des acteurs doit aboutir.");
        var acteurs = await client.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
        var aliceId = acteurs!.Single(a => a.Nom == "Alice").Id;
        var brunoId = acteurs!.Single(a => a.Nom == "Bruno").Id;

        // And — cycle de fond de 2 semaines (fond Bruno sur la semaine ISO 28 paire, index 0).
        var cycle = await client.PostAsJsonAsync("/api/canal/definir-cycle", new
        {
            NombreSemaines = 2,
            Affectations = new Dictionary<int, string> { [0] = brunoId, [1] = aliceId },
        });
        Assert.True(cycle.IsSuccessStatusCode, $"la définition du cycle doit aboutir, statut {(int)cycle.StatusCode}.");

        // When — une période affectant Alice est écrite sur l'intervalle d'un seul jour [08..08/07].
        var periode = await client.PostAsJsonAsync("/api/canal/affecter-periode",
            new { ResponsableId = aliceId, Debut = j, Fin = j });
        Assert.True(periode.IsSuccessStatusCode, $"l'affectation de la période doit aboutir, statut {(int)periode.StatusCode}.");

        // Then — aucune écriture doublonnée : un SEUL enregistrement de période dans le store Mongo relu.
        var enregistrees = serveur.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots();
        var enregistree = Assert.Single(enregistrees);
        Assert.Equal(aliceId, enregistree.ResponsableId);
        Assert.Equal(j, enregistree.Debut);
        Assert.Equal(j, enregistree.Fin);

        // And — la grille relue depuis Mongo durable rend Alice (bleu) le SEUL 08/07 ; J-1 et J+1 restent au fond Bruno (orange).
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/29");
        Assert.NotNull(grille);
        var caseJ = grille!.Jours.Single(x => x.Date == new DateOnly(2026, 7, 8));
        Assert.Equal("bleu", caseJ.CouleurResponsable);
        Assert.Equal("Alice", caseJ.NomResponsable);
        foreach (var voisin in new[] { new DateOnly(2026, 7, 7), new DateOnly(2026, 7, 9) })
        {
            var caseFond = grille!.Jours.Single(x => x.Date == voisin);
            Assert.Equal("orange", caseFond.CouleurResponsable);
            Assert.Equal("Bruno", caseFond.NomResponsable);
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
