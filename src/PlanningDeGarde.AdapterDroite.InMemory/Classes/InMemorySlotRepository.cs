using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Persistance en mémoire des slots (process unique). Copy-on-read : stocke et rend des
/// snapshots, jamais l'agrégat vivant. Singleton DI = source de vérité du foyer.
/// L'enregistrement attribue un identifiant stable (clé de suppression, miroir de l'ObjectId Mongo).
/// </summary>
public sealed class InMemorySlotRepository : ISlotRepository
{
    private readonly object _verrou = new();
    private readonly List<SlotSnapshot> _slots = new();

    public void Enregistrer(SlotDeLocalisation slot)
    {
        lock (_verrou) _slots.Add(slot.ToSnapshot() with { Id = Guid.NewGuid().ToString() });
    }

    public IReadOnlyList<SlotSnapshot> AllSnapshots()
    {
        lock (_verrou) return _slots.ToList();
    }

    public void Remplacer(SlotSnapshot ancien, SlotDeLocalisation nouveau)
    {
        lock (_verrou)
        {
            _slots.Remove(ancien);
            _slots.Add(nouveau.ToSnapshot());
        }
    }

    public void Supprimer(string slotId)
    {
        lock (_verrou) _slots.RemoveAll(s => s.Id == slotId);
    }
}
