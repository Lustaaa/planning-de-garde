using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 40 — Sc.3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : le statut
/// R3 (s40) est SIGNALÉ, jamais IMPOSÉ. Sur le store durable, lier un enfant à un SEUL parent RÉUSSIT (aucune
/// contrainte « exactement 2 » ajoutée) ; la projection enrichie <see cref="GrapheFoyerQuery"/> le signale
/// <c>Incomplet</c> SANS refuser ni muter aucune écriture, et deux exécutions successives laissent le lien
/// durable inchangé (lecture pure). <b>Skip propre</b> si Docker / Mongo indisponible.
/// </summary>
public sealed class Scenario40_S3_AucunBlocageEcritureMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Accepter_un_seul_parent_et_le_signaler_incomplet_sans_muter_le_store_sur_Mongo_reel()
    {
        // --- Given : un acteur portant un rôle marqué parent (B1, s36) + Léa sur le store durable ---
        var roles = new ReferentielRolesEnMemoire();
        roles.Creer("role-papa", "Papa");
        roles.MarquerParent("role-papa", true);
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var papaId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Papa")).Valeur!.ActeurId;
        config.AffecterRole(papaId, "role-papa");

        var store = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);
        var leaId = new AjouterEnfantHandler(store, store, new NotificateurMuet())
            .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;

        // --- When : lier un SEUL parent via le canal d'écriture existant ---
        var resultat = new LierEnfantParentHandler(store, config, roles, store)
            .Handle(new LierEnfantParentCommand(leaId, papaId, RoleDuLien.Pere));

        // --- Then : l'écriture RÉUSSIT (aucun invariant « exactement 2 » imposé) ---
        Assert.True(resultat.EstSucces);

        // Le statut le SIGNALE incomplet sans refuser l'écriture ; deux exécutions ne mutent pas le store durable.
        var enfantsRelu = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);
        var configRelu = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var query = new GrapheFoyerQuery(enfantsRelu, configRelu, configRelu);
        Assert.Equal(StatutCoupleR3.Incomplet, query.Lire().Single(e => e.EnfantId == leaId).StatutCouple);
        _ = query.Lire();

        // Après le redémarrage, le lien unique est TOUJOURS là (aucune écriture déclenchée par la lecture).
        var apres = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest)
            .EnumererEnfants().Single(e => e.Id == leaId).ParentsLies;
        Assert.Single(apres);
        Assert.Contains(apres, p => p.ActeurId == papaId);
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
