using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Transferts.DbModels;

/// <summary>Document persisté d'un transfert (clé technique générée ; bascule + heure + date).</summary>
internal sealed class TransfertDocument
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
