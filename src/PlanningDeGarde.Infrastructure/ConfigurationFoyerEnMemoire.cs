using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire de la configuration volatile des acteurs du foyer. Seedé à la
/// (re)construction depuis le <see cref="Foyer"/> (miroir du seed en dur), puis éditable
/// en session : réalise le port de lecture <see cref="IReferentielResponsables"/> (le nom
/// résolu reflète la dernière écriture) et le port d'écriture
/// <see cref="IEditeurConfigurationFoyer"/>. Volatile = re-seedé au redémarrage (Sc.10).
/// Remplaçant éditable du dictionnaire <c>static readonly</c> lu par
/// <see cref="FoyerReferentielResponsables"/> — la résolution reste sur l'identifiant stable.
/// </summary>
public sealed class ConfigurationFoyerEnMemoire : IReferentielResponsables, IEditeurConfigurationFoyer
{
    private readonly Dictionary<string, string> _noms;

    public ConfigurationFoyerEnMemoire()
    {
        _noms = new Dictionary<string, string>(Foyer.NomsParResponsable);
    }

    public string NomDe(string responsableId)
        => _noms.TryGetValue(responsableId, out var nom) ? nom : responsableId;

    public void Renommer(string acteurId, string nouveauNom)
        => _noms[acteurId] = nouveauNom; // dernière écriture gagne (écrase, sans version)
}
