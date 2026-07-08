using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de persistance des slots récurrents.
/// Copy-on-read : on stocke et on rend des snapshots, jamais l'agrégat vivant.
/// L'enregistrement attribue un identifiant stable (clé de suppression, miroir de l'ObjectId Mongo).
/// </summary>
public sealed class FakeSlotRecurrentRepository : ISlotRecurrentRepository
{
    private readonly List<SlotRecurrentSnapshot> _slots = new();

    public void Enregistrer(SlotRecurrent slot) => _slots.Add(slot.ToSnapshot() with { Id = Guid.NewGuid().ToString() });

    public IReadOnlyList<SlotRecurrentSnapshot> AllSnapshots() => _slots.ToList();

    public void Supprimer(string slotId) => _slots.RemoveAll(s => s.Id == slotId);
}
