using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 38 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : le 2ᵉ des deux adaptateurs, InMemory étant prouvé côté PlanningDeGarde.Tests). La query
/// agrégée <see cref="GrapheFoyerQuery"/> est câblée sur les stores durables réels
/// (<see cref="ReferentielEnfantsMongo"/> pour les enfants + liens, <see cref="ConfigurationFoyerMongo"/>
/// pour les noms d'acteurs) : le graphe restitue, PAR enfant, ses parents liés avec nom résolu depuis le
/// store persisté ET son rôle-du-lien (père / mère / parent-libre s37). Un lien s34 nu est relu à
/// « parent-libre » (défaut neutre). <b>Lecture PURE</b> — aucune écriture par la query.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class GrapheFoyerMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Restituer_le_graphe_par_enfant_avec_nom_resolu_et_role_du_lien_sur_Mongo_reel()
    {
        // --- Given : deux acteurs durables (Alice, Bob) + Léa liée Alice(mère)/Bob(père), Tom lié Alice (nu) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bobId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;

        var storeEnfants = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);
        var ajout = new AjouterEnfantHandler(storeEnfants, storeEnfants, new NotificateurMuet());
        var leaId = ajout.Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
        var tomId = ajout.Handle(new AjouterEnfantCommand("Tom")).Valeur!.EnfantId;
        storeEnfants.LierParent(leaId, aliceId, RoleDuLien.Mere);
        storeEnfants.LierParent(leaId, bobId, RoleDuLien.Pere);
        storeEnfants.LierParent(tomId, aliceId); // lien nu → parent-libre (défaut neutre s37)

        // --- Redémarrage : NOUVELLES instances de stores sur la MÊME base persistée ---
        var configApresRedemarrage = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var enfantsApresRedemarrage = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);

        // --- When : la query câblée sur les adaptateurs Mongo RÉELS ---
        var graphe = new GrapheFoyerQuery(enfantsApresRedemarrage, configApresRedemarrage, configApresRedemarrage).Lire();

        // --- Then : Léa en racine, Alice (mère) + Bob (père) en branches, noms résolus depuis Mongo ---
        var lea = graphe.Single(e => e.EnfantId == leaId);
        Assert.Equal("Léa", lea.Prenom);
        Assert.Contains(lea.Parents, p => p.ActeurId == aliceId && p.Nom == "Alice" && p.Role == RoleDuLien.Mere);
        Assert.Contains(lea.Parents, p => p.ActeurId == bobId && p.Nom == "Bob" && p.Role == RoleDuLien.Pere);

        // Tom : lien nu relu à « parent-libre », nom résolu depuis Mongo
        var tom = graphe.Single(e => e.EnfantId == tomId);
        var parentDeTom = Assert.Single(tom.Parents);
        Assert.Equal(aliceId, parentDeTom.ActeurId);
        Assert.Equal("Alice", parentDeTom.Nom);
        Assert.Equal(RoleDuLien.ParentLibre, parentDeTom.Role);
    }

    [MongoRequisFact]
    public void Acceptation_Should_Ne_produire_aucune_branche_fantome_pour_un_acteur_supprime_encore_reference_sur_Mongo_reel()
    {
        // --- Given : Alice + Bob durables, Léa liée aux deux, puis Bob SUPPRIMÉ (lien résiduel conservé) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bobId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;

        var storeEnfants = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);
        var leaId = new AjouterEnfantHandler(storeEnfants, storeEnfants, new NotificateurMuet())
            .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
        storeEnfants.LierParent(leaId, aliceId, RoleDuLien.Mere);
        storeEnfants.LierParent(leaId, bobId, RoleDuLien.Pere);

        // Bob supprimé du référentiel d'acteurs ; le lien Léa→Bob subsiste (non délié) — orphelin résiduel.
        config.Supprimer(bobId);

        // --- Redémarrage : NOUVELLES instances sur la MÊME base persistée ---
        var configApresRedemarrage = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var enfantsApresRedemarrage = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);

        var graphe = new GrapheFoyerQuery(enfantsApresRedemarrage, configApresRedemarrage, configApresRedemarrage).Lire();

        // --- Then : Alice reste en branche, Bob orphelin est neutralisé (aucune branche fantôme) ---
        var lea = graphe.Single(e => e.EnfantId == leaId);
        var parent = Assert.Single(lea.Parents);
        Assert.Equal(aliceId, parent.ActeurId);
        Assert.DoesNotContain(lea.Parents, p => p.ActeurId == bobId);
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
