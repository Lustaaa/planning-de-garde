using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 41 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : rempart anti vert-qui-ment). Deux acteurs Parents sont désignés admins sur le store réel,
/// puis l'un est <b>dé-désigné</b> via le handler câblé sur le store durable <see cref="AdminsFoyerMongo"/> ;
/// le redémarrage du serveur est matérialisé par de <b>nouvelles instances de store</b> sur la <b>même base
/// Mongo</b> persistée : après redémarrage, l'acteur dé-désigné ne doit PLUS être énuméré admin, l'autre
/// SI — preuve que la dé-désignation a atteint le store durable (write-through, elle survit au rechargement).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class DeDesignerAdminMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private AdminsFoyerMongo NouveauStore() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Ne_plus_enumerer_l_admin_de_designe_apres_redemarrage_When_il_a_ete_de_designe_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : deux acteurs Parents déclarés et désignés admins, puis l'un dé-désigné ---
        string parentA, parentB;
        {
            var config1 = NouvelleConfig();
            parentA = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
            parentB = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Bruno", "bleu")).Valeur!.ActeurId;
            var admins1 = NouveauStore();
            new DesignerAdminHandler(admins1, admins1, config1).Handle(new DesignerAdminCommand(parentA));
            new DesignerAdminHandler(admins1, admins1, config1).Handle(new DesignerAdminCommand(parentB));

            var resultat = new DeDesignerAdminHandler(admins1, admins1, config1).Handle(new DeDesignerAdminCommand(parentA));
            Assert.True(resultat.EstSucces);
        }

        // --- Redémarrage : NOUVELLES instances de store sur la MÊME base Mongo persistée ---
        var admins2 = NouveauStore();

        // Then — après redémarrage, l'admin dé-désigné n'est PLUS énuméré ; l'autre l'est toujours.
        Assert.DoesNotContain(parentA, admins2.EnumererAdmins());
        Assert.Contains(parentB, admins2.EnumererAdmins());
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
