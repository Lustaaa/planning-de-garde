using MongoDB.Driver;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Surface <b>seed-once</b> du pivot durabilité (Sc.3), prouvée contre un store <b>Mongo réel</b>
/// (Docker) au niveau de l'adaptateur de droite (lecture via une instance fraîche = un redémarrage).
/// Deux tests cernent l'inversion exacte de la volatilité assumée :
/// <list type="bullet">
///   <item>amorçage initial : seed depuis le <c>Foyer</c> quand le store est vide ;</item>
///   <item><b>cardinal</b> : une instance fraîche relit l'état persisté <b>sans re-seeder</b> —
///   un re-seed au démarrage écraserait Alicia → Alice et supprimerait l'acteur ajouté.</item>
/// </list>
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution, supprimée en fin de test.
/// </summary>
public sealed class ConfigurationFoyerMongoSeedOnceTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_seed_{Guid.NewGuid():N}";

    private ConfigurationFoyerMongo NouvelleInstance() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Should_Seeder_le_referentiel_depuis_le_Foyer_au_demarrage_When_le_store_durable_est_vide()
    {
        var store = NouvelleInstance(); // store vide → seed-once amorce depuis le Foyer

        var acteurs = store.EnumererActeurs();
        Assert.Contains("parent-a", acteurs);
        Assert.Equal("Alice", store.NomDe("parent-a"));
        Assert.Equal("bleu", store.CouleurDe("parent-a"));
    }

    [MongoRequisFact]
    public void Should_Relire_l_etat_persiste_sans_re_seeder_par_dessus_les_editions_When_le_store_durable_est_deja_peuple_au_redemarrage()
    {
        const string carlaId = "acteur-carla-seedonce";

        // Instance #1 : seed-once amorce, puis on édite (renommage) et on ajoute (acteur neuf).
        var instance1 = NouvelleInstance();
        instance1.Renommer("parent-a", "Alicia");
        instance1.Ajouter(carlaId, "Carla", "rose");

        // Redémarrage : instance fraîche sur la MÊME base déjà peuplée → relit SANS re-seeder.
        var instance2 = NouvelleInstance();

        Assert.Equal("Alicia", instance2.NomDe("parent-a"));     // l'édition n'est pas écrasée par un re-seed
        Assert.Contains(carlaId, instance2.EnumererActeurs());    // l'acteur ajouté n'est pas supprimé
        Assert.Equal("Carla", instance2.NomDe(carlaId));
        Assert.Equal("rose", instance2.CouleurDe(carlaId));
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
