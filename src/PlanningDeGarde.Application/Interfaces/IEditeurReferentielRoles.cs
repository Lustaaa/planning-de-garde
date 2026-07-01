namespace PlanningDeGarde.Application;

/// <summary>
/// Port d'<b>écriture</b> du référentiel de rôles du foyer (miroir écriture d'<see cref="IEnumerationRoles"/>) :
/// crée un rôle neuf sur un identifiant stable opaque (jamais dérivé du libellé — l'id est la clé).
/// Réalisé par le store mutable en Infrastructure (InMemory tests / Mongo runtime), consommé par
/// le handler de gestion du référentiel. Le renommage et la suppression seront ajoutés aux scénarios
/// suivants (borne YAGNI : Sc.1 ne crée que <see cref="Creer"/>).
/// </summary>
public interface IEditeurReferentielRoles
{
    /// <summary>Enregistre un rôle <b>neuf</b> dans le référentiel : persiste son libellé sur
    /// l'identifiant stable opaque fourni (jamais un id existant).</summary>
    void Creer(string roleId, string libelle);
}
