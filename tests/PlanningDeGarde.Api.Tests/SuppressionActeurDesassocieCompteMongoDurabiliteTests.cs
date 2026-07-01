using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 22 — Sc.6 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : R4). Un acteur déclaré porte un compte, puis l'acteur est supprimé du foyer via le
/// handler câblé sur les stores durables ; le redémarrage du serveur est matérialisé par de
/// <b>nouvelles instances de store</b> sur la <b>même base Mongo</b> persistée : après redémarrage, le
/// compte doit toujours être énuméré mais DÉSASSOCIÉ (ActeurId null), sans compte fantôme référençant
/// l'acteur absent — preuve que le repli propre a atteint le store durable (write-through).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class SuppressionActeurDesassocieCompteMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielComptesMongo NouveauStoreComptes() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    /// <summary>Notificateur inerte : la diffusion temps réel n'est pas l'objet de cette durabilité.</summary>
    private sealed class NotificateurInerte : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Enumerer_toujours_le_compte_desassocie_sans_fantome_apres_un_redemarrage_When_l_acteur_associe_a_ete_supprime_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : un acteur déclaré porte un compte, puis l'acteur est supprimé ---
        string compteId;
        string acteurId;
        {
            var config1 = NouvelleConfig();
            var comptes1 = NouveauStoreComptes();
            acteurId = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
            compteId = new CreerCompteHandler(comptes1, comptes1, config1).Handle(new CreerCompteCommand("alice@foyer.fr", acteurId)).Valeur!.CompteId;
            var suppression = new SupprimerActeurHandler(config1, new NotificateurInerte(), comptes1, comptes1)
                .Handle(new SupprimerActeurCommand(acteurId));
            Assert.True(suppression.EstSucces);
        }

        // --- Redémarrage : NOUVELLES instances de store sur la MÊME base Mongo persistée ---
        var comptes2 = NouveauStoreComptes();

        // Then — après redémarrage, le compte survit, énuméré, mais DÉSASSOCIÉ (aucun fantôme).
        var comptes = comptes2.EnumererComptes();
        var compte = comptes.Single(c => c.Id == compteId);
        Assert.Null(compte.ActeurId);                                    // désassocié durablement
        Assert.DoesNotContain(comptes, c => c.ActeurId == acteurId);     // aucun compte fantôme
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
