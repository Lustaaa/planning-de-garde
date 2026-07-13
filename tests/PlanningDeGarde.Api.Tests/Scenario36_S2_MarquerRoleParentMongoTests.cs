using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 36 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) de la commande
/// <c>MarquerRoleParent</c> câblée sur le store durable. Le flag posé via le handler survit au redémarrage
/// (nouvelle instance de store sur la même base persistée) ; la décoche (estParent=false) est durable ; un
/// roleId inexistant est refusé sans écriture (aucun rôle fantôme au redémarrage).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class Scenario36_S2_MarquerRoleParentMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielRolesMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Persister_la_bascule_du_flag_durablement_apres_redemarrage_When_la_commande_est_cablee_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : créer « Papa » puis le marquer parent via le handler câblé sur le store durable ---
        string papaId;
        {
            var store1 = NouveauStore();
            papaId = new CreerRoleHandler(store1, store1).Handle(new CreerRoleCommand("Papa")).Valeur!.RoleId;
            Assert.True(new MarquerRoleParentHandler(store1, store1)
                .Handle(new MarquerRoleParentCommand(papaId, true)).EstSucces);
        }

        // --- Redémarrage : le flag est relu true (write-through durable) ---
        var store2 = NouveauStore();
        Assert.True(store2.EnumererRoles().Single(r => r.Id == papaId).EstRoleParent);

        // Décoche via le handler → durable au redémarrage suivant.
        Assert.True(new MarquerRoleParentHandler(store2, store2)
            .Handle(new MarquerRoleParentCommand(papaId, false)).EstSucces);
        var store3 = NouveauStore();
        Assert.False(store3.EnumererRoles().Single(r => r.Id == papaId).EstRoleParent);
    }

    [MongoRequisFact]
    public void Un_role_inexistant_est_refuse_sans_ecriture_aucun_role_fantome_au_redemarrage()
    {
        var store1 = NouveauStore();
        var resultat = new MarquerRoleParentHandler(store1, store1)
            .Handle(new MarquerRoleParentCommand("role-fantome", true));

        Assert.False(resultat.EstSucces);
        Assert.Equal("rôle inexistant", resultat.Motif);

        var store2 = NouveauStore();
        Assert.DoesNotContain(store2.EnumererRoles(), r => r.Id == "role-fantome");
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
