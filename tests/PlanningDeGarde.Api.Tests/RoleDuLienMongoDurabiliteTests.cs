using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 37 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) de la
/// <b>durabilité du rôle-du-lien</b>. Un enfant « Léa » est lié à un parent-acteur avec le rôle-du-lien
/// « père » sur le store durable <see cref="ReferentielEnfantsMongo"/> ; après un <b>redémarrage</b>
/// (nouvelle instance de store sur la même base persistée), le parent est relu avec son rôle-du-lien
/// « père » (round-trip durable), l'id stable de l'enfant inchangé. Un lien sans rôle explicite est
/// relu à « parent-libre » (défaut neutre), durablement.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class RoleDuLienMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    private static (ConfigurationFoyerEnMemoire config, ReferentielRolesEnMemoire roles, string papaId, string mamanId) Foyer()
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
        return (config, roles, Parent("Papa"), Parent("Maman"));
    }

    [MongoRequisFact]
    public void Acceptation_Should_Relire_le_role_du_lien_pere_et_parent_libre_apres_redemarrage_When_on_lie_avec_role_sur_le_store_Mongo_reel()
    {
        var (config, roles, papaId, mamanId) = Foyer();

        string leaId;
        {
            var store1 = NouveauStore();
            leaId = new AjouterEnfantHandler(store1, store1, new NotificateurMuet())
                .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            var lier = new LierEnfantParentHandler(store1, config, roles, store1);
            Assert.True(lier.Handle(new LierEnfantParentCommand(leaId, papaId, RoleDuLien.Pere)).EstSucces);
            Assert.True(lier.Handle(new LierEnfantParentCommand(leaId, mamanId)).EstSucces); // sans rôle → parent-libre
        }

        // --- Redémarrage : le rôle-du-lien est durable (round-trip), id enfant inchangé ---
        var store2 = NouveauStore();
        var lea = store2.EnumererEnfants().Single(e => e.Id == leaId);
        Assert.Equal(leaId, lea.Id);
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single(p => p.ActeurId == papaId).Role);
        Assert.Equal(RoleDuLien.ParentLibre, lea.ParentsLies.Single(p => p.ActeurId == mamanId).Role);
    }

    [MongoRequisFact]
    public void Acceptation_Should_Mettre_a_jour_le_role_durablement_sans_dupliquer_When_on_relie_un_parent_deja_lie_avec_un_nouveau_role()
    {
        var (config, roles, papaId, _) = Foyer();

        string leaId;
        {
            var store1 = NouveauStore();
            leaId = new AjouterEnfantHandler(store1, store1, new NotificateurMuet())
                .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            var lier = new LierEnfantParentHandler(store1, config, roles, store1);
            lier.Handle(new LierEnfantParentCommand(leaId, papaId, RoleDuLien.ParentLibre));
            lier.Handle(new LierEnfantParentCommand(leaId, papaId, RoleDuLien.Pere)); // ré-lien → mise à jour du rôle
        }

        var store2 = NouveauStore();
        var lea = store2.EnumererEnfants().Single(e => e.Id == leaId);
        Assert.Single(lea.ParentsLies); // pas de doublon durable
        Assert.Equal(RoleDuLien.Pere, lea.ParentsLies.Single().Role);
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
