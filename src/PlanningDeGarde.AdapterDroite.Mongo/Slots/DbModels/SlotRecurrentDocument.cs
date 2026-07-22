using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Slots.DbModels;

/// <summary>Document persisté d'un slot récurrent (clé technique générée ; jour de semaine + plage horaire).</summary>
internal sealed class SlotRecurrentDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string EnfantId { get; set; } = default!;
    public string LieuId { get; set; } = default!;
    public int JourDeSemaine { get; set; }
    public long HeureDebutTicks { get; set; }
    public long HeureFinTicks { get; set; }
    // D1 (s31) : conditionnement à la garde + identité du parent poseur. Champs à défaut ([BsonDefaultValue])
    // pour les documents antérieurs (slots non conditionnés) — parité round-trip sans migration.
    [BsonDefaultValue(false)]
    public bool ConditionneGarde { get; set; }
    [BsonDefaultValue("")]
    public string PoseurId { get; set; } = "";

    public static SlotRecurrentDocument De(SlotRecurrentSnapshot s)
        // Plage horaire en ticks (round-trip exact, sans fuseau) ; jour de semaine en entier stable.
        => new()
        {
            EnfantId = s.EnfantId,
            LieuId = s.LieuId,
            JourDeSemaine = (int)s.JourDeSemaine,
            HeureDebutTicks = s.HeureDebut.Ticks,
            HeureFinTicks = s.HeureFin.Ticks,
            ConditionneGarde = s.ConditionneGarde,
            PoseurId = s.PoseurId,
        };

    public SlotRecurrentSnapshot VersSnapshot()
        => new(EnfantId, LieuId, (DayOfWeek)JourDeSemaine, TimeSpan.FromTicks(HeureDebutTicks), TimeSpan.FromTicks(HeureFinTicks),
            ConditionneGarde, PoseurId, Id.ToString());
}
