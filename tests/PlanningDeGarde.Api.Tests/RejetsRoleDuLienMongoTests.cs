using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 37 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) du rejet
/// « pas deux liens de même rôle exclusif » (père/mère), prouvé <b>sans écriture partielle DURABLE</b>.
/// Un enfant lié à un « père » sur le store durable <see cref="ReferentielEnfantsMongo"/> refuse un
/// SECOND « père » ; après un <b>redémarrage</b> (nouvelle instance de store sur la même base persistée),
/// l'enfant relu porte TOUJOURS son unique lien « père » d'origine — le refus n'a pas corrompu le store.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class RejetsRoleDuLienMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Conserver_l_unique_lien_pere_durable_apres_le_refus_d_un_second_pere_When_on_lie_sur_le_store_Mongo_reel()
    {
        var roles = new ReferentielRolesEnMemoire();
        roles.Creer("role-papa", "Papa");
        roles.MarquerParent("role-papa", true);
        var config = new ConfigurationFoyerEnMemoire();
        string Parent(string p)
        {
            var id = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand(p)).Valeur!.ActeurId;
            config.AffecterRole(id, "role-papa");
            return id;
        }
        var papa = Parent("Papa");
        var autre = Parent("Autre");

        string leaId;
        {
            var store1 = NouveauStore();
            leaId = new AjouterEnfantHandler(store1, store1, new NotificateurMuet())
                .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            var lier = new LierEnfantParentHandler(store1, config, roles, store1);
            Assert.True(lier.Handle(new LierEnfantParentCommand(leaId, papa, RoleDuLien.Pere)).EstSucces);

            // Refus « un père est déjà lié à cet enfant » (second père) — aucune écriture durable.
            Assert.Equal("un père est déjà lié à cet enfant",
                lier.Handle(new LierEnfantParentCommand(leaId, autre, RoleDuLien.Pere)).Motif);
        }

        // --- Redémarrage : le store durable conserve exactement l'unique lien « père » d'origine ---
        var store2 = NouveauStore();
        var lea = store2.EnumererEnfants().Single(e => e.Id == leaId);
        Assert.Single(lea.ParentsLies);
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single(p => p.ActeurId == papa).Role);
        Assert.DoesNotContain(lea.ParentsLies, p => p.ActeurId == autre);
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
