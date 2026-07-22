using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 16 — Sc.5 — <b>Acceptation runtime sur Mongo RÉEL</b> de l'idempotence de la suppression
/// (rempart anti vert-qui-ment, R4). Le risque réel vit ICI, pas dans la doublure : un identifiant qui
/// n'est pas un <c>ObjectId</c> (« periode-inexistante ») lèverait à l'analyse sans la garde
/// <c>ObjectId.TryParse</c> ; supprimer une période <b>déjà supprimée</b> est un no-op qui réussit
/// (DeleteOne ne retire rien). Une doublure InMemory (RemoveAll) masquerait ces deux cas.
///
/// <para><b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// jamais un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.</para>
/// </summary>
public sealed class SupprimerPeriodeIdempotenteMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_suppr_idem_{Guid.NewGuid():N}";

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
    public async Task Should_reussir_sans_effet_et_sans_erreur_When_on_supprime_un_id_non_objectid_puis_une_periode_deja_supprimee()
    {
        using var serveur = new HoteMongo(ConnectionString, _baseDeTest);
        var client = serveur.CreateClient();
        var periodes = serveur.Services.GetRequiredService<IPeriodeRepository>();
        var handler = serveur.Services.GetRequiredService<SupprimerPeriodeHandler>();

        // Given — store durable avec deux périodes P1 et P2.
        await Affecter(client, "resp-p1", new DateTime(2026, 6, 10));
        await Affecter(client, "resp-p2", new DateTime(2026, 6, 20));
        var idP2 = periodes.AllSnapshots().Single(p => p.ResponsableId == "resp-p2").Id;

        // When/Then — supprimer un identifiant non-ObjectId réussit SANS lever et sans effet (P1 + P2 restent).
        var suppressionAbsente = handler.Handle(new SupprimerPeriodeCommand("periode-inexistante"));
        Assert.True(suppressionAbsente.EstSucces);
        Assert.Equal(2, periodes.AllSnapshots().Count);

        // When/Then — supprimer P2 puis la re-supprimer : les deux réussissent, la seconde est un no-op.
        Assert.True(handler.Handle(new SupprimerPeriodeCommand(idP2)).EstSucces);
        Assert.True(handler.Handle(new SupprimerPeriodeCommand(idP2)).EstSucces);

        var restantes = periodes.AllSnapshots();
        Assert.Single(restantes);
        Assert.Equal("resp-p1", restantes[0].ResponsableId);
    }

    private static async Task Affecter(HttpClient client, string responsableId, DateTime jour)
    {
        var reponse = await client.PostAsJsonAsync("/api/periodes",
            new { ResponsableId = responsableId, Debut = jour, Fin = jour });
        Assert.True(reponse.IsSuccessStatusCode, $"l'affectation de la période doit aboutir, statut {(int)reponse.StatusCode}.");
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
