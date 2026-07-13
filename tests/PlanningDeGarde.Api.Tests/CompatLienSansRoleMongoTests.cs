using MongoDB.Bson;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 37 — Sc.3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) de la
/// <b>compat non destructive</b> des liens enfant↔parent déjà persistés par un sprint antérieur (s34),
/// SANS attribut rôle-du-lien. Un document est écrit <b>à la main dans la forme s34</b> (champ
/// <c>ParentsLies</c> = tableau d'ids d'acteurs, AUCUN champ de rôles) directement dans la collection
/// Mongo, puis relu par l'adaptateur <see cref="ReferentielEnfantsMongo"/> : chaque parent lié est relu
/// à <see cref="RoleDuLien.ParentLibre"/> (défaut neutre), SANS crash de désérialisation, l'ancien
/// document restant valide (aucune migration destructive). Round-trip : un lien créé ce sprint avec un
/// rôle « mère » explicite est relu tel quel après rechargement.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class CompatLienSansRoleMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Relire_un_lien_s34_sans_attribut_role_a_parent_libre_sans_crash_When_le_document_est_dans_la_forme_ancienne()
    {
        // --- Un document dans la FORME s34 (ParentsLies = ids, AUCUN champ de rôles) écrit à la main ---
        var db = new MongoClient(ConnectionString).GetDatabase(_baseDeTest);
        db.GetCollection<BsonDocument>("enfants").InsertOne(new BsonDocument
        {
            { "_id", "enfant-legacy" },
            { "Prenom", "Léa" },
            { "ParentsLies", new BsonArray { "parent-a", "parent-b" } },
            // NB : AUCUN champ RolesDesLiens — c'est exactement l'état persisté par s34.
        });

        // --- Relecture par l'adaptateur courant : défaut « parent-libre », pas de crash ---
        var lea = NouveauStore().EnumererEnfants().Single(e => e.Id == "enfant-legacy");
        Assert.Equal("Léa", lea.Prenom);
        Assert.Equal(2, lea.ParentsLies.Count);
        Assert.All(lea.ParentsLies, p => Assert.Equal(RoleDuLien.ParentLibre, p.Role));

        // L'ancien document reste valide, non migré destructivement : ses ids d'acteurs intacts.
        Assert.Contains(lea.ParentsLies, p => p.ActeurId == "parent-a");
        Assert.Contains(lea.ParentsLies, p => p.ActeurId == "parent-b");
    }

    [MongoRequisFact]
    public void Acceptation_Should_Relire_le_role_mere_tel_quel_apres_rechargement_When_le_lien_a_ete_cree_avec_un_role_explicite()
    {
        var roles = new ReferentielRolesEnMemoire();
        roles.Creer("role-papa", "Papa");
        roles.MarquerParent("role-papa", true);
        var config = new ConfigurationFoyerEnMemoire();
        var mamanId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Maman")).Valeur!.ActeurId;
        config.AffecterRole(mamanId, "role-papa");

        string leaId;
        {
            var store1 = NouveauStore();
            leaId = new AjouterEnfantHandler(store1, store1, new NotificateurMuet())
                .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            var lier = new LierEnfantParentHandler(store1, config, roles, store1);
            Assert.True(lier.Handle(new LierEnfantParentCommand(leaId, mamanId, RoleDuLien.Mere)).EstSucces);
        }

        var lea = NouveauStore().EnumererEnfants().Single(e => e.Id == leaId);
        Assert.Equal(leaId, lea.Id); // id enfant inchangé
        Assert.Equal(RoleDuLien.Mere, lea.ParentsLies.Single(p => p.ActeurId == mamanId).Role);
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
