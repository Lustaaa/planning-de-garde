using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 21 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). Un parent crée le rôle « Nounou » via le
/// handler câblé sur le store durable <see cref="ReferentielRolesMongo"/> ; le redémarrage du serveur
/// est matérialisé par une <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée :
/// le rôle créé doit toujours être énuméré (exactement une fois) après redémarrage, sans ressaisie —
/// preuve que la création a bien atteint le store durable (write-through), pas seulement un cache de
/// session.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// plutôt qu'un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class ReferentielRolesMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielRolesMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Enumerer_toujours_Nounou_exactement_une_fois_apres_un_redemarrage_du_serveur_When_le_role_a_ete_cree_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : le parent crée le rôle « Nounou » via le handler câblé sur le store durable ---
        string nounouId;
        {
            var store1 = NouveauStore();
            var handler = new CreerRoleHandler(store1, store1);
            var resultat = handler.Handle(new CreerRoleCommand("Nounou"));
            Assert.True(resultat.EstSucces);
            nounouId = resultat.Valeur!.RoleId;
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — après redémarrage, le référentiel relu énumère toujours « Nounou », exactement une fois,
        // porté par le même identifiant stable.
        var roles = store2.EnumererRoles();
        Assert.Single(roles, r => r.Libelle == "Nounou");
        Assert.Equal(nounouId, roles.Single(r => r.Libelle == "Nounou").Id);
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
