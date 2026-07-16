using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>Doublure à la main du port <see cref="IJournalChangements"/> (append-only, trace de lecture).</summary>
public sealed class FakeJournalChangements : IJournalChangements
{
    private readonly List<EvenementChangementSnapshot> _evenements = new();

    public void Consigner(EvenementChangementSnapshot evenement) => _evenements.Add(evenement);

    public IReadOnlyList<EvenementChangementSnapshot> Tout() => _evenements.ToList();
}
