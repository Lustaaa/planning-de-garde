namespace PlanningDeGarde.Application;

/// <summary>
/// Port d'<b>écriture</b> du référentiel de lieux du foyer (miroir écriture d'<see cref="IEnumerationLieux"/>) :
/// enregistre un lieu neuf sur un identifiant stable. Réalisé par le store mutable en Infrastructure
/// (InMemory tests / Mongo runtime), consommé par le handler de gestion du référentiel. La suppression
/// sera ajoutée au scénario suivant (borne YAGNI : S1 ne fait qu'<see cref="Ajouter"/>).
/// </summary>
public interface IEditeurLieux
{
    /// <summary>Enregistre un lieu <b>neuf</b> dans le référentiel : persiste son libellé sur
    /// l'identifiant stable fourni.</summary>
    void Ajouter(string lieuId, string libelle);
}
