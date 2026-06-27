using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store de configuration : réalise le port d'écriture
/// <see cref="IEditeurConfigurationFoyer"/> (renommer / recolorier), la lecture des noms
/// <see cref="IReferentielResponsables"/> (NomDe) ET la lecture des couleurs
/// <see cref="IPaletteCouleurs"/> (CouleurDe) sur deux dictionnaires en mémoire séparés, pour
/// asserter en unitaire que le handler a bien appliqué l'édition via le port d'écriture sur la
/// bonne surface (nom et couleur indépendants). Seeds explicites au constructeur ; dernière
/// écriture gagne.
/// </summary>
public sealed class FakeConfigurationFoyer : IEditeurConfigurationFoyer, IReferentielResponsables, IPaletteCouleurs
{
    public const string Neutre = "gris";

    private readonly Dictionary<string, string> _noms;
    private readonly Dictionary<string, string> _couleurs;

    public FakeConfigurationFoyer(IDictionary<string, string> seed, IDictionary<string, string>? couleurs = null)
    {
        _noms = new Dictionary<string, string>(seed);
        _couleurs = couleurs is null ? new Dictionary<string, string>() : new Dictionary<string, string>(couleurs);
    }

    public void Renommer(string acteurId, string nouveauNom) => _noms[acteurId] = nouveauNom;

    public void Recolorier(string acteurId, string nouvelleCouleur) => _couleurs[acteurId] = nouvelleCouleur;

    public string NomDe(string responsableId)
        => _noms.TryGetValue(responsableId, out var nom) ? nom : responsableId;

    public string CouleurNeutre => Neutre;

    public string CouleurDe(string acteurId)
        => _couleurs.TryGetValue(acteurId, out var couleur) ? couleur : Neutre;
}
