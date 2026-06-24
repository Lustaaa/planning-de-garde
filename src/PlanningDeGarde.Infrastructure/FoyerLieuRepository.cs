using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Répond « ce lieu existe-t-il dans le foyer ? » à partir du référentiel de départ.</summary>
public sealed class FoyerLieuRepository : ILieuRepository
{
    private readonly HashSet<string> _lieux = new(Foyer.Lieux);

    public bool Existe(string lieuId) => _lieux.Contains(lieuId);
}
