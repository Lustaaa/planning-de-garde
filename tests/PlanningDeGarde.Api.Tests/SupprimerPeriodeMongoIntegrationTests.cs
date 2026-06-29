using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 16 — Sc.1 — <b>Acceptation runtime sur Mongo RÉEL</b> (rempart anti vert-qui-ment, R4 :
/// jamais une doublure comme seule preuve). Une période persistée puis supprimée via le câblage réel
/// (handler résolu de la DI → <see cref="MongoPeriodeRepository"/>) ne doit plus figurer dans le store
/// relu — y compris après <b>redémarrage</b> de l'hôte (nouvelle instance sur la <b>même</b> base Mongo).
///
/// <para><b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// jamais un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.</para>
/// </summary>
public sealed class SupprimerPeriodeMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_suppr_periode_{Guid.NewGuid():N}";

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
    public async Task Should_retirer_la_periode_du_store_Mongo_relu_meme_apres_redemarrage_When_on_la_supprime_par_son_identifiant_stable()
    {
        var mardi16 = new DateTime(2026, 6, 16);
        string idStable;

        // Given — store vierge ; "Nounou" créée, période durable du mardi 16/06/2026 affectée à Nounou.
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            var ajout = await c1.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Nounou", Couleur = "vert" });
            Assert.True(ajout.IsSuccessStatusCode, $"l'ajout de Nounou doit aboutir, statut {(int)ajout.StatusCode}.");
            var acteurs = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            var nounouId = acteurs!.Single(a => a.Nom == "Nounou").Id;

            var periode = await c1.PostAsJsonAsync("/api/canal/affecter-periode",
                new { ResponsableId = nounouId, Debut = mardi16, Fin = mardi16 });
            Assert.True(periode.IsSuccessStatusCode, $"l'affectation de la période doit aboutir, statut {(int)periode.StatusCode}.");

            // L'identifiant stable = celui attribué par le store durable, relu via le port réel.
            var stockees = serveur1.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots();
            idStable = Assert.Single(stockees).Id;
            Assert.False(string.IsNullOrEmpty(idStable), "la période persistée doit porter un identifiant stable.");

            // When — je supprime la période par son identifiant stable, via le handler câblé sur Mongo réel.
            var resultat = serveur1.Services.GetRequiredService<SupprimerPeriodeHandler>()
                .Handle(new SupprimerPeriodeCommand(idStable));

            // Then — la suppression réussit ; la période a disparu du store relu (même hôte).
            Assert.True(resultat.EstSucces);
            Assert.Empty(serveur1.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        }

        // And — après redémarrage (nouvelle instance, même base Mongo) la période est toujours absente.
        using var serveur2 = NouveauServeur();
        Assert.DoesNotContain(
            serveur2.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots(),
            p => p.Id == idStable);

        // And — la grille relue ne nomme plus "Nounou" sur la case du mardi 16/06/2026.
        var c2 = serveur2.CreateClient();
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/16");
        Assert.NotNull(grille);
        var case16 = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 16));
        Assert.NotEqual("Nounou", case16.NomResponsable);
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
