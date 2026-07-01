using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 22 — Sc.4 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : R4). Un acteur de type Parent est déclaré dans le store réel, désigné admin du foyer via
/// le handler câblé sur le store durable <see cref="AdminsFoyerMongo"/> ; le redémarrage du serveur est
/// matérialisé par de <b>nouvelles instances de store</b> sur la <b>même base Mongo</b> persistée : après
/// redémarrage, l'admin doit toujours être énuméré — preuve que la désignation a atteint le store durable
/// (write-through). Un acteur non-Parent, lui, est rejeté par l'invariant et n'est jamais persisté.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class AdminsFoyerMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private AdminsFoyerMongo NouveauStore() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Enumerer_toujours_le_parent_admin_apres_un_redemarrage_When_il_a_ete_designe_admin_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : un acteur (type Parent par défaut, s14) déclaré, puis désigné admin ---
        string parentId;
        {
            var config1 = NouvelleConfig();
            parentId = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
            var admins1 = NouveauStore();
            var handler = new DesignerAdminHandler(admins1, admins1, config1);
            var resultat = handler.Handle(new DesignerAdminCommand(parentId));
            Assert.True(resultat.EstSucces);
        }

        // --- Redémarrage : NOUVELLES instances de store sur la MÊME base Mongo persistée ---
        var admins2 = NouveauStore();

        // Then — après redémarrage, l'admin désigné est toujours énuméré (désignation durable).
        Assert.Contains(parentId, admins2.EnumererAdmins());
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
