using System;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Echanges.DbModels;

/// <summary>Document persisté d'une proposition (clé métier stable ; jour wall-clock ; statut).</summary>
internal sealed class PropositionDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public DateTime Jour { get; set; }

    /// <summary>Fin de plage INCLUSE (s52). Absente sur les documents antérieurs (échange d'UN jour) → repli sur <see cref="Jour"/>.</summary>
    [BsonIgnoreIfNull]
    public DateTime? JourFin { get; set; }
    public string EnfantId { get; set; } = default!;
    public string DeActeurId { get; set; } = default!;
    public string VersActeurId { get; set; } = default!;
    public string Statut { get; set; } = default!;

    public static PropositionDocument De(PropositionEchangeSnapshot p)
        => new()
        {
            Id = p.Id,
            Jour = DateTimeMongo.WallClock(p.Jour.ToDateTime(TimeOnly.MinValue)),
            JourFin = DateTimeMongo.WallClock(p.JourFin.ToDateTime(TimeOnly.MinValue)),
            EnfantId = p.EnfantId,
            DeActeurId = p.DeActeurId,
            VersActeurId = p.VersActeurId,
            Statut = p.Statut.ToString(),
        };

    public PropositionEchangeSnapshot VersSnapshot()
        => new(Id, DateOnly.FromDateTime(Jour), EnfantId, DeActeurId, VersActeurId, Enum.Parse<StatutProposition>(Statut),
            DateOnly.FromDateTime(JourFin ?? Jour));
}
