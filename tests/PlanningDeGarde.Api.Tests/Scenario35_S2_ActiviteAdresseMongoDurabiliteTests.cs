using System;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 35 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : une doublure « mentirait au vert », R4). L'adresse d'une activité, changée via le handler
/// câblé sur le store durable <see cref="ReferentielActivitesMongo"/>, doit être <b>relue après
/// redémarrage</b> (nouvelle instance de store sur la même base) — preuve que l'écriture a atteint le
/// store durable (write-through), pas seulement un cache de session. Miroir strict de l'adresse acteur s33.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class Scenario35_S2_ActiviteAdresseMongoDurabiliteTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielActivitesMongo NouveauStore() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Relire_l_adresse_apres_redemarrage_sans_toucher_le_libelle_When_l_adresse_a_ete_changee_sur_le_store_Mongo_reel()
    {
        // --- Serveur #1 : le parent ajoute « piscine » puis lui donne une adresse (write-through durable) ---
        {
            var store1 = NouveauStore();
            store1.Ajouter("piscine", "piscine");
            new EditerActiviteHandler(store1).Handle(new EditerActiviteCommand("piscine", Adresse: "3 allée du Bassin"));
        }

        // --- Redémarrage : NOUVELLE instance de store sur la MÊME base Mongo persistée ---
        var store2 = NouveauStore();

        // Then — l'adresse est relue après redémarrage, le libellé reste intact (aucune écriture partielle).
        var activite = store2.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal("3 allée du Bassin", activite.Adresse);
        Assert.Equal("piscine", activite.Libelle);
    }

    [MongoRequisFact]
    public void Should_Accepter_une_adresse_vide_durable_When_l_adresse_est_videe_sur_le_store_Mongo_reel()
    {
        {
            var store1 = NouveauStore();
            store1.Ajouter("piscine", "piscine");
            var handler = new EditerActiviteHandler(store1);
            handler.Handle(new EditerActiviteCommand("piscine", Adresse: "3 allée du Bassin"));
            handler.Handle(new EditerActiviteCommand("piscine", Adresse: "")); // vidage licite
        }

        var store2 = NouveauStore();

        var activite = store2.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal("", activite.Adresse);        // adresse vide relue telle quelle (champ optionnel)
        Assert.Equal("piscine", activite.Libelle); // libellé toujours intact
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
