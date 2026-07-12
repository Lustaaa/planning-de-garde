using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du référentiel d'enfants du foyer (s30) — réalise,
/// derrière les <b>ports inchangés</b>, la lecture (<see cref="IEnumerationEnfants"/>) et l'écriture
/// (<see cref="IEditeurEnfants"/>). Remplaçant durable de <c>ReferentielEnfantsEnMemoire</c> : un enfant
/// ajouté/édité survit au redémarrage du serveur (une instance fraîche = un redémarrage relit l'état
/// persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « enfants ». <b>Aucun seed</b> (parité asymétrie seed s15, comme lieux/rôles/config
/// acteurs) : la base ouvre vide et se peuple par les écritures write-through — l'InMemory conserve son
/// seed pour la non-régression, jamais le Mongo. La clé est l'identifiant stable opaque de l'enfant.</para>
/// </summary>
public sealed class ReferentielEnfantsMongo : IEnumerationEnfants, IEditeurEnfants
{
    private readonly IMongoCollection<EnfantDocument> _enfants;
    private readonly Dictionary<string, EnfantDocument> _cache;

    public ReferentielEnfantsMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _enfants = db.GetCollection<EnfantDocument>("enfants");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _enfants.Find(Builders<EnfantDocument>.Filter.Empty).ToList().ToDictionary(d => d.Id);
    }

    public void Ajouter(string enfantId, string prenom) => EcrireWriteThrough(enfantId, prenom);

    public void Editer(string enfantId, string nouveauPrenom) => EcrireWriteThrough(enfantId, nouveauPrenom);

    private void EcrireWriteThrough(string enfantId, string prenom)
    {
        // Écriture write-through : cache de session ET store durable (upsert sur l'id stable) — l'enfant
        // survit au redémarrage. Le même id reste un unique document (jamais de doublon) ; l'édition écrase
        // le prénom sur la même clé (dernière écriture gagne). Les parents liés déjà persistés sont conservés.
        var parents = _cache.TryGetValue(enfantId, out var existant) ? existant.ParentsLies : new List<string>();
        Persister(new EnfantDocument { Id = enfantId, Prenom = prenom, ParentsLies = parents });
    }

    public void LierParent(string enfantId, string acteurId)
    {
        // Enrichissement durable : on ajoute le parent-acteur à la liste des parents liés de l'enfant
        // (id de l'enfant inchangé, prénom conservé), write-through sur le store durable. Aucun doublon.
        var doc = _cache.TryGetValue(enfantId, out var existant)
            ? new EnfantDocument { Id = existant.Id, Prenom = existant.Prenom, ParentsLies = new List<string>(existant.ParentsLies) }
            : new EnfantDocument { Id = enfantId, Prenom = string.Empty, ParentsLies = new List<string>() };
        if (!doc.ParentsLies.Contains(acteurId))
            doc.ParentsLies.Add(acteurId);
        Persister(doc);
    }

    public void DelierParent(string enfantId, string acteurId)
    {
        // Retrait DURABLE du lien (id de l'enfant et prénom inchangés, autres liens conservés). Idempotent :
        // un enfant absent ou un parent déjà non lié → aucune écriture (write-through seulement si mutation).
        if (!_cache.TryGetValue(enfantId, out var existant) || !existant.ParentsLies.Contains(acteurId))
            return;
        var doc = new EnfantDocument { Id = existant.Id, Prenom = existant.Prenom, ParentsLies = new List<string>(existant.ParentsLies) };
        doc.ParentsLies.Remove(acteurId);
        Persister(doc);
    }

    private void Persister(EnfantDocument doc)
    {
        _cache[doc.Id] = doc;
        _enfants.ReplaceOne(
            Builders<EnfantDocument>.Filter.Eq(d => d.Id, doc.Id),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public IReadOnlyCollection<EnfantFoyer> EnumererEnfants()
        => _cache.Values.Select(d => new EnfantFoyer(d.Id, d.Prenom, d.ParentsLies.ToList())).ToList();

    /// <summary>Document persisté d'un enfant du foyer : identifiant stable opaque (clé), prénom et la
    /// liste des identifiants stables de ses parents-acteurs liés (0..2, s34).</summary>
    private sealed class EnfantDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
        public string Prenom { get; set; } = default!;
        public List<string> ParentsLies { get; set; } = new();
    }
}
