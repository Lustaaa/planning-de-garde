using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) de la configuration du foyer (référentiel acteurs :
/// noms, couleurs, acteurs ajoutés). Réalise — derrière les <b>ports inchangés</b> — la lecture
/// (<see cref="IReferentielResponsables"/> / <see cref="IPaletteCouleurs"/> /
/// <see cref="IEnumerationActeursFoyer"/>) et l'écriture (<see cref="IEditeurConfigurationFoyer"/>).
/// Remplaçant <b>durable</b> de <see cref="ConfigurationFoyerEnMemoire"/> : l'ajout comme l'édition
/// survivent au redémarrage du serveur (pivot Sc.3), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Aucun seed</b> (Sc.8, s15 — inversion exacte du seed-once s09) : Mongo ne s'amorce
/// <b>jamais</b>, même vide. Au tout premier lancement sur une base vierge, l'application ouvre
/// totalement vide (aucun acteur) ; dès qu'on saisit, c'est durable et rechargé aux lancements
/// suivants. <b>Asymétrie assumée</b> : seul l'InMemory garde son seed (pour la non-régression).</para>
///
/// <para><b>Lecture / écriture</b> : les lectures servent un cache mémoire chargé à la construction
/// (une instance fraîche = un redémarrage : elle relit l'état persisté, vide au premier lancement) ;
/// les écritures sont répercutées (write-through) sur Mongo.</para>
/// </summary>
public sealed class ConfigurationFoyerMongo : IReferentielResponsables, IEditeurConfigurationFoyer, IPaletteCouleurs, IEnumerationActeursFoyer
{
    private readonly IMongoCollection<ActeurDocument> _acteurs;
    private readonly Dictionary<string, ActeurDocument> _cache;

    public ConfigurationFoyerMongo(string connectionString, string baseDeDonnees)
    {
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(baseDeDonnees);
        _acteurs = db.GetCollection<ActeurDocument>("acteurs");

        // AUCUN seed (Sc.8, s15) : on ne relit que l'état persisté tel quel — vide au tout premier
        // lancement (l'application ouvre totalement vide), peuplé ensuite par les écritures write-through.
        var tous = Builders<ActeurDocument>.Filter.Empty;
        _cache = _acteurs.Find(tous).ToList().ToDictionary(d => d.Id);
    }

    public string NomDe(string responsableId)
        => _cache.TryGetValue(responsableId, out var doc) && doc.Nom is not null ? doc.Nom : responsableId;

    public string CouleurNeutre => Foyer.CouleurNeutre;

    public string CouleurDe(string acteurId)
        => _cache.TryGetValue(acteurId, out var doc) && doc.Couleur is not null ? doc.Couleur : Foyer.CouleurNeutre;

    public IReadOnlyCollection<string> EnumererActeurs()
        // Acteurs porteurs d'un nom (seeds + ajoutés) — parité avec le store mémoire (_noms.Keys).
        => _cache.Values.Where(d => d.Nom is not null).Select(d => d.Id).ToList();

    public TypeActeur TypeDe(string acteurId)
        // Type surfacé en lecture seule depuis la déclaration seed (D3) — JAMAIS persisté (borne
        // anti-cliquet règle 30) : aucun champ type au document Mongo. Un acteur ajouté en session
        // (absent du seed de types) retombe sur le défaut Parent.
        => Foyer.TypesParActeur.TryGetValue(acteurId, out var type) ? type : Foyer.TypeParDefaut;

    public void Ajouter(string acteurId, string nom, string? couleur)
        => Persister(acteurId, doc => { doc.Nom = nom; if (couleur is not null) doc.Couleur = couleur; });

    public void Renommer(string acteurId, string nouveauNom)
        => Persister(acteurId, doc => doc.Nom = nouveauNom); // dernière écriture gagne

    public void Recolorier(string acteurId, string nouvelleCouleur)
        => Persister(acteurId, doc => doc.Couleur = nouvelleCouleur); // surface distincte du nom

    public void ChangerAdresse(string acteurId, string adresse)
        => Persister(acteurId, doc => doc.Adresse = adresse); // surface optionnelle durable (write-through, vide licite)

    /// <summary>Adresse de résidence persistée de l'acteur, ou <c>null</c> s'il n'en porte aucune.</summary>
    public string? AdresseDe(string acteurId)
        => _cache.TryGetValue(acteurId, out var doc) ? doc.Adresse : null;

    public void AffecterRole(string acteurId, string roleId)
        => Persister(acteurId, doc => doc.RoleId = roleId); // id de rôle porté par l'acteur (write-through durable)

    public void RetirerRole(string acteurId)
    {
        // « sans rôle » (neutre) write-through : ne mute que si l'acteur existe (tolérant à l'absence,
        // aucun rôle fantôme, aucun document créé pour un acteur inconnu).
        if (_cache.ContainsKey(acteurId))
            Persister(acteurId, d => d.RoleId = null);
    }

    /// <summary>Id de rôle porté par l'acteur, ou <c>null</c> s'il n'en porte aucun (« sans rôle »).</summary>
    public string? RoleDe(string acteurId)
        => _cache.TryGetValue(acteurId, out var doc) ? doc.RoleId : null;

    public void Supprimer(string acteurId)
    {
        // Retrait write-through : le cache de session ET le store durable (l'acteur ne réapparaît pas
        // au redémarrage). Tolérant à l'absence (no-op sur clé/document absent) — idempotence Sc.5.
        _cache.Remove(acteurId);
        _acteurs.DeleteOne(Builders<ActeurDocument>.Filter.Eq(d => d.Id, acteurId));
    }

    /// <summary>Mute le document de l'acteur (créé si absent) puis le répercute sur Mongo (write-through)
    /// — l'écriture survit au redémarrage (une instance fraîche relira l'état persisté).</summary>
    private void Persister(string acteurId, System.Action<ActeurDocument> mutation)
    {
        if (!_cache.TryGetValue(acteurId, out var doc))
        {
            doc = new ActeurDocument { Id = acteurId };
            _cache[acteurId] = doc;
        }
        mutation(doc);
        _acteurs.ReplaceOne(
            Builders<ActeurDocument>.Filter.Eq(d => d.Id, acteurId),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    /// <summary>Document persisté d'un acteur du foyer : identifiant stable (clé), nom et couleur
    /// d'affichage (chacun optionnel — résolus sur l'id, jamais sur le libellé).</summary>
    private sealed class ActeurDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
        public string? Nom { get; set; }
        public string? Couleur { get; set; }
        public string? RoleId { get; set; } // id de rôle porté par l'acteur (null = « sans rôle »)
        public string? Adresse { get; set; } // adresse de résidence (null = non renseignée, optionnelle)
    }
}
