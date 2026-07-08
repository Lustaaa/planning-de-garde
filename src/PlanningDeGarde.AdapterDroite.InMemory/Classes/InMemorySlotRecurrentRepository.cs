using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Persistance en mémoire des slots récurrents (process unique). Copy-on-read : stocke et rend des
/// snapshots, jamais l'agrégat vivant. Singleton DI = source de vérité du foyer. L'enregistrement
/// attribue un identifiant stable (clé de suppression, miroir de l'ObjectId Mongo). Volatile
/// (re-parti vide au redémarrage) ; le remplaçant durable est <see cref="MongoSlotRecurrentRepository"/>.
/// </summary>
public sealed class InMemorySlotRecurrentRepository : ISlotRecurrentRepository
{
    private readonly object _verrou = new();
    private readonly List<SlotRecurrentSnapshot> _slots = new();

    public void Enregistrer(SlotRecurrent slot)
    {
        lock (_verrou) _slots.Add(slot.ToSnapshot() with { Id = Guid.NewGuid().ToString() });
    }

    public IReadOnlyList<SlotRecurrentSnapshot> AllSnapshots()
    {
        lock (_verrou) return _slots.ToList();
    }

    public void Supprimer(string slotId)
    {
        lock (_verrou) _slots.RemoveAll(s => s.Id == slotId);
    }
}
