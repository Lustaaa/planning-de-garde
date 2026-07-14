using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 40 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : la
/// projection s38 <see cref="GrapheFoyerQuery"/>, câblée sur les stores durables réels, restitue PAR
/// enfant un <b>statut de complétude du couple</b> (R3, s40) composé des liens s34 + rôles-du-lien s37
/// déjà persistés. Un enfant lié à un « père » ET une « mère » relus depuis Mongo est <c>Complet</c>.
/// <b>Lecture PURE</b> — aucune écriture par la query. <b>Skip propre</b> si Docker / Mongo indisponible.
/// </summary>
public sealed class Scenario40_S1_StatutCoupleMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Enrichir_le_statut_de_completude_sur_Mongo_reel()
    {
        // --- Given : Alice + Bob durables ; Léa liée Alice(mère)/Bob(père) — couple père+mère complet ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bobId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;

        var storeEnfants = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);
        var leaId = new AjouterEnfantHandler(storeEnfants, storeEnfants, new NotificateurMuet())
            .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
        storeEnfants.LierParent(leaId, aliceId, RoleDuLien.Mere);
        storeEnfants.LierParent(leaId, bobId, RoleDuLien.Pere);

        // --- Redémarrage : NOUVELLES instances de stores sur la MÊME base persistée ---
        var configApresRedemarrage = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var enfantsApresRedemarrage = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);

        // --- When : la query câblée sur les adaptateurs Mongo RÉELS ---
        var graphe = new GrapheFoyerQuery(enfantsApresRedemarrage, configApresRedemarrage, configApresRedemarrage).Lire();

        // --- Then : Léa (père + mère) est COMPLET, statut composé depuis Mongo ---
        var lea = graphe.Single(e => e.EnfantId == leaId);
        Assert.Equal(StatutCoupleR3.Complet, lea.StatutCouple);
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
