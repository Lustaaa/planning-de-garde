using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 34 — S3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) du retrait de
/// lien enfant↔parent, prouvé <b>durable</b>. Un enfant lié à deux parents sur le store durable
/// <see cref="ReferentielEnfantsMongo"/> est délié d'un parent ; après un <b>redémarrage</b> (nouvelle
/// instance de store sur la même base persistée) l'enfant relu ne porte plus ce parent mais conserve
/// l'autre — preuve que le retrait a atteint le store durable (write-through). Délier un parent déjà
/// non lié est idempotent : aucune écriture, l'état durable reste identique.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class DelierEnfantParentMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Relire_l_enfant_sans_le_parent_delie_mais_avec_l_autre_apres_redemarrage_When_on_delie_sur_le_store_Mongo_reel()
    {
        // Foyer (adaptateurs InMemory réels) : deux acteurs portant un rôle marqué « est rôle parent »
        // (option B1, s36 : parent liable = l'acteur porte un rôle marqué parent).
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
        var maman = Parent("Maman");
        var mamie = Parent("Mamie"); // jamais liée — cible du délier idempotent

        string leaId;
        {
            var store1 = NouveauStore();
            leaId = new AjouterEnfantHandler(store1, store1, new NotificateurMuet())
                .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            var lier = new LierEnfantParentHandler(store1, config, roles, store1);
            lier.Handle(new LierEnfantParentCommand(leaId, papa));
            lier.Handle(new LierEnfantParentCommand(leaId, maman));

            var delier = new DelierEnfantParentHandler(store1);
            Assert.True(delier.Handle(new DelierEnfantParentCommand(leaId, papa)).EstSucces);      // retrait
            Assert.True(delier.Handle(new DelierEnfantParentCommand(leaId, mamie)).EstSucces);     // idempotent (non liée)
        }

        // --- Redémarrage : le retrait est durable, l'autre lien conservé ---
        var store2 = NouveauStore();
        var lea = store2.EnumererEnfants().Single(e => e.Id == leaId);
        Assert.DoesNotContain(lea.ParentsLies, p => p.ActeurId == papa); // parent délié absent durablement
        Assert.Single(lea.ParentsLies);
        Assert.Contains(lea.ParentsLies, p => p.ActeurId == maman);      // l'autre lien intact
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
