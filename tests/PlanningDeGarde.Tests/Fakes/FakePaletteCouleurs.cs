using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port d'accès au set de couleurs acteur → couleur.
/// Set explicite passé au constructeur ; repli neutre (gris) déterministe pour tout
/// acteur absent du set.
/// </summary>
public sealed class FakePaletteCouleurs : IPaletteCouleurs
{
    public const string Neutre = "gris";

    private readonly IReadOnlyDictionary<string, string> _couleurs;

    public FakePaletteCouleurs(IReadOnlyDictionary<string, string> couleurs)
    {
        _couleurs = couleurs;
    }

    public string CouleurNeutre => Neutre;

    public string CouleurDe(string acteurId)
        => _couleurs.TryGetValue(acteurId, out var couleur) ? couleur : Neutre;
}
