using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>Doublure à la main du port de persistance des propositions d'échange. Upsert par identifiant stable.</summary>
public sealed class FakePropositionEchangeRepository : IPropositionEchangeRepository
{
    private readonly Dictionary<string, PropositionEchangeSnapshot> _propositions = new();

    public void Sauvegarder(PropositionEchange proposition) => _propositions[proposition.Id] = proposition.ToSnapshot();

    public IReadOnlyList<PropositionEchangeSnapshot> AllSnapshots() => _propositions.Values.ToList();

    public PropositionEchangeSnapshot? ParId(string id) => _propositions.TryGetValue(id, out var p) ? p : null;

    public void Supprimer(string id) => _propositions.Remove(id);
}
