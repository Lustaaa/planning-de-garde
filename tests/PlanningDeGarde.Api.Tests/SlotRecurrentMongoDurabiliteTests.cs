using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 29 — S6 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). Un slot récurrent (samedi 11h30–12h15 « Piscine »
/// pour Léa) enregistré via <see cref="MongoSlotRecurrentRepository"/> sur le store durable doit
/// <b>survivre au redémarrage</b> — matérialisé par une <b>nouvelle instance de store</b> sur la
/// <b>même base Mongo</b> — avec son identifiant stable et son snapshot intacts (parité slot ponctuel s15).
/// Aucun seed Mongo (parité asymétrie seed s15) : la base ouvre vide, seul le récurrent posé y figure.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible, jamais
/// un faux vert. Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class SlotRecurrentMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private MongoSlotRecurrentRepository NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Should_Relire_le_slot_recurrent_intact_apres_un_redemarrage_When_il_a_ete_enregistre_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : on enregistre le slot récurrent sur le store durable ---
        string idStable;
        {
            var store1 = NouveauStore();
            store1.Enregistrer(SlotRecurrent
                .Poser("lea", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);
            var enregistre = Assert.Single(store1.AllSnapshots());
            idStable = enregistre.Id;
            Assert.False(string.IsNullOrEmpty(idStable), "le slot récurrent enregistré doit porter un identifiant stable.");
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — le slot récurrent est toujours présent, identifiant stable et snapshot intacts.
        var relu = Assert.Single(store2.AllSnapshots());
        Assert.Equal(idStable, relu.Id);
        Assert.Equal("lea", relu.EnfantId);
        Assert.Equal("piscine", relu.LieuId);
        Assert.Equal(DayOfWeek.Saturday, relu.JourDeSemaine);
        Assert.Equal(new TimeSpan(11, 30, 0), relu.HeureDebut);
        Assert.Equal(new TimeSpan(12, 15, 0), relu.HeureFin);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
