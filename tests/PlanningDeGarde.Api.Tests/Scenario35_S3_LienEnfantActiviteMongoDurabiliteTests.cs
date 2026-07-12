using System;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 35 — Sc.3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure). Le lien enfant↔activité (N-M), posé via le store durable <see cref="ReferentielActivitesMongo"/>,
/// doit être <b>relu après redémarrage</b> (nouvelle instance de store sur la même base) — preuve que
/// l'écriture write-through a atteint le store durable, pas seulement un cache de session. Le délier
/// durable est prouvé de même. Miroir du lien enfant↔parent s34.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class Scenario35_S3_LienEnfantActiviteMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielActivitesMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Relire_les_liens_N_M_apres_redemarrage_When_des_enfants_sont_lies_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : deux enfants liés à « piscine » (N-M), write-through durable ---
        {
            var store1 = NouveauStore();
            store1.Ajouter("piscine", "piscine");
            store1.LierEnfant("piscine", "lea");
            store1.LierEnfant("piscine", "noa");
            store1.LierEnfant("piscine", "lea"); // déjà lié = no-op (aucun doublon)
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        var activite = store2.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal(new[] { "lea", "noa" }, activite.EnfantsLies.OrderBy(x => x)); // liens relus, sans doublon
        Assert.Equal("piscine", activite.Libelle); // libellé intact (lien ne touche pas les autres champs)
    }

    [MongoRequisFact]
    public void Should_Relire_le_delien_apres_redemarrage_When_un_enfant_est_delie_sur_le_store_Mongo_reel()
    {
        {
            var store1 = NouveauStore();
            store1.Ajouter("piscine", "piscine");
            store1.LierEnfant("piscine", "lea");
            store1.LierEnfant("piscine", "noa");
            store1.DelierEnfant("piscine", "lea"); // retrait durable
        }

        var store2 = NouveauStore();

        var activite = store2.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal(new[] { "noa" }, activite.EnfantsLies); // seul « noa » subsiste après redémarrage
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
