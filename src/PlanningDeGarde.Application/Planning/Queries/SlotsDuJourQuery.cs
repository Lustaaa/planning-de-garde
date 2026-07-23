using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Planning.Queries;

/// <summary>
/// Projection de lecture (CQRS) des slots de localisation <b>couvrant</b> une date donnée — alimente la
/// dialog de suppression (6ᵉ usage du menu clic-case). Chaque slot est rendu avec son identifiant stable,
/// son enfant, son lieu et ses bornes horaires (le <see cref="SlotSnapshot"/> de la frontière de
/// persistance). Un slot franchissant minuit couvre ses deux jours → il apparaît dans la liste de chacun.
/// N'écrit jamais et ne déclenche <b>jamais</b> la diffusion temps réel.
/// </summary>
public sealed class SlotsDuJourQuery
{
    private readonly ISlotRepository _slots;

    public SlotsDuJourQuery(ISlotRepository slots) => _slots = slots;

    /// <summary>Slots dont l'intervalle [Debut.Fin] couvre la <paramref name="date"/> (jours bornes inclus).</summary>
    public IReadOnlyList<SlotSnapshot> Lister(DateOnly date)
    {
        return _slots.AllSnapshots()
            .Where(s => date >= DateOnly.FromDateTime(s.Debut) && date <= DateOnly.FromDateTime(s.Fin))
            .ToList();
    }

    /// <summary>Slots couvrant la <paramref name="date"/> pour le seul enfant <paramref name="enfantId"/>
    /// (isolation par enfant, s53 — l'activité est une sous-ressource de l'enfant depuis s54).</summary>
    public IReadOnlyList<SlotSnapshot> Lister(DateOnly date, string enfantId)
    {
        return Lister(date).Where(s => s.EnfantId == enfantId).ToList();
    }
}
