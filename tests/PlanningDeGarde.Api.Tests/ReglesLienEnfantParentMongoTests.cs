using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 34 — S2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (jamais une doublure) des règles du
/// lien enfant↔parent, prouvées <b>sans écriture partielle DURABLE</b>. Un enfant « Léa » est lié à
/// deux parents sur le store durable <see cref="ReferentielEnfantsMongo"/>, puis on tente un 3ᵉ parent
/// valide → refus « 2 parents max » ; un acteur inexistant → refus ; un acteur non-Parent → refus.
/// Après un <b>redémarrage</b> (nouvelle instance de store sur la même base persistée), l'enfant relu
/// porte TOUJOURS exactement ses deux liens d'origine — aucun refus n'a corrompu le store durable.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class ReglesLienEnfantParentMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Conserver_exactement_les_deux_liens_durables_apres_des_refus_When_on_tente_un_3e_parent_un_inexistant_et_un_non_parent()
    {
        // Foyer (adaptateurs InMemory réels) : trois acteurs portant un rôle marqué « est rôle parent »
        // (option B1, s36) + un acteur EXISTANT sans rôle-parent (seed « grand-pere », aucun rôle → non liable).
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
        var mamie = Parent("Mamie");
        const string bob = "grand-pere"; // acteur seed EXISTANT sans rôle-parent → non liable

        string leaId;
        {
            var store1 = NouveauStore();
            leaId = new AjouterEnfantHandler(store1, store1, new NotificateurMuet())
                .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            var lier = new LierEnfantParentHandler(store1, config, roles, store1);

            Assert.True(lier.Handle(new LierEnfantParentCommand(leaId, papa)).EstSucces);
            Assert.True(lier.Handle(new LierEnfantParentCommand(leaId, maman)).EstSucces);

            // Refus « 2 parents max » (3ᵉ parent valide), acteur inexistant, acteur sans rôle-parent
            Assert.Equal("2 parents max", lier.Handle(new LierEnfantParentCommand(leaId, mamie)).Motif);
            Assert.Equal("acteur inexistant", lier.Handle(new LierEnfantParentCommand(leaId, "acteur-fantome")).Motif);
            Assert.Equal("acteur sans rôle-parent", lier.Handle(new LierEnfantParentCommand(leaId, bob)).Motif);
        }

        // --- Redémarrage : le store durable conserve exactement les 2 liens d'origine (aucune corruption) ---
        var store2 = NouveauStore();
        var lea = store2.EnumererEnfants().Single(e => e.Id == leaId);
        Assert.Equal(2, lea.ParentsLies.Count);
        Assert.Contains(papa, lea.ParentsLies);
        Assert.Contains(maman, lea.ParentsLies);
        Assert.DoesNotContain(mamie, lea.ParentsLies);
        Assert.DoesNotContain(bob, lea.ParentsLies);
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
