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
}
