using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Persistance en mémoire des propositions d'échange (process unique). Upsert par identifiant stable.</summary>
public sealed class InMemoryPropositionEchangeRepository : IPropositionEchangeRepository
{
    private readonly object _verrou = new();
    private readonly Dictionary<string, PropositionEchangeSnapshot> _propositions = new();

    public void Sauvegarder(PropositionEchange proposition)
    {
        lock (_verrou) _propositions[proposition.Id] = proposition.ToSnapshot();
    }

    public IReadOnlyList<PropositionEchangeSnapshot> AllSnapshots()
    {
        lock (_verrou) return _propositions.Values.ToList();
    }

    public PropositionEchangeSnapshot? ParId(string id)
    {
        lock (_verrou) return _propositions.TryGetValue(id, out var p) ? p : null;
    }

    public void Supprimer(string id)
    {
        lock (_verrou) _propositions.Remove(id);
    }
}
