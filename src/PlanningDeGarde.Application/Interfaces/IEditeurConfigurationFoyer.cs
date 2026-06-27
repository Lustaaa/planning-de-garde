namespace PlanningDeGarde.Application;

/// <summary>
/// Port d'écriture de la configuration volatile des acteurs du foyer : renomme un acteur
/// sur son identifiant stable (l'id n'est jamais éditable — il est la clé). Miroir écriture
/// du port de lecture <see cref="IReferentielResponsables"/> ; réalisé par le store mutable
/// en Infrastructure, consommé par le handler d'édition. Dernière écriture gagne (le store
/// écrase la valeur, sans version ni rejet — préparé pour la concurrence Sc.7).
/// </summary>
public interface IEditeurConfigurationFoyer
{
    /// <summary>Ajoute un acteur <b>neuf</b> au foyer sur un identifiant stable neuf (jamais un id
    /// existant) : persiste le nom et, si fournie, la couleur (couleur absente → repli neutre par
    /// le contrat <see cref="IPaletteCouleurs"/>, aucune écriture couleur). Surface d'<b>ajout</b>
    /// distincte de l'édition (<see cref="Renommer"/> / <see cref="Recolorier"/> mutent un acteur
    /// déjà présent sur son id stable).</summary>
    void Ajouter(string acteurId, string nom, string? couleur);

    /// <summary>Affecte un nouveau nom d'affichage à l'acteur identifié de façon stable.</summary>
    void Renommer(string acteurId, string nouveauNom);

    /// <summary>Affecte une nouvelle couleur à l'acteur identifié de façon stable (surface
    /// distincte du nom — recolorier ne touche jamais le nom).</summary>
    void Recolorier(string acteurId, string nouvelleCouleur);
}
