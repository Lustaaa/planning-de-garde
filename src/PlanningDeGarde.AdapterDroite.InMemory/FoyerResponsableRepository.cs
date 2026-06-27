using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Répond « ce responsable existe-t-il dans le foyer ? » à partir du référentiel.</summary>
public sealed class FoyerResponsableRepository : IResponsableRepository
{
    private readonly HashSet<string> _responsables = new(Foyer.Responsables);

    public bool Existe(string responsableId) => _responsables.Contains(responsableId);
}
