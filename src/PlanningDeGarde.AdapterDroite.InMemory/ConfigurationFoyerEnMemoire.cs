using System.Collections.Generic;
using System.Linq;
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
public sealed class ConfigurationFoyerEnMemoire : IReferentielResponsables, IEditeurConfigurationFoyer, IPaletteCouleurs, IEnumerationActeursFoyer
{
    private readonly Dictionary<string, string> _noms;
    private readonly Dictionary<string, string> _couleurs;

    public ConfigurationFoyerEnMemoire()
    {
        _noms = new Dictionary<string, string>(Foyer.NomsParResponsable);
        _couleurs = new Dictionary<string, string>(Foyer.CouleursParActeur);
    }

    public string NomDe(string responsableId)
        => _noms.TryGetValue(responsableId, out var nom) ? nom : responsableId;

    public void Ajouter(string acteurId, string nom, string? couleur)
    {
        _noms[acteurId] = nom;                  // l'acteur ajouté existe désormais sur son id neuf
        if (couleur is not null)
            _couleurs[acteurId] = couleur;      // couleur absente → repli neutre par contrat CouleurDe (Sc.5)
    }

    public IReadOnlyCollection<string> EnumererActeurs()
        => _noms.Keys.ToList(); // tous les acteurs du store : seeds + ajoutés (résolution nom/couleur sur l'id)

    public void Renommer(string acteurId, string nouveauNom)
        => _noms[acteurId] = nouveauNom; // dernière écriture gagne (écrase, sans version)

    public string CouleurNeutre => Foyer.CouleurNeutre;

    public string CouleurDe(string acteurId)
        => _couleurs.TryGetValue(acteurId, out var couleur) ? couleur : Foyer.CouleurNeutre;

    public void Recolorier(string acteurId, string nouvelleCouleur)
        => _couleurs[acteurId] = nouvelleCouleur; // dernière écriture gagne (surface distincte du nom)
}
