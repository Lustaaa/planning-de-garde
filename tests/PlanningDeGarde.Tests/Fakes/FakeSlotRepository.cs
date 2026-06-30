using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de persistance des slots.
/// Copy-on-read : on stocke et on rend des snapshots, jamais l'agrégat vivant.
/// L'enregistrement attribue un identifiant stable (clé de suppression, miroir de l'ObjectId Mongo).
/// </summary>
public sealed class FakeSlotRepository : ISlotRepository
{
    private readonly List<SlotSnapshot> _slots = new();

    public void Enregistrer(SlotDeLocalisation slot) => _slots.Add(slot.ToSnapshot() with { Id = Guid.NewGuid().ToString() });

    public IReadOnlyList<SlotSnapshot> AllSnapshots() => _slots.ToList();

    public void Remplacer(SlotSnapshot ancien, SlotDeLocalisation nouveau)
    {
        _slots.Remove(ancien);
        _slots.Add(nouveau.ToSnapshot());
    }

    public void Supprimer(string slotId) => _slots.RemoveAll(s => s.Id == slotId);
}
