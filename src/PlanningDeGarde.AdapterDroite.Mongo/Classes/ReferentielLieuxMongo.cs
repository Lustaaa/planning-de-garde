using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du référentiel de lieux du foyer (s27) — réalise,
/// derrière les <b>ports inchangés</b>, la lecture (<see cref="IEnumerationLieux"/>) et l'écriture
/// (<see cref="IEditeurLieux"/>). Remplaçant durable de <c>ReferentielLieuxEnMemoire</c> : un lieu
/// ajouté survit au redémarrage du serveur (une instance fraîche = un redémarrage relit l'état
/// persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « lieux ». <b>Aucun seed</b> (parité asymétrie seed s15, comme rôles/config
/// acteurs) : la base ouvre vide et se peuple par les écritures write-through — l'InMemory conserve
/// son seed pour la non-régression, jamais le Mongo. La clé est l'identifiant stable du lieu.</para>
/// </summary>
public sealed class ReferentielLieuxMongo : IEnumerationLieux, IEditeurLieux
{
    private readonly IMongoCollection<LieuDocument> _lieux;
    private readonly Dictionary<string, LieuDocument> _cache;

    public ReferentielLieuxMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _lieux = db.GetCollection<LieuDocument>("lieux");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _lieux.Find(Builders<LieuDocument>.Filter.Empty).ToList().ToDictionary(d => d.Id);
    }

    public void Ajouter(string lieuId, string libelle)
    {
        // Écriture write-through : cache de session ET store durable (upsert sur l'id stable) — le lieu
        // survit au redémarrage. Le même id reste un unique document (jamais de doublon).
        var doc = new LieuDocument { Id = lieuId, Libelle = libelle };
        _cache[lieuId] = doc;
        _lieux.ReplaceOne(
            Builders<LieuDocument>.Filter.Eq(d => d.Id, lieuId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public void Supprimer(string lieuId)
    {
        // Retrait write-through : cache de session ET store durable (le lieu ne réapparaît pas au
        // redémarrage). Tolérant à l'absence (no-op sur clé/document absent) — idempotence.
        _cache.Remove(lieuId);
        _lieux.DeleteOne(Builders<LieuDocument>.Filter.Eq(d => d.Id, lieuId));
    }

    public IReadOnlyCollection<LieuFoyer> EnumererLieux()
        => _cache.Values.Select(d => new LieuFoyer(d.Id, d.Libelle)).ToList();

    /// <summary>Document persisté d'un lieu du foyer : identifiant stable (clé) et libellé.</summary>
    private sealed class LieuDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
        public string Libelle { get; set; } = default!;
    }
}
