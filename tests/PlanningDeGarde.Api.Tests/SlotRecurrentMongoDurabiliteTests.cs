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

    // Sprint 54 — S4 — durabilité du SET de jours (multi-jours) : un récurrent multi-jours
    // (École {lundi, mardi, jeudi, vendredi}) enregistré sur le store durable survit au redémarrage
    // avec son set de jours intact (parité mono-jour). Prouvé contre un Mongo réel.
    [MongoRequisFact]
    public void Should_Relire_le_set_de_jours_intact_apres_un_redemarrage_When_un_recurrent_multi_jours_est_enregistre_sur_le_store_Mongo_reel()
    {
        var jours = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        {
            var store1 = NouveauStore();
            store1.Enregistrer(SlotRecurrent
                .Poser("lea", "ecole", jours, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);
        }

        var store2 = NouveauStore();

        var relu = Assert.Single(store2.AllSnapshots());
        Assert.Equal("lea", relu.EnfantId);
        Assert.Equal("ecole", relu.LieuId);
        Assert.Equal(jours, relu.JoursDeSemaine);
    }

    // Sprint 54 — S7 — durabilité des PLAGES D'EXCLUSION (vacances) : une série avec une plage d'exclusion
    // rattachée survit au redémarrage avec sa plage intacte. Prouvé contre un Mongo réel.
    [MongoRequisFact]
    public void Should_Relire_la_plage_d_exclusion_intacte_apres_un_redemarrage_When_un_recurrent_avec_exclusion_est_enregistre_sur_le_store_Mongo_reel()
    {
        var exclusion = new PlageExclusion(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 5));
        {
            var store1 = NouveauStore();
            var slot = SlotRecurrent.Poser("lea", "ecole", new[] { DayOfWeek.Monday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!
                .AjouterExclusion(exclusion.Debut, exclusion.Fin);
            store1.Enregistrer(slot);
        }

        var store2 = NouveauStore();

        var relu = Assert.Single(store2.AllSnapshots());
        Assert.Contains(exclusion, relu.Exclusions);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
