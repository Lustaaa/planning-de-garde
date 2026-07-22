using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

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
            "/api/session", new { Email = EmailDemo, MotDePasse = MotDePasseDemo });
        Assert.True(bon.IsSuccessStatusCode, $"la connexion du compte de démo doit réussir, statut {(int)bon.StatusCode}.");

        // Mauvais mot de passe → refus (motif neutre) : le condensat posé est bien vérifié.
        var mauvais = await client.PostAsJsonAsync(
            "/api/session", new { Email = EmailDemo, MotDePasse = "mauvais-mot-de-passe" });
        Assert.False(mauvais.IsSuccessStatusCode, "un mauvais mot de passe doit être refusé.");
    }

    [MongoRequisFact]
    public async Task Should_Poser_le_mot_de_passe_et_durcir_le_login_When_un_compte_email_only_de_meme_email_preexiste_sur_le_store_durable()
    {
        // Contexte réel : une tentative antérieure a laissé sur le store durable un compte ACTIF mais
        // email-only (aucun mot de passe) portant l'email de démo. Un amorçage « skip si l'email existe »
        // laisserait ce compte SANS mot de passe → le login accepterait n'importe quel mot de passe
        // (login email-only). L'amorçage doit CONVERGER : poser le mot de passe cible sur ce compte.
        new ReferentielComptesMongo(ConnectionString, _baseDeTest)
            .Creer("compte-demo-preexistant", EmailDemo, StatutCompte.Actif, "acteur-demo-preexistant");

        using var hote = new HoteDemo(ConnectionString, _baseDeTest); // l'amorçage de démo converge au démarrage
        var client = hote.CreateClient();

        // Bon couple → connexion réussie : le condensat cible a bien été posé sur le compte préexistant.
        var bon = await client.PostAsJsonAsync(
            "/api/session", new { Email = EmailDemo, MotDePasse = MotDePasseDemo });
        Assert.True(bon.IsSuccessStatusCode, $"la connexion du compte de démo doit réussir, statut {(int)bon.StatusCode}.");

        // Mauvais mot de passe → refus : le login n'est plus permissif (le mot de passe est désormais vérifié).
        var mauvais = await client.PostAsJsonAsync(
            "/api/session", new { Email = EmailDemo, MotDePasse = "mauvais-mot-de-passe" });
        Assert.False(mauvais.IsSuccessStatusCode, "un mauvais mot de passe doit être refusé (login non permissif).");
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
