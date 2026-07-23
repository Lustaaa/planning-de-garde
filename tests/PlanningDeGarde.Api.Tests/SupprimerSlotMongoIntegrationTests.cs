using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 18 — Sc.1 — <b>Acceptation runtime sur Mongo RÉEL</b> (rempart anti vert-qui-ment, R4 :
/// jamais une doublure comme seule preuve). Un slot posé puis supprimé via le câblage réel (handler
/// résolu de la DI → <see cref="MongoSlotRepository"/>) ne doit plus figurer dans le store relu —
/// y compris après <b>redémarrage</b> de l'hôte (nouvelle instance sur la <b>même</b> base Mongo).
///
/// <para><b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// jamais un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.</para>
/// </summary>
public sealed class SupprimerSlotMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_suppr_slot_{Guid.NewGuid():N}";

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
    public async Task Should_retirer_le_slot_du_store_Mongo_relu_meme_apres_redemarrage_When_on_le_supprime_par_son_identifiant_stable()
    {
        var debut = new DateTime(2026, 6, 16, 8, 30, 0);
        var fin = new DateTime(2026, 6, 16, 16, 30, 0);
        string idStable;

        // Given — store vierge ; slot durable plaçant "Léa" à "école" le mardi 16/06/2026 08h30–16h30.
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            // Mongo config foyer sans seed (s27/s30) : on établit le lieu « école » ET l'enfant « Léa »
            // avant la pose (la pose valide désormais l'existence de l'enfant, s30 S7).
            serveur1.Services.GetRequiredService<IEditeurActivites>().Ajouter("école", "école");
            serveur1.Services.GetRequiredService<IEditeurEnfants>().Ajouter("Léa", "Léa");
            var pose = await c1.PostAsJsonAsync("/api/enfants/Léa/activites",
                new { LieuId = "école", Debut = debut, Fin = fin });
            Assert.True(pose.IsSuccessStatusCode, $"la pose du slot doit aboutir, statut {(int)pose.StatusCode}.");

            // L'identifiant stable = celui attribué par le store durable, relu via le port réel.
            var stockes = serveur1.Services.GetRequiredService<ISlotRepository>().AllSnapshots();
            idStable = Assert.Single(stockes).Id;
            Assert.False(string.IsNullOrEmpty(idStable), "le slot persisté doit porter un identifiant stable.");

            // When — je supprime le slot par son identifiant stable, via le handler câblé sur Mongo réel.
            var resultat = serveur1.Services.GetRequiredService<SupprimerSlotHandler>()
                .Handle(new SupprimerSlotCommand(idStable));

            // Then — la suppression réussit ; le slot a disparu du store relu (même hôte).
            Assert.True(resultat.EstSucces);
            Assert.Empty(serveur1.Services.GetRequiredService<ISlotRepository>().AllSnapshots());
        }

        // And — après redémarrage (nouvelle instance, même base Mongo) le slot est toujours absent.
        using var serveur2 = NouveauServeur();
        Assert.DoesNotContain(
            serveur2.Services.GetRequiredService<ISlotRepository>().AllSnapshots(),
            s => s.Id == idStable);

        // And — la grille relue ne montre plus de slot "école" sur la case du mardi 16/06/2026.
        var c2 = serveur2.CreateClient();
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/16");
        Assert.NotNull(grille);
        var case16 = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 16));
        Assert.DoesNotContain(case16.Slots, s => s.Libelle == "école");
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
