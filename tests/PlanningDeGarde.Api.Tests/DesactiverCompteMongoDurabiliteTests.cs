using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 41 — Sc.3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure). Un compte est créé puis activé (Actif) sur le store réel, puis <b>désactivé</b> via le
/// handler câblé sur le store durable <see cref="ReferentielComptesMongo"/> ; le redémarrage du serveur
/// est matérialisé par une <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée :
/// après redémarrage, le compte doit être énuméré au statut « Inactif » — preuve que la désactivation a
/// atteint le store durable (write-through, elle survit au rechargement), email et acteur associé inchangés.
///
/// <b>Skip propre</b> si Docker / Mongo est indisponible. Base isolée par exécution (Guid), supprimée en fin.
/// </summary>
public sealed class DesactiverCompteMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielComptesMongo NouveauStore() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Enumerer_le_compte_Inactif_apres_redemarrage_When_il_a_ete_desactive_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : un compte créé (Inactif), activé (Actif), puis désactivé (Inactif) ---
        string acteurId, compteId;
        {
            var config1 = NouvelleConfig();
            acteurId = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
            var store1 = NouveauStore();
            compteId = new CreerCompteHandler(store1, store1, config1).Handle(new CreerCompteCommand("alice@foyer.fr", acteurId)).Valeur!.CompteId;
            new ActiverCompteHandler(store1, store1).Handle(new ActiverCompteCommand(compteId));

            var resultat = new DesactiverCompteHandler(store1, store1).Handle(new DesactiverCompteCommand(compteId));
            Assert.True(resultat.EstSucces);
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — après redémarrage, le compte est relu au statut « Inactif », email et acteur inchangés.
        var compte = store2.EnumererComptes().Single(c => c.Id == compteId);
        Assert.Equal(StatutCompte.Inactif, compte.Statut);
        Assert.Equal("alice@foyer.fr", compte.Email);
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
            // Best effort.
        }
    }
}
