using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du référentiel de rôles du foyer — réalise, derrière
/// les <b>ports inchangés</b>, la lecture (<see cref="IEnumerationRoles"/>) et l'écriture
/// (<see cref="IEditeurReferentielRoles"/>). Remplaçant durable de <c>ReferentielRolesEnMemoire</c> :
/// un rôle créé survit au redémarrage du serveur (une instance fraîche = un redémarrage relit l'état
/// persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « roles ». Aucun seed (comme la config acteurs, s15) : la base ouvre vide et se
/// peuple par les écritures write-through. La clé est l'identifiant stable opaque, jamais le libellé.</para>
/// </summary>
public sealed class ReferentielRolesMongo : IEnumerationRoles, IEditeurReferentielRoles
{
    private readonly IMongoCollection<RoleDocument> _roles;
    private readonly Dictionary<string, RoleDocument> _cache;

    public ReferentielRolesMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _roles = db.GetCollection<RoleDocument>("roles");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _roles.Find(Builders<RoleDocument>.Filter.Empty).ToList().ToDictionary(d => d.Id);
    }

    public void Creer(string roleId, string libelle)
        => Persister(roleId, libelle);

    public void Renommer(string roleId, string nouveauLibelle)
        => Persister(roleId, nouveauLibelle); // même id (clé) → aucun doublon, dernière écriture gagne

    public void Supprimer(string roleId)
    {
        // Retrait write-through : cache de session ET store durable (le rôle ne réapparaît pas au
        // redémarrage). Tolérant à l'absence (no-op sur clé/document absent) — idempotence Sc.6.
        _cache.Remove(roleId);
        _roles.DeleteOne(Builders<RoleDocument>.Filter.Eq(d => d.Id, roleId));
    }

    /// <summary>Écrit le libellé du rôle sur son id stable (write-through Mongo, upsert) — survit
    /// au redémarrage. Le même id reste un unique document (jamais de doublon).</summary>
    private void Persister(string roleId, string libelle)
    {
        var doc = new RoleDocument { Id = roleId, Libelle = libelle };
        _cache[roleId] = doc;
        _roles.ReplaceOne(
            Builders<RoleDocument>.Filter.Eq(d => d.Id, roleId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public IReadOnlyCollection<RoleFoyer> EnumererRoles()
        => _cache.Values.Select(d => new RoleFoyer(d.Id, d.Libelle)).ToList();

    /// <summary>Document persisté d'un rôle du foyer : identifiant stable (clé) et libellé.</summary>
    private sealed class RoleDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
        public string Libelle { get; set; } = default!;
    }
}
