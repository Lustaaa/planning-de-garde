namespace PlanningDeGarde.Application;

/// <summary>
/// Port d'<b>écriture</b> du référentiel d'enfants du foyer (miroir écriture d'<see cref="IEnumerationEnfants"/>) :
/// enregistre un enfant neuf sur un identifiant stable opaque. Réalisé par le store mutable en
/// Infrastructure (InMemory tests-runtime / Mongo runtime), consommé par le handler de gestion du
/// référentiel. L'édition du prénom sera ajoutée au scénario suivant (borne YAGNI : S1 ne fait
/// qu'<see cref="Ajouter"/>).
/// </summary>
public interface IEditeurEnfants
{
    /// <summary>Enregistre un enfant <b>neuf</b> dans le référentiel : persiste son prénom sur
    /// l'identifiant stable opaque fourni (jamais un id existant).</summary>
    void Ajouter(string enfantId, string prenom);

    /// <summary>Affecte un nouveau prénom à l'enfant identifié de façon stable (l'id n'est jamais
    /// éditable — il est la clé) : dernière écriture gagne, aucun doublon (le même id reste un
    /// unique enfant).</summary>
    void Editer(string enfantId, string nouveauPrenom);

    /// <summary>Lie un <b>parent-acteur</b> (identifiant stable) à l'enfant identifié de façon stable
    /// (enrichissement, jamais recréation — l'id de l'enfant est la clé inchangée). Le lien est porté
    /// par le modèle et persisté ; relu ensuite via <see cref="EnfantFoyer.ParentsLies"/>.</summary>
    void LierParent(string enfantId, string acteurId);
}
