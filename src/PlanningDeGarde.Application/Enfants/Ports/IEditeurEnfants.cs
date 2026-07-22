namespace PlanningDeGarde.Application.Enfants.Ports;

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
    /// (enrichissement, jamais recréation — l'id de l'enfant est la clé inchangée), avec son
    /// <b>rôle-du-lien</b> (père / mère / parent-libre, s37 — défaut « parent-libre » si absent,
    /// comportement s34 préservé). Upsert par acteur : re-lier un parent déjà lié <b>met à jour son
    /// rôle-du-lien</b> sans dupliquer le lien. Persisté ; relu via <see cref="EnfantFoyer.ParentsLies"/>.</summary>
    void LierParent(string enfantId, string acteurId, RoleDuLien role = RoleDuLien.ParentLibre);

    /// <summary>Retire le lien vers un <b>parent-acteur</b> de l'enfant identifié de façon stable (l'id
    /// de l'enfant et ses autres liens restent inchangés). <b>Idempotent</b> : délier un parent déjà
    /// non lié (ou un enfant sans lien) est neutre — aucune écriture, aucune erreur.</summary>
    void DelierParent(string enfantId, string acteurId);
}
