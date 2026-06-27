using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Persistance en mémoire des transferts de bascule (process unique).</summary>
public sealed class InMemoryTransfertRepository : ITransfertRepository
{
    private readonly object _verrou = new();
    private readonly List<TransfertSnapshot> _transferts = new();

    public void Enregistrer(Transfert transfert)
    {
        lock (_verrou) _transferts.Add(transfert.ToSnapshot());
    }

    public IReadOnlyList<TransfertSnapshot> AllSnapshots()
    {
        lock (_verrou) return _transferts.ToList();
    }
}
