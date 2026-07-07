using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 18 — Sc.5 — <b>Acceptation runtime sur Mongo RÉEL</b> de l'idempotence de la suppression de slot
/// (rempart anti vert-qui-ment, R4). Le risque réel vit ICI, pas dans la doublure : un identifiant qui
/// n'est pas un <c>ObjectId</c> (« slot-inexistant ») lèverait à l'analyse sans la garde
/// <c>ObjectId.TryParse</c> ; supprimer un slot <b>déjà supprimé</b> est un no-op qui réussit (DeleteOne
/// ne retire rien). Une doublure InMemory (RemoveAll) masquerait ces deux cas.
///
/// <para><b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// jamais un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.</para>
/// </summary>
public sealed class SupprimerSlotIdempotenteMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_suppr_slot_idem_{Guid.NewGuid():N}";

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
    public async Task Should_reussir_sans_effet_et_sans_erreur_When_on_supprime_un_id_non_objectid_puis_un_slot_deja_supprime()
    {
        using var serveur = new HoteMongo(ConnectionString, _baseDeTest);
        var client = serveur.CreateClient();
        var slots = serveur.Services.GetRequiredService<ISlotRepository>();
        var handler = serveur.Services.GetRequiredService<SupprimerSlotHandler>();
        // Mongo config foyer sans seed (s27) : on établit les lieux « école » et « nounou » avant les poses.
        var lieux = serveur.Services.GetRequiredService<IEditeurLieux>();
        lieux.Ajouter("école", "école");
        lieux.Ajouter("nounou", "nounou");

        // Given — store durable avec deux slots S1 (école) et S2 (nounou) pour Léa.
        await Poser(client, "école", new DateTime(2026, 6, 10, 8, 0, 0), new DateTime(2026, 6, 10, 12, 0, 0));
        await Poser(client, "nounou", new DateTime(2026, 6, 20, 8, 0, 0), new DateTime(2026, 6, 20, 12, 0, 0));
        var idS2 = slots.AllSnapshots().Single(s => s.LieuId == "nounou").Id;

        // When/Then — supprimer un identifiant non-ObjectId réussit SANS lever et sans effet (S1 + S2 restent).
        var suppressionAbsente = handler.Handle(new SupprimerSlotCommand("slot-inexistant"));
        Assert.True(suppressionAbsente.EstSucces);
        Assert.Equal(2, slots.AllSnapshots().Count);

        // When/Then — supprimer S2 puis le re-supprimer : les deux réussissent, la seconde est un no-op.
        Assert.True(handler.Handle(new SupprimerSlotCommand(idS2)).EstSucces);
        Assert.True(handler.Handle(new SupprimerSlotCommand(idS2)).EstSucces);

        var restants = slots.AllSnapshots();
        Assert.Single(restants);
        Assert.Equal("école", restants[0].LieuId);
    }

    private static async Task Poser(HttpClient client, string lieuId, DateTime debut, DateTime fin)
    {
        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot",
            new { EnfantId = "Léa", LieuId = lieuId, Debut = debut, Fin = fin });
        Assert.True(reponse.IsSuccessStatusCode, $"la pose du slot doit aboutir, statut {(int)reponse.StatusCode}.");
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
