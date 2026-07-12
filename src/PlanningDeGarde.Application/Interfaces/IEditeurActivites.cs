namespace PlanningDeGarde.Application;

/// <summary>
/// Port d'<b>écriture</b> du référentiel de lieux du foyer (miroir écriture d'<see cref="IEnumerationActivites"/>) :
/// enregistre un lieu neuf sur un identifiant stable. Réalisé par le store mutable en Infrastructure
/// (InMemory tests / Mongo runtime), consommé par le handler de gestion du référentiel. La suppression
/// sera ajoutée au scénario suivant (borne YAGNI : S1 ne fait qu'<see cref="Ajouter"/>).
/// </summary>
public interface IEditeurActivites
{
    /// <summary>Enregistre un lieu <b>neuf</b> dans le référentiel : persiste son libellé sur
    /// l'identifiant stable fourni.</summary>
    void Ajouter(string lieuId, string libelle);

    /// <summary>Retire le lieu identifié de façon stable du référentiel : il cesse d'être énuméré et
    /// n'est plus acceptable à la saisie. Tolérant à l'absence (un lieu déjà absent = no-op qui
    /// réussit — idempotence). Aucune réécriture rétroactive : un slot déjà posé sur ce lieu conserve
    /// son lieu (borne s27).</summary>
    void Supprimer(string lieuId);
}
