using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Comptes.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du référentiel des comptes utilisateurs du foyer —
/// réalise, derrière les <b>ports inchangés</b>, la lecture (<see cref="IEnumerationComptes"/>) et
/// l'écriture (<see cref="IEditeurComptes"/>). Remplaçant durable de
/// <c>ReferentielComptesEnMemoire</c> : un compte créé survit au redémarrage du serveur (une instance
/// fraîche = un redémarrage relit l'état persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « comptes ». Aucun seed (comme la config acteurs) : la base ouvre vide et
/// se peuple par les écritures write-through. La clé est l'identifiant stable opaque, jamais l'email.</para>
/// </summary>
public sealed class ReferentielComptesMongo : IEnumerationComptes, IEditeurComptes
{
    private readonly IMongoCollection<CompteDocument> _comptes;
    private readonly Dictionary<string, CompteDocument> _cache;

    public ReferentielComptesMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _comptes = db.GetCollection<CompteDocument>("comptes");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _comptes.Find(Builders<CompteDocument>.Filter.Empty).ToList().ToDictionary(d => d.Id);
    }

    public void Creer(string compteId, string email, StatutCompte statut, string? acteurId, string? motDePasseHache = null)
    {
        // Écriture write-through : cache de session ET store durable (le compte réapparaît au
        // redémarrage). Le même id reste un unique document (jamais de doublon).
        var doc = new CompteDocument
        {
            Id = compteId,
            Email = email,
            Statut = statut,
            ActeurId = acteurId,
            MotDePasseHache = motDePasseHache
        };
        _cache[compteId] = doc;
        _comptes.ReplaceOne(
            Builders<CompteDocument>.Filter.Eq(d => d.Id, compteId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public void Desassocier(string compteId)
    {
        // Repli propre write-through : le compte survit, sans acteur (ActeurId null) — cache de session
        // ET store durable (il reste désassocié au redémarrage, pas de compte fantôme). Tolérant à
        // l'absence / à un compte déjà désassocié (no-op qui réussit — idempotence Sc.6).
        if (!_cache.TryGetValue(compteId, out var doc))
            return;
        doc.ActeurId = null;
        _cache[compteId] = doc;
        _comptes.ReplaceOne(
            Builders<CompteDocument>.Filter.Eq(d => d.Id, compteId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public void Activer(string compteId)
    {
        // Mutation ciblée du statut write-through : cache de session ET store durable (le compte reste
        // Actif au redémarrage). La règle métier est portée par l'agrégat (Activer()). Tolérant à
        // l'absence (no-op). Email et acteur associé inchangés.
        if (!_cache.TryGetValue(compteId, out var doc))
            return;
        var compteActif = new CompteUtilisateur(doc.Id, doc.Email, doc.Statut, doc.ActeurId, doc.MotDePasseHache).Activer();
        doc.Statut = compteActif.Statut;
        _cache[compteId] = doc;
        _comptes.ReplaceOne(
            Builders<CompteDocument>.Filter.Eq(d => d.Id, compteId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public void Desactiver(string compteId)
    {
        // Sens OFF s41 : mutation ciblée du statut write-through — cache de session ET store durable (le
        // compte reste Inactif au redémarrage). La règle métier est portée par l'agrégat (Desactiver()).
        // Tolérant à l'absence / à un compte déjà Inactif (no-op). Email et acteur associé inchangés.
        if (!_cache.TryGetValue(compteId, out var doc))
            return;
        var compteInactif = new CompteUtilisateur(doc.Id, doc.Email, doc.Statut, doc.ActeurId, doc.MotDePasseHache).Desactiver();
        doc.Statut = compteInactif.Statut;
        _cache[compteId] = doc;
        _comptes.ReplaceOne(
            Builders<CompteDocument>.Filter.Eq(d => d.Id, compteId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public void RedefinirMotDePasse(string compteId, string motDePasseHache)
    {
        // Mutation ciblée du seul mot de passe write-through (récupération, s25) : cache de session ET
        // store durable (le nouveau condensat survit au redémarrage). Tolérant à l'absence (no-op).
        if (!_cache.TryGetValue(compteId, out var doc))
            return;
        doc.MotDePasseHache = motDePasseHache;
        _cache[compteId] = doc;
        _comptes.ReplaceOne(
            Builders<CompteDocument>.Filter.Eq(d => d.Id, compteId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public IReadOnlyCollection<CompteUtilisateur> EnumererComptes()
        => _cache.Values.Select(d => new CompteUtilisateur(d.Id, d.Email, d.Statut, d.ActeurId, d.MotDePasseHache)).ToList();
}
