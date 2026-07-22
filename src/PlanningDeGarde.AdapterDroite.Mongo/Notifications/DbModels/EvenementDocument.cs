using System;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Notifications.DbModels;

/// <summary>Document persisté d'un événement (clé métier stable ; jour + horodatage wall-clock).</summary>
internal sealed class EvenementDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string Type { get; set; } = default!;
    public DateTime Jour { get; set; }
    public string EnfantId { get; set; } = default!;
    public string CedantId { get; set; } = default!;
    public string RecevantId { get; set; } = default!;
    public DateTime Horodatage { get; set; }

    /// <summary>Sous-type d'imprévu (s48) — présent seulement quand <see cref="Type"/> vaut Imprevu (null sinon).</summary>
    public string? Imprevu { get; set; }

    /// <summary>Motif optionnel d'un imprévu (s48) — vide par défaut.</summary>
    public string Motif { get; set; } = "";

    public static EvenementDocument De(EvenementChangementSnapshot e)
        => new()
        {
            Id = e.Id,
            Type = e.Type.ToString(),
            Jour = DateTimeMongo.WallClock(e.Jour.ToDateTime(TimeOnly.MinValue)),
            EnfantId = e.EnfantId,
            CedantId = e.CedantId,
            RecevantId = e.RecevantId,
            Horodatage = DateTimeMongo.WallClock(e.Horodatage),
            Imprevu = e.Imprevu?.ToString(),
            Motif = e.Motif,
        };

    public EvenementChangementSnapshot VersSnapshot()
        => new(Id, Enum.Parse<TypeChangement>(Type), DateOnly.FromDateTime(Jour), EnfantId, CedantId, RecevantId, Horodatage,
            Imprevu is null ? null : Enum.Parse<TypeImprevu>(Imprevu), Motif ?? "");
}
