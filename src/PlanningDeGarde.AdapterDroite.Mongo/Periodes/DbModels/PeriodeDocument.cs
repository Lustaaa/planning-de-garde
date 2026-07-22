using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Periodes.DbModels;

/// <summary>Document persisté d'une période (clé technique générée ; responsable + bornes datées).</summary>
internal sealed class PeriodeDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string ResponsableId { get; set; } = default!;
    public System.DateTime Debut { get; set; }
    public System.DateTime Fin { get; set; }
    // s53 : "" = surcharge partagée/legacy ; sinon scope à un enfant. Élément BSON absent → "" (rétro-compat).
    public string EnfantId { get; set; } = "";

    public static PeriodeDocument De(PeriodeSnapshot p, ObjectId? id = null)
        // Bornes en wall-clock STABLE (Kind=Utc) : évite le décalage de date BSON (cf. DateTimeMongo).
        => new() { Id = id ?? ObjectId.GenerateNewId(), ResponsableId = p.ResponsableId, Debut = DateTimeMongo.WallClock(p.Debut), Fin = DateTimeMongo.WallClock(p.Fin), EnfantId = p.EnfantId };

    public PeriodeSnapshot VersSnapshot() => new(ResponsableId, Debut, Fin, Id.ToString(), EnfantId);
}
