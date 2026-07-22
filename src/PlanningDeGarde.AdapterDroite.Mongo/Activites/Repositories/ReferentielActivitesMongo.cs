using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Activites.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du référentiel de lieux du foyer — réalise,
/// derrière les <b>ports inchangés</b>, la lecture (<see cref="IEnumerationActivites"/>) et l'écriture
/// (<see cref="IEditeurActivites"/>). Remplaçant durable de <c>ReferentielActivitesEnMemoire</c> : un lieu
/// ajouté survit au redémarrage du serveur (une instance fraîche = un redémarrage relit l'état
/// persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « lieux ». <b>Aucun seed</b> (parité asymétrie seed, comme rôles/config
/// acteurs) : la base ouvre vide et se peuple par les écritures write-through — l'InMemory conserve
/// son seed pour la non-régression, jamais le Mongo. La clé est l'identifiant stable du lieu.</para>
/// </summary>
public sealed class ReferentielActivitesMongo : IEnumerationActivites, IEditeurActivites
{
    private readonly IMongoCollection<ActiviteDocument> _lieux;
    private readonly Dictionary<string, ActiviteDocument> _cache;

    public ReferentielActivitesMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _lieux = db.GetCollection<ActiviteDocument>("lieux");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _lieux.Find(Builders<ActiviteDocument>.Filter.Empty).ToList().ToDictionary(d => d.Id);
    }

    public void Ajouter(string lieuId, string libelle)
    {
        // Écriture write-through : cache de session ET store durable (upsert sur l'id stable) — le lieu
        // survit au redémarrage. Le même id reste un unique document (jamais de doublon).
        var doc = new ActiviteDocument { Id = lieuId, Libelle = libelle };
        _cache[lieuId] = doc;
        _lieux.ReplaceOne(
            Builders<ActiviteDocument>.Filter.Eq(d => d.Id, lieuId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public void Supprimer(string lieuId)
    {
        // Retrait write-through : cache de session ET store durable (le lieu ne réapparaît pas au
        // redémarrage). Tolérant à l'absence (no-op sur clé/document absent) — idempotence.
        _cache.Remove(lieuId);
        _lieux.DeleteOne(Builders<ActiviteDocument>.Filter.Eq(d => d.Id, lieuId));
    }

    public void Renommer(string activiteId, string libelle)
    {
        // Mise à jour write-through du SEUL champ libellé ($set ciblé) — l'adresse persistée n'est PAS
        // touchée (aucune écriture partielle croisée, s35 Sc.2). Cache de session aligné.
        _lieux.UpdateOne(
            Builders<ActiviteDocument>.Filter.Eq(d => d.Id, activiteId),
            Builders<ActiviteDocument>.Update.Set(d => d.Libelle, libelle));
        if (_cache.TryGetValue(activiteId, out var doc))
            doc.Libelle = libelle;
    }

    public void ChangerAdresse(string activiteId, string adresse)
    {
        // Mise à jour write-through du SEUL champ adresse ($set ciblé) — le libellé persisté n'est PAS
        // touché (aucune écriture partielle, s35 Sc.2 ; miroir strict de l'adresse acteur s33). L'adresse
        // survit au redémarrage ; une adresse vide est une valeur licite écrite telle quelle.
        _lieux.UpdateOne(
            Builders<ActiviteDocument>.Filter.Eq(d => d.Id, activiteId),
            Builders<ActiviteDocument>.Update.Set(d => d.Adresse, adresse));
        if (_cache.TryGetValue(activiteId, out var doc))
            doc.Adresse = adresse;
    }

    public void LierEnfant(string activiteId, string enfantId)
    {
        // Lien N-M enfant↔activité (s35 Sc.3) : ajoute l'enfant aux enfants liés (sans doublon), write-through
        // du SEUL champ EnfantsLies ($set ciblé) — libellé et adresse persistés NON touchés. Enrichissement
        // (l'id de l'activité reste la clé) : le lien survit au redémarrage. Tolérant à l'activité absente du
        // cache (crée la liste) — le handler garantit l'existence en amont.
        var lies = _cache.TryGetValue(activiteId, out var doc) ? new List<string>(doc.EnfantsLies) : new List<string>();
        if (lies.Contains(enfantId)) return; // déjà lié = no-op (aucune écriture, aucun doublon)
        lies.Add(enfantId);
        _lieux.UpdateOne(
            Builders<ActiviteDocument>.Filter.Eq(d => d.Id, activiteId),
            Builders<ActiviteDocument>.Update.Set(d => d.EnfantsLies, lies));
        if (doc is not null) doc.EnfantsLies = lies;
    }

    public void DelierEnfant(string activiteId, string enfantId)
    {
        // Retrait du lien (s35 Sc.3) : tolérant à l'absence (enfant déjà non lié = no-op qui réussit).
        // Write-through du SEUL champ EnfantsLies — autres liens, libellé et adresse NON touchés.
        if (!_cache.TryGetValue(activiteId, out var doc) || !doc.EnfantsLies.Contains(enfantId)) return;
        var lies = new List<string>(doc.EnfantsLies);
        lies.Remove(enfantId);
        _lieux.UpdateOne(
            Builders<ActiviteDocument>.Filter.Eq(d => d.Id, activiteId),
            Builders<ActiviteDocument>.Update.Set(d => d.EnfantsLies, lies));
        doc.EnfantsLies = lies;
    }

    public IReadOnlyCollection<ActiviteFoyer> EnumererActivites()
        => _cache.Values.Select(d => new ActiviteFoyer(d.Id, d.Libelle, d.Adresse ?? "")
        {
            EnfantsLies = d.EnfantsLies.ToList()
        }).ToList();
}
