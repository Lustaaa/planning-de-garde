using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port répondant à la capacité « ce responsable existe-t-il dans le foyer ? ».
/// </summary>
public sealed class FakeResponsableRepository : IResponsableRepository
{
    private readonly HashSet<string> _responsables = new();

    public FakeResponsableRepository AvecResponsable(string responsableId) { _responsables.Add(responsableId); return this; }

    public bool Existe(string responsableId) => _responsables.Contains(responsableId);
}
