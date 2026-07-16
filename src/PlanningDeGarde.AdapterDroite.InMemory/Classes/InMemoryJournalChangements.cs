using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Journal de changements append-only en mémoire (process unique). Trace de lecture, jamais autorité.</summary>
public sealed class InMemoryJournalChangements : IJournalChangements
{
    private readonly object _verrou = new();
    private readonly List<EvenementChangementSnapshot> _evenements = new();

    public void Consigner(EvenementChangementSnapshot evenement)
    {
        lock (_verrou) _evenements.Add(evenement);
    }

    public IReadOnlyList<EvenementChangementSnapshot> Tout()
    {
        lock (_verrou) return _evenements.ToList();
    }
}
