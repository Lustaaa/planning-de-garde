using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — Rework G3 — Amorçage d'un compte de DÉMONSTRATION sur Mongo RÉEL (Docker). Quand le flag
/// <c>Demo:SeedCompteDemo</c> est actif (amorçage de dev explicite, JAMAIS en environnement de test par
/// défaut → la parité « aucun seed Mongo » s15 reste intacte), l'hôte d'API amorce à son démarrage un
/// compte <c>deveaux.cyril@gmail.com</c> : acteur créé, compte lié + ACTIVÉ, mot de passe <c>Toto123@</c>
/// posé par le CHEMIN RÉEL (DefinirMotDePasseHandler → PBKDF2, jamais un hash en dur). Résultat : la
/// connexion « email + mot de passe » réussit IMMÉDIATEMENT sur le store durable, et échoue sur un mauvais
/// mot de passe (motif neutre). Preuve runtime réelle (aucune doublure). Base Mongo isolée par exécution.
/// </summary>
public sealed class SeedCompteDemoMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string EmailDemo = "deveaux.cyril@gmail.com";
    private const string MotDePasseDemo = "Toto123@";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    /// <summary>Hôte API sur Mongo durable AVEC l'amorçage de démo activé (flag explicite).</summary>
    private sealed class HoteDemo(string connectionString, string database) : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Foyer:Persistance", "Mongo");
            builder.UseSetting("Foyer:Mongo:ConnectionString", connectionString);
            builder.UseSetting("Foyer:Mongo:Database", database);
            builder.UseSetting("Demo:SeedCompteDemo", "true"); // amorçage de démo explicitement demandé
        }
    }

    [MongoRequisFact]
    public async Task Should_Connecter_le_compte_demo_avec_le_bon_couple_et_le_refuser_avec_le_mauvais_When_l_amorcage_de_demo_a_seede_le_store_Mongo_reel()
    {
        using var hote = new HoteDemo(ConnectionString, _baseDeTest);
        var client = hote.CreateClient();

        // Bon couple → connexion réussie (session ouverte) : le compte de démo est actif et porte le MDP.
        var bon = await client.PostAsJsonAsync(
            "/api/canal/se-connecter", new { Email = EmailDemo, MotDePasse = MotDePasseDemo });
        Assert.True(bon.IsSuccessStatusCode, $"la connexion du compte de démo doit réussir, statut {(int)bon.StatusCode}.");

        // Mauvais mot de passe → refus (motif neutre) : le condensat posé est bien vérifié.
        var mauvais = await client.PostAsJsonAsync(
            "/api/canal/se-connecter", new { Email = EmailDemo, MotDePasse = "mauvais-mot-de-passe" });
        Assert.False(mauvais.IsSuccessStatusCode, "un mauvais mot de passe doit être refusé.");
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
