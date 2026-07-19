using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="ITransfertRepository"/> : les transferts
/// de bascule (déposant, récupérant, lieu, heure, date) survivent au redémarrage de l'hôte (Sc.9, s15).
/// Remplaçant durable de <see cref="InMemoryTransfertRepository"/> derrière le <b>port inchangé</b> ;
/// choisi par DI en runtime (« Foyer:Persistance = Mongo »). Write-through + relecture directe.
/// </summary>
public sealed class MongoTransfertRepository : ITransfertRepository
{
    private readonly IMongoCollection<TransfertDocument> _transferts;

    public MongoTransfertRepository(string connectionString, string baseDeDonnees)
        => _transferts = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<TransfertDocument>("transferts");

    public void Enregistrer(Transfert transfert) => _transferts.InsertOne(TransfertDocument.De(transfert.ToSnapshot()));

    public IReadOnlyList<TransfertSnapshot> AllSnapshots()
        => _transferts.Find(Builders<TransfertDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();

    /// <summary>Document persisté d'un transfert (clé technique générée ; bascule + heure + date).</summary>
    private sealed class TransfertDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string DeposeParId { get; set; } = default!;
        public string RecupereParId { get; set; } = default!;
        public string LieuId { get; set; } = default!;
        public long HeureTicks { get; set; }
        public DateTime Date { get; set; }
        // s53 : "" = transfert partagé/legacy ; sinon scope à un enfant. Élément BSON absent → "" (rétro-compat).
        public string EnfantId { get; set; } = "";

        public static TransfertDocument De(TransfertSnapshot t)
            => new()
            {
                DeposeParId = t.DeposeParId,
                RecupereParId = t.RecupereParId,
                LieuId = t.LieuId,
                HeureTicks = t.Heure.Ticks, // l'heure de bascule en ticks (round-trip exact, sans fuseau)
                Date = DateTimeMongo.WallClock(t.Date), // date en wall-clock STABLE (cf. DateTimeMongo)
                EnfantId = t.EnfantId,
            };

        public TransfertSnapshot VersSnapshot()
            => new(DeposeParId, RecupereParId, LieuId, TimeSpan.FromTicks(HeureTicks), Date, EnfantId);
    }
}
