using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.InMemory.Foyer.Repositories;

// Le segment de namespace « .Foyer » masque la classe seed Foyer (Application.Foyer.Seed) : alias
// scopé au namespace (gagne sur le membre de namespace externe) pour relire le référentiel d'amorçage.
using Foyer = PlanningDeGarde.Application.Foyer.Seed.Foyer;

/// <summary>
/// Implémentation du port <see cref="IReferentielResponsables"/> lisant les noms du
/// <see cref="Foyer"/>. Miroir de <see cref="FoyerPaletteCouleurs"/> : résolution sur
/// l'identifiant stable, repli déterministe sur l'identifiant lui-même pour un responsable
/// absent du référentiel.
/// </summary>
public sealed class FoyerReferentielResponsables : IReferentielResponsables
{
    public string NomDe(string responsableId)
        => Foyer.NomsParResponsable.TryGetValue(responsableId, out var nom) ? nom : responsableId;
}
