using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 21 — Sc.4 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : R4). Un acteur est ajouté et un rôle « Nounou » créé sur le store durable ; on affecte
/// « Nounou » à l'acteur via le handler câblé sur <see cref="ConfigurationFoyerMongo"/> ; le redémarrage
/// est matérialisé par une <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée :
/// l'acteur relu doit toujours porter l'id de rôle — preuve que l'affectation a atteint le store durable
/// (persistée avec la config acteur), pas seulement un cache de session.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class AffecterRoleActeurMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);
    private ReferentielRolesMongo NouveauReferentiel() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Faire_porter_l_id_de_role_a_l_acteur_apres_un_redemarrage_When_le_role_a_ete_affecte_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : acteur ajouté, rôle « Nounou » créé, rôle affecté à l'acteur ---
        string acteurId;
        string nounouId;
        {
            var config1 = NouvelleConfig();
            var referentiel1 = NouveauReferentiel();
            acteurId = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Carla", "rose")).Valeur!.ActeurId;
            nounouId = new CreerRoleHandler(referentiel1, referentiel1).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;
            var affectation = new AffecterRoleActeurHandler(referentiel1, config1).Handle(new AffecterRoleActeurCommand(acteurId, nounouId));
            Assert.True(affectation.EstSucces);
        }

        // --- Redémarrage : NOUVELLE instance de config sur la MÊME base Mongo persistée ---
        var config2 = NouvelleConfig();

        // Then — après redémarrage, l'acteur relu porte toujours l'id de rôle « Nounou ».
        Assert.Equal(nounouId, config2.RoleDe(acteurId));
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
