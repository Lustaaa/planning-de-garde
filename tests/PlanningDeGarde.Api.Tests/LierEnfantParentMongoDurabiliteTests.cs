using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 34 — S1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert »). Un enfant « Léa » est ajouté puis lié à un
/// parent-acteur (portant le rôle « Parent ») via le handler câblé sur le store durable
/// <see cref="ReferentielEnfantsMongo"/> ; le redémarrage du serveur est matérialisé par une
/// <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée : après redémarrage,
/// l'enfant relu doit toujours porter le parent dans <see cref="EnfantFoyer.ParentsLies"/>, avec son
/// identifiant stable inchangé — preuve que le lien a atteint le store durable (write-through), pas un
/// cache de session. Un enfant non lié reste valide avec une liste de parents vide.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class LierEnfantParentMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    /// <summary>Foyer (adaptateurs InMemory réels) avec un acteur AJOUTÉ en session — de type
    /// <see cref="TypeActeur.Parent"/> par défaut (Foyer.TypeParDefaut), SANS aucun rôle — précondition
    /// valide du lien (option A, s36 : parent liable = TypeActeur.Parent, le rôle ne qualifie plus).
    /// Retourne l'identifiant stable du parent.</summary>
    private static (ConfigurationFoyerEnMemoire config, string parentId) FoyerAvecUnParent(string prenom)
    {
        var config = new ConfigurationFoyerEnMemoire();
        var parentId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand(prenom)).Valeur!.ActeurId;
        return (config, parentId);
    }

    [MongoRequisFact]
    public void Acceptation_Should_Relire_le_parent_lie_sur_le_meme_id_apres_redemarrage_When_l_enfant_a_ete_lie_sur_le_store_Mongo_reel()
    {
        var (config, papaId) = FoyerAvecUnParent("Papa");

        // --- Serveur #1 : ajout de « Léa » et « Tom », puis lien Léa → parent-acteur « Papa » ---
        string leaId, tomId;
        {
            var store1 = NouveauStore();
            var ajout = new AjouterEnfantHandler(store1, store1, new NotificateurMuet());
            leaId = ajout.Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
            tomId = ajout.Handle(new AjouterEnfantCommand("Tom")).Valeur!.EnfantId;

            var lier = new LierEnfantParentHandler(store1, config, store1);
            var resultat = lier.Handle(new LierEnfantParentCommand(leaId, papaId));
            Assert.True(resultat.EstSucces);
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();
        var enfants = store2.EnumererEnfants();

        // Léa relue porte le parent lié, id stable inchangé (enrichissement durable)
        var lea = enfants.Single(e => e.Id == leaId);
        Assert.Equal("Léa", lea.Prenom);
        Assert.Contains(papaId, lea.ParentsLies);

        // Tom non lié reste valide : liste de parents vide (lien optionnel, 0 parent accepté)
        var tom = enfants.Single(e => e.Id == tomId);
        Assert.Empty(tom.ParentsLies);
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
