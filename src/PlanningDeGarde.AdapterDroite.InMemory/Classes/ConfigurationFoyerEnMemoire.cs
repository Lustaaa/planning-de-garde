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
    private readonly Dictionary<string, string> _roles = new(); // acteurId → id de rôle (attribut optionnel)
    private readonly Dictionary<string, string> _adresses = new(); // acteurId → adresse de résidence (optionnelle)

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

    public TypeActeur TypeDe(string acteurId)
        // Type surfacé en lecture seule depuis la déclaration seed (D3) ; un acteur ajouté en session
        // (absent du seed de types) retombe sur le défaut Parent — aucune persistance neuve de type.
        => Foyer.TypesParActeur.TryGetValue(acteurId, out var type) ? type : Foyer.TypeParDefaut;

    public void Renommer(string acteurId, string nouveauNom)
        => _noms[acteurId] = nouveauNom; // dernière écriture gagne (écrase, sans version)

    public string CouleurNeutre => Foyer.CouleurNeutre;

    public string CouleurDe(string acteurId)
        => _couleurs.TryGetValue(acteurId, out var couleur) ? couleur : Foyer.CouleurNeutre;

    public void Recolorier(string acteurId, string nouvelleCouleur)
        => _couleurs[acteurId] = nouvelleCouleur; // dernière écriture gagne (surface distincte du nom)

    public void ChangerAdresse(string acteurId, string adresse)
        => _adresses[acteurId] = adresse; // surface optionnelle distincte ; dernière écriture gagne (vide licite)

    /// <summary>Adresse de résidence de l'acteur, ou <c>null</c> s'il n'en porte aucune (attribut optionnel).</summary>
    public string? AdresseDe(string acteurId)
        => _adresses.TryGetValue(acteurId, out var adresse) ? adresse : null;

    public void AffecterRole(string acteurId, string roleId)
        => _roles[acteurId] = roleId; // surface distincte du nom/couleur : l'id de rôle porté par l'acteur

    public void RetirerRole(string acteurId)
        => _roles.Remove(acteurId); // « sans rôle » (neutre) ; tolérant à l'absence — aucun rôle fantôme

    /// <summary>Id de rôle porté par l'acteur, ou <c>null</c> s'il n'en porte aucun (« sans rôle »,
    /// attribut optionnel — valeur neutre par défaut).</summary>
    public string? RoleDe(string acteurId)
        => _roles.TryGetValue(acteurId, out var roleId) ? roleId : null;

    public void Supprimer(string acteurId)
    {
        _noms.Remove(acteurId);     // l'acteur cesse d'être énuméré et résolu (NomDe retombe sur l'id brut)
        _couleurs.Remove(acteurId); // ... ET sa couleur retombe sur le neutre — miroir d'Ajouter
    }
}
