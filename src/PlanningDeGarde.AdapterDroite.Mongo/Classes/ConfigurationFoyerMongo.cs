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
/// <para><b>Seed-once</b> (cœur du pivot, inversion exacte de la volatilité assumée) : au démarrage,
/// le store est seedé depuis le <see cref="Foyer"/> <b>seulement si la collection est vide</b> ;
/// sinon l'état persisté est relu tel quel, <b>sans re-seeder par-dessus les éditions</b> — un
/// re-seed à chaque démarrage écraserait Alicia → Alice et supprimerait les acteurs ajoutés.</para>
///
/// <para><b>Borne anti-cliquet (règle 30)</b> : SEULE la config foyer est durable ; slots / périodes
/// / transferts restent InMemory. Les lectures servent un cache mémoire chargé à la construction
/// (une instance fraîche = un redémarrage : elle relit l'état persisté) ; les écritures sont
/// répercutées (write-through) sur Mongo.</para>
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

        var tous = Builders<ActeurDocument>.Filter.Empty;

        // Seed-once : amorce depuis le Foyer SEULEMENT si le store durable est vide ; sinon relit
        // l'état persisté (jamais de re-seed par-dessus les éditions — principale surface de bug).
        if (_acteurs.CountDocuments(tous) == 0)
            _acteurs.InsertMany(SeedDepuisFoyer());

        _cache = _acteurs.Find(tous).ToList().ToDictionary(d => d.Id);
    }

    private static IEnumerable<ActeurDocument> SeedDepuisFoyer()
    {
        // Union des acteurs portés par un nom ET/OU une couleur (parité exacte avec le store mémoire :
        // un nom absent retombe sur l'id, une couleur absente sur la teinte neutre par contrat).
        var ids = Foyer.NomsParResponsable.Keys.Union(Foyer.CouleursParActeur.Keys);
        return ids.Select(id => new ActeurDocument
        {
            Id = id,
            Nom = Foyer.NomsParResponsable.TryGetValue(id, out var nom) ? nom : null,
            Couleur = Foyer.CouleursParActeur.TryGetValue(id, out var couleur) ? couleur : null,
        });
    }

    public string NomDe(string responsableId)
        => _cache.TryGetValue(responsableId, out var doc) && doc.Nom is not null ? doc.Nom : responsableId;

    public string CouleurNeutre => Foyer.CouleurNeutre;

    public string CouleurDe(string acteurId)
        => _cache.TryGetValue(acteurId, out var doc) && doc.Couleur is not null ? doc.Couleur : Foyer.CouleurNeutre;

    public IReadOnlyCollection<string> EnumererActeurs()
        // Acteurs porteurs d'un nom (seeds + ajoutés) — parité avec le store mémoire (_noms.Keys).
        => _cache.Values.Where(d => d.Nom is not null).Select(d => d.Id).ToList();

    public void Ajouter(string acteurId, string nom, string? couleur)
        => Persister(acteurId, doc => { doc.Nom = nom; if (couleur is not null) doc.Couleur = couleur; });

    public void Renommer(string acteurId, string nouveauNom)
        => Persister(acteurId, doc => doc.Nom = nouveauNom); // dernière écriture gagne

    public void Recolorier(string acteurId, string nouvelleCouleur)
        => Persister(acteurId, doc => doc.Couleur = nouvelleCouleur); // surface distincte du nom

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
    }
}
