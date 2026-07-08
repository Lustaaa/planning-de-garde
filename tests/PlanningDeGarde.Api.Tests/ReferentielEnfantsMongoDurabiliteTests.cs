using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 30 — S6 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert »). Un parent ajoute l'enfant « Léa » via le handler
/// câblé sur le store durable <see cref="ReferentielEnfantsMongo"/> ; le redémarrage du serveur est
/// matérialisé par une <b>nouvelle instance de store</b> sur la <b>même base Mongo</b> persistée : l'enfant
/// ajouté doit toujours être énuméré après redémarrage, avec son identifiant stable OPAQUE et son prénom
/// intacts — preuve que l'ajout a bien atteint le store durable (write-through), pas un cache de session.
/// Aucun seed Mongo (parité asymétrie seed s15) : la base ouvre vide, seul « Léa » y figure après l'ajout.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible. Base
/// Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class ReferentielEnfantsMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielEnfantsMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Relire_Lea_avec_son_id_stable_depuis_le_store_durable_apres_un_redemarrage_When_l_enfant_a_ete_ajoute_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : le parent ajoute l'enfant « Léa » via le handler câblé sur le store durable ---
        string leaId;
        {
            var store1 = NouveauStore();
            var handler = new AjouterEnfantHandler(store1, store1, new NotificateurMuet());
            var resultat = handler.Handle(new AjouterEnfantCommand("Léa"));
            Assert.True(resultat.EstSucces);
            leaId = resultat.Valeur!.EnfantId;
            Assert.NotEqual("Léa", leaId); // id opaque, jamais dérivé du prénom
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — après redémarrage, le référentiel relu énumère toujours « Léa », porté par le même
        // identifiant stable (aucun seed Mongo : « Léa » est le seul enfant présent).
        var enfants = store2.EnumererEnfants();
        Assert.Single(enfants, e => e.Prenom == "Léa");
        Assert.Equal(leaId, enfants.Single(e => e.Prenom == "Léa").Id);
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
