using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 17 — Sc.1 — <b>Acceptation runtime sur Mongo RÉEL</b> (rempart anti vert-qui-ment, R4 :
/// jamais une doublure comme seule preuve). Une période persistée puis <b>re-bornée</b> via le câblage réel
/// (handler résolu de la DI → <see cref="MongoPeriodeRepository"/>) doit figurer dans le store relu avec
/// ses <b>nouvelles</b> bornes — y compris après <b>redémarrage</b> de l'hôte (nouvelle instance sur la
/// <b>même</b> base Mongo).
///
/// <para><b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// jamais un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.</para>
/// </summary>
public sealed class EditerPeriodeMongoIntegrationTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_editer_periode_{Guid.NewGuid():N}";

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
    public async Task Should_mettre_a_jour_les_bornes_dans_le_store_Mongo_relu_meme_apres_redemarrage_When_on_re_borne_par_son_identifiant_stable()
    {
        var lundi15 = new DateTime(2026, 6, 15);
        var mardi16 = new DateTime(2026, 6, 16);
        var mercredi17 = new DateTime(2026, 6, 17);
        string idStable;

        // Given — store vierge ; "Nounou" créée, période durable du lundi 15 → mercredi 17 affectée à Nounou.
        using (var serveur1 = NouveauServeur())
        {
            var c1 = serveur1.CreateClient();
            var ajout = await c1.PostAsJsonAsync("/api/canal/ajouter-acteur", new { Nom = "Nounou", Couleur = "vert" });
            Assert.True(ajout.IsSuccessStatusCode, $"l'ajout de Nounou doit aboutir, statut {(int)ajout.StatusCode}.");
            var acteurs = await c1.GetFromJsonAsync<List<CanalLecture.ActeurFoyerVue>>("/api/foyer/acteurs");
            var nounouId = acteurs!.Single(a => a.Nom == "Nounou").Id;

            var periode = await c1.PostAsJsonAsync("/api/canal/affecter-periode",
                new { ResponsableId = nounouId, Debut = lundi15, Fin = mercredi17 });
            Assert.True(periode.IsSuccessStatusCode, $"l'affectation de la période doit aboutir, statut {(int)periode.StatusCode}.");

            // L'état observé (identifiant stable inclus) = celui attribué par le store durable, relu via le port réel.
            var etatObserve = Assert.Single(serveur1.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
            idStable = etatObserve.Id;
            Assert.False(string.IsNullOrEmpty(idStable), "la période persistée doit porter un identifiant stable.");

            // When — je re-borne la période (mardi 16 → mercredi 17), via le handler câblé sur Mongo réel.
            var resultat = serveur1.Services.GetRequiredService<EditerPeriodeHandler>()
                .Handle(new EditerPeriodeCommand(etatObserve, etatObserve.ResponsableId, mardi16, mercredi17));

            // Then — l'édition réussit ; le store relu (même hôte) porte les nouvelles bornes, même identifiant.
            Assert.True(resultat.EstSucces);
            var relueMemeHote = Assert.Single(serveur1.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
            Assert.Equal(idStable, relueMemeHote.Id);
            Assert.Equal(mardi16, relueMemeHote.Debut);
            Assert.Equal(mercredi17, relueMemeHote.Fin);
        }

        // And — après redémarrage (nouvelle instance, même base Mongo) les nouvelles bornes persistent.
        using var serveur2 = NouveauServeur();
        var relueApresRedemarrage = Assert.Single(
            serveur2.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        Assert.Equal(idStable, relueApresRedemarrage.Id);
        Assert.Equal(mardi16, relueApresRedemarrage.Debut);
        Assert.Equal(mercredi17, relueApresRedemarrage.Fin);

        // And — le lundi 15 juin 2026 n'est plus couvert : la grille relue ne nomme plus "Nounou" ce jour-là.
        var c2 = serveur2.CreateClient();
        var grille = await c2.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/15");
        Assert.NotNull(grille);
        var case15 = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 15));
        Assert.NotEqual("Nounou", case15.NomResponsable);
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
