using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Comptes.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du stockage serveur des jetons de réinitialisation de
/// mot de passe — réalise, derrière le <b>port inchangé</b>
/// <see cref="IReferentielJetonsReset"/>, l'émission (<c>Enregistrer</c>), la relecture
/// (<c>Trouver</c>) et la consommation usage-unique (<c>Consommer</c>). Remplaçant durable de la
/// doublure : un jeton émis survit au redémarrage du serveur (une instance fraîche relit l'état
/// persisté), et sa consommation est durable — un jeton consommé une fois ne redevient jamais valide.
/// Prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à l'auth</b> : même socle Mongo, collection dédiée « jetons_reset ». Aucun seed.
/// La clé est la valeur opaque du jeton (jamais l'email ni l'id du compte). Chaque lecture interroge
/// directement le store durable (pas de cache) — la relecture reflète toujours l'état persisté.</para>
/// </summary>
public sealed class ReferentielJetonsResetMongo : IReferentielJetonsReset
{
    private readonly IMongoCollection<JetonDocument> _jetons;

    public ReferentielJetonsResetMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _jetons = db.GetCollection<JetonDocument>("jetons_reset");
    }

    public void Enregistrer(JetonReset jeton)
    {
        // Émission write-through : le jeton est persisté (upsert sur sa valeur opaque → jamais de
        // doublon) et survit au redémarrage. L'expiration est stockée en UTC.
        var doc = new JetonDocument
        {
            Jeton = jeton.Jeton,
            CompteId = jeton.CompteId,
            Expiration = jeton.Expiration,
            Consomme = jeton.Consomme,
        };
        _jetons.ReplaceOne(
            Builders<JetonDocument>.Filter.Eq(d => d.Jeton, jeton.Jeton),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public JetonReset? Trouver(string jeton)
    {
        // Relecture directe du store durable (aucun cache) : reflète toujours l'état persisté, y compris
        // la consommation. L'expiration relue est en UTC (comparée à l'horloge injectée côté handler).
        var doc = _jetons.Find(Builders<JetonDocument>.Filter.Eq(d => d.Jeton, jeton)).FirstOrDefault();
        return doc is null
            ? null
            : new JetonReset(doc.Jeton, doc.CompteId, doc.Expiration, doc.Consomme);
    }

    public void Consommer(string jeton)
    {
        // Consommation durable (usage unique) : le drapeau passe à vrai dans le store persisté — un jeton
        // consommé ne redevient jamais valide, même après redémarrage. Tolérant à l'absence (no-op).
        _jetons.UpdateOne(
            Builders<JetonDocument>.Filter.Eq(d => d.Jeton, jeton),
            Builders<JetonDocument>.Update.Set(d => d.Consomme, true));
    }
}
