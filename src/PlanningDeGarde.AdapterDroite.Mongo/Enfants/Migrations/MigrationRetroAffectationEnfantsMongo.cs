using MongoDB.Bson;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Enfants.Migrations;

/// <summary>
/// Migration de <b>rétro-affectation</b> des slots existants attachés au <b>fantôme</b> d'enfant.
/// Les slots posés avant le hissage de l'enfant portent un <c>EnfantId</c> fantôme transmis par la
/// <c>Session</c> : la valeur littérale du <b>prénom</b> (« Léa »), jamais choisie ni validée. Cette
/// migration <b>réconcilie une dette de données</b> sur le store Mongo durable : elle réattache chaque
/// slot dont l'<c>EnfantId</c> vaut le <b>prénom</b> d'un enfant réel du référentiel à l'<b>identifiant
/// stable opaque</b> de cet enfant. Elle n'invente aucune sémantique de rattachement.
///
/// <para><b>Idempotente</b> : après un premier passage, plus aucun slot ne porte le prénom comme
/// <c>EnfantId</c> (tous portent l'id opaque) — un rejeu ne matche rien et réussit en no-op, sans double
/// rattachement ni erreur. Prouvée contre un store Mongo RÉEL (Docker), jamais une doublure.</para>
/// </summary>
public sealed class MigrationRetroAffectationEnfantsMongo
{
    private readonly IMongoDatabase _db;

    public MigrationRetroAffectationEnfantsMongo(string connectionString, string baseDeDonnees)
        => _db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);

    /// <summary>Réattache les slots (ponctuels ET récurrents) du fantôme aux enfants réels du référentiel.
    /// Retourne le nombre de slots effectivement réattachés (0 au rejeu = idempotence).</summary>
    public long Executer(IEnumerationEnfants enfants)
    {
        long reattaches = 0;
        foreach (var enfant in enfants.EnumererEnfants())
        {
            // Un enfant dont l'id EST déjà son prénom (id = prénom) n'a pas de dette à réconcilier :
            // le fantôme et le réel coïncident (aucune écriture au rejeu — idempotence stricte).
            if (enfant.Id == enfant.Prenom)
                continue;

            // Réconcilie les DEUX canaux de slots (ponctuel + récurrent) : tout slot dont l'EnfantId vaut
            // le prénom fantôme est réattaché à l'identifiant stable opaque de l'enfant réel.
            reattaches += Reattacher("slots", enfant.Prenom, enfant.Id);
            reattaches += Reattacher("slots_recurrents", enfant.Prenom, enfant.Id);
        }
        return reattaches;
    }

    private long Reattacher(string collection, string enfantIdFantome, string enfantIdReel)
    {
        var slots = _db.GetCollection<BsonDocument>(collection);
        var resultat = slots.UpdateMany(
            Builders<BsonDocument>.Filter.Eq("EnfantId", enfantIdFantome),
            Builders<BsonDocument>.Update.Set("EnfantId", enfantIdReel));
        return resultat.ModifiedCount;
    }
}
