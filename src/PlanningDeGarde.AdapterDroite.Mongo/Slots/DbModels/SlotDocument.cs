using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Slots.DbModels;

/// <summary>Document persisté d'un slot (clé technique générée ; bornes datées + enfant/lieu).</summary>
internal sealed class SlotDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string EnfantId { get; set; } = default!;
    public string LieuId { get; set; } = default!;
    public System.DateTime Debut { get; set; }
    public System.DateTime Fin { get; set; }

    public static SlotDocument De(SlotSnapshot s)
        // Bornes traitées en wall-clock STABLE (Kind=Utc) : les dates du planning n'ont pas de fuseau,
        // marquer Utc évite la conversion locale→UTC de BSON qui décalerait la date d'un jour près de minuit.
        => new() { EnfantId = s.EnfantId, LieuId = s.LieuId, Debut = DateTimeMongo.WallClock(s.Debut), Fin = DateTimeMongo.WallClock(s.Fin) };

    public SlotSnapshot VersSnapshot() => new(EnfantId, LieuId, Debut, Fin, Id.ToString());
}
