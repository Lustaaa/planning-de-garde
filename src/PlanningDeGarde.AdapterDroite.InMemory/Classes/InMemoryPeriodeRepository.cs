using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Persistance en mémoire des périodes de garde (process unique). Le contrôle d'écriture
/// périmée (concurrence optimiste) compare la base observée à l'état courant.
/// </summary>
public sealed class InMemoryPeriodeRepository : IPeriodeRepository
{
    private readonly object _verrou = new();
    private readonly List<PeriodeSnapshot> _periodes = new();

    public void Enregistrer(PeriodeDeGarde periode)
    {
        lock (_verrou) _periodes.Add(periode.ToSnapshot());
    }

    public IReadOnlyList<PeriodeSnapshot> AllSnapshots()
    {
        lock (_verrou) return _periodes.ToList();
    }

    public bool Modifier(PeriodeSnapshot baseObservee, PeriodeSnapshot modification)
    {
        lock (_verrou)
        {
            var index = _periodes.IndexOf(baseObservee);
            if (index < 0)
                return false; // état périmé : la base observée n'est plus l'état courant

            _periodes[index] = modification;
            return true;
        }
    }
}
