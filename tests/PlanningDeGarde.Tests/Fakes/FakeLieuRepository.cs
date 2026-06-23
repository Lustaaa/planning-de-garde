using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port répondant à la capacité « ce lieu existe-t-il dans le foyer ? ».
/// </summary>
public sealed class FakeLieuRepository : ILieuRepository
{
    private readonly HashSet<string> _lieux = new();

    public FakeLieuRepository AvecLieu(string lieuId) { _lieux.Add(lieuId); return this; }

    public bool Existe(string lieuId) => _lieux.Contains(lieuId);
}
