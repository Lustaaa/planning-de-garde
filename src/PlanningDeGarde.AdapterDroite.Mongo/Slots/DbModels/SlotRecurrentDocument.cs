using System;
using System.Collections.Generic;
using System.Linq;
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
    // MULTI-JOURS (s54) : set de jours de la série (entiers stables). [BsonDefaultValue] vide pour les
    // documents antérieurs (mono-jour) → réconciliés à la relecture sur JourDeSemaine (seed convergent).
    [BsonDefaultValue(new int[0])]
    public int[] JoursDeSemaine { get; set; } = Array.Empty<int>();
    // VACANCES (s54) : plages d'exclusion de la série (bornes en jours). Champ absent des documents
    // antérieurs (aucune exclusion) → l'initialiseur ci-dessous (tableau vide) tient lieu de défaut
    // (le driver tolère l'élément manquant) — parité round-trip sans migration.
    public PlageExclusionDocument[] Exclusions { get; set; } = Array.Empty<PlageExclusionDocument>();

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
            JoursDeSemaine = s.JoursDeSemaine.Select(j => (int)j).ToArray(),
            Exclusions = s.Exclusions.Select(PlageExclusionDocument.De).ToArray(),
        };

    public SlotRecurrentSnapshot VersSnapshot()
        => new(EnfantId, LieuId, (DayOfWeek)JourDeSemaine, TimeSpan.FromTicks(HeureDebutTicks), TimeSpan.FromTicks(HeureFinTicks),
            ConditionneGarde, PoseurId, Id.ToString())
        {
            // Réconciliation seed convergent : document mono-jour legacy (set vide) → set = son jour unique.
            JoursDeSemaine = JoursDeSemaine.Length > 0
                ? JoursDeSemaine.Select(j => (DayOfWeek)j).ToArray()
                : new[] { (DayOfWeek)JourDeSemaine },
            Exclusions = (Exclusions ?? Array.Empty<PlageExclusionDocument>()).Select(e => e.VersPlage()).ToArray(),
        };
}

/// <summary>Plage d'exclusion persistée (bornes en numéro de jour, round-trip stable sans fuseau).</summary>
internal sealed class PlageExclusionDocument
{
    public int DebutDayNumber { get; set; }
    public int FinDayNumber { get; set; }

    public static PlageExclusionDocument De(PlageExclusion p)
        => new() { DebutDayNumber = p.Debut.DayNumber, FinDayNumber = p.Fin.DayNumber };

    public PlageExclusion VersPlage()
        => new(DateOnly.FromDayNumber(DebutDayNumber), DateOnly.FromDayNumber(FinDayNumber));
}
