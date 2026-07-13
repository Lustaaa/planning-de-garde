using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 36 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) du flag « est
/// un rôle parent » (option B1). Le flag posé via <see cref="ReferentielRolesMongo.MarquerParent"/> sur le
/// store durable survit au redémarrage (nouvelle instance de store sur la même base persistée) ; un rôle
/// renommé conserve son flag (surfaces distinctes) ; un document ANTÉRIEUR sans le champ (donnée d'avant
/// s36) se relit « est rôle parent » = false (défaut neutre, pas de crash).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class Scenario36_S1_RoleFlagParentMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielRolesMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Relire_le_flag_parent_durablement_apres_redemarrage_When_le_role_a_ete_marque_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : créer « Papa », le marquer parent, puis le renommer (le flag doit survivre) ---
        string papaId;
        {
            var store1 = NouveauStore();
            papaId = new CreerRoleHandler(store1, store1).Handle(new CreerRoleCommand("Papa")).Valeur!.RoleId;
            store1.MarquerParent(papaId, true);
            store1.Renommer(papaId, "Papounet"); // renommage : surface distincte, ne réinitialise pas le flag
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();
        var papa = store2.EnumererRoles().Single(r => r.Id == papaId);
        Assert.Equal("Papounet", papa.Libelle);
        Assert.True(papa.EstRoleParent); // flag durable, préservé au renommage

        // Décoche durable : MarquerParent(false) relu après un nouveau redémarrage.
        store2.MarquerParent(papaId, false);
        var store3 = NouveauStore();
        Assert.False(store3.EnumererRoles().Single(r => r.Id == papaId).EstRoleParent);
    }

    [MongoRequisFact]
    public void Un_document_anterieur_sans_le_champ_flag_se_relit_non_parent_defaut_neutre()
    {
        // Given — un document rôle « à l'ancienne » (avant s36) écrit SANS le champ EstRoleParent.
        var db = new MongoClient(ConnectionString).GetDatabase(_baseDeTest);
        db.GetCollection<BsonDocument>("roles").InsertOne(new BsonDocument
        {
            { "_id", "role-legacy" },
            { "Libelle", "Nounou" },
        });

        // When — le store le relit ; Then — flag = false par défaut, aucun crash.
        var role = NouveauStore().EnumererRoles().Single(r => r.Id == "role-legacy");
        Assert.Equal("Nounou", role.Libelle);
        Assert.False(role.EstRoleParent);
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
