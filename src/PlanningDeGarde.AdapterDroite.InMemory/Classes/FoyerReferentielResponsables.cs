using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

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
