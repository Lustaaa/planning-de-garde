using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 21 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). Un rôle « Nounou » est créé puis renommé
/// « Assistante maternelle » via les handlers câblés sur le store durable <see cref="ReferentielRolesMongo"/> ;
/// le redémarrage est matérialisé par une <b>nouvelle instance de store</b> sur la <b>même base Mongo</b>
/// persistée : le rôle relu doit porter le nouveau libellé sur le MÊME id, sans doublon — preuve que le
/// renommage a atteint le store durable (write-through), pas seulement un cache de session.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class RenommerRoleMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielRolesMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Relire_Assistante_maternelle_sur_le_meme_id_sans_doublon_apres_un_redemarrage_When_le_role_a_ete_renomme_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : « Nounou » est créée puis renommée « Assistante maternelle » ---
        string nounouId;
        {
            var store1 = NouveauStore();
            nounouId = new CreerRoleHandler(store1, store1).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;
            var renommage = new RenommerRoleHandler(store1, store1).Handle(new RenommerRoleCommand(nounouId, "Assistante maternelle"));
            Assert.True(renommage.EstSucces);
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — après redémarrage, le rôle relu porte le nouveau libellé sur le même id, sans doublon.
        var roles = store2.EnumererRoles();
        Assert.Single(roles);
        Assert.Equal(nounouId, roles.Single().Id);
        Assert.Equal("Assistante maternelle", roles.Single().Libelle);
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
