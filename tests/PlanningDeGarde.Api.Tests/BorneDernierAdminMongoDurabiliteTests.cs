using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 41 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure). La borne « dernier admin » est vérifiée à l'identique sur le store durable
/// <see cref="AdminsFoyerMongo"/> : un unique admin déclaré sur le store réel ne peut être dé-désigné
/// (refus AVANT écriture) ; après « redémarrage » (nouvelles instances de store sur la même base Mongo
/// persistée), il est TOUJOURS énuméré admin — le store n'a jamais été touché (aucune mutation partielle).
///
/// <b>Skip propre</b> si Docker / Mongo est indisponible. Base isolée par exécution (Guid), supprimée en fin.
/// </summary>
public sealed class BorneDernierAdminMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private AdminsFoyerMongo NouveauStore() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Refuser_et_conserver_le_dernier_admin_apres_redemarrage_When_on_le_de_designe_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : un unique acteur Parent désigné admin, puis tentative de dé-désignation ---
        string parentA;
        {
            var config1 = NouvelleConfig();
            parentA = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
            var admins1 = NouveauStore();
            new DesignerAdminHandler(admins1, admins1, config1).Handle(new DesignerAdminCommand(parentA));

            var resultat = new DeDesignerAdminHandler(admins1, admins1, config1).Handle(new DeDesignerAdminCommand(parentA));
            Assert.False(resultat.EstSucces); // borne « dernier admin » : refus AVANT écriture
            Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        }

        // --- Redémarrage : NOUVELLES instances de store sur la MÊME base Mongo persistée ---
        var admins2 = NouveauStore();

        // Then — le dernier admin est TOUJOURS énuméré (store jamais touché, foyer garde ≥1 admin).
        Assert.Contains(parentA, admins2.EnumererAdmins());
    }

    public void Dispose()
    {
        try
        {
            new MongoClient(ConnectionString).DropDatabase(_baseDeTest);
        }
        catch
        {
            // Best effort.
        }
    }
}
