using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de persistance des périodes de garde.
/// Copy-on-read : on stocke et on rend des snapshots, jamais l'agrégat vivant.
/// </summary>
public sealed class FakePeriodeRepository : IPeriodeRepository
{
    private readonly List<PeriodeSnapshot> _periodes = new();

    public void Enregistrer(PeriodeDeGarde periode) => _periodes.Add(periode.ToSnapshot());

    public IReadOnlyList<PeriodeSnapshot> AllSnapshots() => _periodes.ToList();
}
