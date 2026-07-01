using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 22 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). Un parent crée un compte d'email
/// « alice@foyer.fr » associé à un acteur via le handler câblé sur le store durable
/// <see cref="ReferentielComptesMongo"/> ; le redémarrage du serveur est matérialisé par une
/// <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée : le compte créé doit
/// toujours être énuméré (exactement une fois), avec son email, son statut « inactif » et l'id de
/// l'acteur associé, après redémarrage — preuve que la création a atteint le store durable
/// (write-through), pas seulement un cache de session.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// plutôt qu'un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class ReferentielComptesMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielComptesMongo NouveauStore() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Enumerer_toujours_le_compte_avec_email_statut_inactif_et_acteur_apres_un_redemarrage_When_le_compte_a_ete_cree_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : un acteur déclaré dans le store réel, puis un compte « alice@foyer.fr » créé pour lui ---
        string acteurId;
        string compteId;
        {
            var config1 = NouvelleConfig();
            acteurId = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
            var store1 = NouveauStore();
            var handler = new CreerCompteHandler(store1, store1, config1);
            var resultat = handler.Handle(new CreerCompteCommand("alice@foyer.fr", acteurId));
            Assert.True(resultat.EstSucces);
            compteId = resultat.Valeur!.CompteId;
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — après redémarrage, le référentiel relu énumère toujours le compte, exactement une fois,
        // porté par le même id stable, avec son email, son statut « inactif » et l'acteur associé.
        var comptes = store2.EnumererComptes();
        Assert.Single(comptes, c => c.Email == "alice@foyer.fr");
        var compte = comptes.Single(c => c.Email == "alice@foyer.fr");
        Assert.Equal(compteId, compte.Id);
        Assert.Equal(StatutCompte.Inactif, compte.Statut);
        Assert.Equal(acteurId, compte.ActeurId);
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
