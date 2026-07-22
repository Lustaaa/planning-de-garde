using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.InMemory.Foyer.Repositories;

// Le segment de namespace « .Foyer » masque la classe seed Foyer (Application.Foyer.Seed) : alias
// scopé au namespace (gagne sur le membre de namespace externe) pour relire le référentiel d'amorçage.
using Foyer = PlanningDeGarde.Application.Foyer.Seed.Foyer;

/// <summary>Répond « ce responsable existe-t-il dans le foyer ? » à partir du référentiel.</summary>
public sealed class FoyerResponsableRepository : IResponsableRepository
{
    private readonly HashSet<string> _responsables = new(Foyer.Responsables);

    public bool Existe(string responsableId) => _responsables.Contains(responsableId);
}
