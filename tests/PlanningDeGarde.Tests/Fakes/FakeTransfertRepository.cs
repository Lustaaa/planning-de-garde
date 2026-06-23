using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de persistance des transferts.
/// Copy-on-read : on stocke et on rend des snapshots, jamais l'agrégat vivant.
/// </summary>
public sealed class FakeTransfertRepository : ITransfertRepository
{
    private readonly List<TransfertSnapshot> _transferts = new();

    public void Enregistrer(Transfert transfert) => _transferts.Add(transfert.ToSnapshot());

    public IReadOnlyList<TransfertSnapshot> AllSnapshots() => _transferts.ToList();
}
