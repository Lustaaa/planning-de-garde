using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store de configuration : réalise le port d'écriture
/// <see cref="IEditeurConfigurationFoyer"/> (renommer) ET la lecture
/// <see cref="IReferentielResponsables"/> (NomDe) sur un dictionnaire en mémoire, pour
/// asserter en unitaire que le handler a bien appliqué l'édition via le port d'écriture.
/// Seed explicite passé au constructeur ; dernière écriture gagne.
/// </summary>
public sealed class FakeConfigurationFoyer : IEditeurConfigurationFoyer, IReferentielResponsables
{
    private readonly Dictionary<string, string> _noms;

    public FakeConfigurationFoyer(IDictionary<string, string> seed)
    {
        _noms = new Dictionary<string, string>(seed);
    }

    public void Renommer(string acteurId, string nouveauNom) => _noms[acteurId] = nouveauNom;

    public string NomDe(string responsableId)
        => _noms.TryGetValue(responsableId, out var nom) ? nom : responsableId;
}
