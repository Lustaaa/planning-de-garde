using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) des admins du foyer — réalise, derrière les <b>ports
/// inchangés</b>, la lecture (<see cref="IEnumerationAdminsFoyer"/>) et l'écriture
/// (<see cref="IEditeurAdminsFoyer"/>). Remplaçant durable de <c>AdminsFoyerEnMemoire</c> : une
/// désignation d'admin survit au redémarrage du serveur (une instance fraîche = un redémarrage relit
/// l'état persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « admins ». Aucun seed : la base ouvre vide et se peuple par les écritures
/// write-through. La clé est l'id stable de l'acteur admin. L'invariant admin=parent est porté par
/// l'agrégat Domain, jamais par le store.</para>
/// </summary>
public sealed class AdminsFoyerMongo : IEnumerationAdminsFoyer, IEditeurAdminsFoyer
{
    private readonly IMongoCollection<AdminDocument> _admins;
    private readonly HashSet<string> _cache;

    public AdminsFoyerMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _admins = db.GetCollection<AdminDocument>("admins");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _admins.Find(Builders<AdminDocument>.Filter.Empty).ToList().Select(d => d.Id).ToHashSet();
    }

    public void DesignerAdmin(string acteurId)
    {
        // Écriture write-through : cache de session ET store durable (l'admin réapparaît au
        // redémarrage). Idempotent : le même id reste un unique document.
        _cache.Add(acteurId);
        _admins.ReplaceOne(
            Builders<AdminDocument>.Filter.Eq(d => d.Id, acteurId),
            new AdminDocument { Id = acteurId },
            new ReplaceOptions { IsUpsert = true });
    }

    public void DeDesignerAdmin(string acteurId)
    {
        // Retrait write-through (sens OFF, s41) : cache de session ET store durable (l'acteur reste
        // non-admin au redémarrage). Idempotent : retirer un id absent est un no-op. La borne « dernier
        // admin » est validée par l'agrégat AVANT cet appel (le store ne persiste qu'une décision tenue).
        _cache.Remove(acteurId);
        _admins.DeleteOne(Builders<AdminDocument>.Filter.Eq(d => d.Id, acteurId));
    }

    public IReadOnlyCollection<string> EnumererAdmins() => _cache.ToList();

    /// <summary>Document persisté d'un admin du foyer : id stable de l'acteur (clé).</summary>
    private sealed class AdminDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
    }
}
