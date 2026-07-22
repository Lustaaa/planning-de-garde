using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.InMemory.Foyer.Repositories;

// Le segment de namespace « .Foyer » masque la classe seed Foyer (Application.Foyer.Seed) : alias
// scopé au namespace (gagne sur le membre de namespace externe) pour relire le référentiel d'amorçage.
using Foyer = PlanningDeGarde.Application.Foyer.Seed.Foyer;

/// <summary>
/// Implémentation du port <see cref="IPaletteCouleurs"/> lisant le set de couleurs par défaut
/// du <see cref="Foyer"/>. Repli déterministe sur la couleur neutre pour tout acteur absent du set.
/// </summary>
public sealed class FoyerPaletteCouleurs : IPaletteCouleurs
{
    public string CouleurNeutre => Foyer.CouleurNeutre;

    public string CouleurDe(string acteurId)
        => Foyer.CouleursParActeur.TryGetValue(acteurId, out var couleur) ? couleur : Foyer.CouleurNeutre;
}
