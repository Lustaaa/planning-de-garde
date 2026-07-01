using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 21 — Sc.6 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : R4). Un rôle « Nounou » est créé, un acteur le porte, puis le parent supprime le rôle via
/// le handler câblé sur les stores durables ; le redémarrage est matérialisé par de <b>nouvelles
/// instances de store</b> sur la <b>même base Mongo</b> persistée : après redémarrage, le rôle ne doit
/// plus être énuméré ET l'acteur qui le portait doit être « sans rôle » (repli neutre durable, aucun rôle
/// fantôme) — preuve que suppression et repli ont atteint le store durable (write-through).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class SupprimerRoleMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);
    private ReferentielRolesMongo NouveauReferentiel() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Ne_plus_enumerer_le_role_et_faire_retomber_le_porteur_sans_role_apres_un_redemarrage_When_le_role_a_ete_supprime_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : rôle « Nounou » créé, porté par un acteur, puis supprimé ---
        string acteurId;
        string nounouId;
        {
            var config1 = NouvelleConfig();
            var referentiel1 = NouveauReferentiel();
            acteurId = new AjouterActeurHandler(config1).Handle(new AjouterActeurCommand("Carla", "rose")).Valeur!.ActeurId;
            nounouId = new CreerRoleHandler(referentiel1, referentiel1).Handle(new CreerRoleCommand("Nounou")).Valeur!.RoleId;
            new AffecterRoleActeurHandler(referentiel1, config1).Handle(new AffecterRoleActeurCommand(acteurId, nounouId));
            var suppression = new SupprimerRoleHandler(referentiel1, config1, config1).Handle(new SupprimerRoleCommand(nounouId));
            Assert.True(suppression.EstSucces);
        }

        // --- Redémarrage : NOUVELLES instances de store sur la MÊME base Mongo persistée ---
        var config2 = NouvelleConfig();
        var referentiel2 = NouveauReferentiel();

        // Then — après redémarrage, le rôle a disparu du référentiel et l'acteur est « sans rôle ».
        Assert.DoesNotContain(referentiel2.EnumererRoles(), r => r.Id == nounouId);
        Assert.Null(config2.RoleDe(acteurId));
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
