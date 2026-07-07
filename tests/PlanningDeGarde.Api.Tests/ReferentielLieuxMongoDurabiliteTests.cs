using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 27 — S4 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). Un parent ajoute le lieu « piscine » via le
/// handler câblé sur le store durable <see cref="ReferentielLieuxMongo"/> ; le redémarrage du serveur
/// est matérialisé par une <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée :
/// le lieu ajouté doit toujours être énuméré après redémarrage, sans ressaisie — preuve que l'ajout a
/// bien atteint le store durable (write-through), pas seulement un cache de session. Aucun seed Mongo
/// (parité asymétrie seed s15) : la base ouvre vide, seul « piscine » y figure après l'ajout.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible,
/// plutôt qu'un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class ReferentielLieuxMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielLieuxMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Relire_piscine_depuis_le_store_durable_apres_un_redemarrage_du_serveur_When_le_lieu_a_ete_ajoute_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : le parent ajoute le lieu « piscine » via le handler câblé sur le store durable ---
        string piscineId;
        {
            var store1 = NouveauStore();
            var handler = new AjouterLieuHandler(store1, store1);
            var resultat = handler.Handle(new AjouterLieuCommand("piscine"));
            Assert.True(resultat.EstSucces);
            piscineId = resultat.Valeur!.LieuId;
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — après redémarrage, le référentiel relu énumère toujours « piscine », porté par le même
        // identifiant stable (aucun seed Mongo : « piscine » est le seul lieu présent).
        var lieux = store2.EnumererLieux();
        Assert.Single(lieux, l => l.Libelle == "piscine");
        Assert.Equal(piscineId, lieux.Single(l => l.Libelle == "piscine").Id);
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
