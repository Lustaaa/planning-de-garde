namespace PlanningDeGarde.Application.Foyer.Ports;

/// <summary>
/// Port d'<b>écriture</b> du référentiel de rôles du foyer (miroir écriture d'<see cref="IEnumerationRoles"/>) :
/// crée un rôle neuf sur un identifiant stable opaque (jamais dérivé du libellé — l'id est la clé).
/// Réalisé par le store mutable en Infrastructure (InMemory tests / Mongo runtime), consommé par
/// le handler de gestion du référentiel. Le renommage et la suppression seront ajoutés aux scénarios
/// suivants (borne YAGNI : ne crée que <see cref="Creer"/>).
/// </summary>
public interface IEditeurReferentielRoles
{
    /// <summary>Enregistre un rôle <b>neuf</b> dans le référentiel : persiste son libellé sur
    /// l'identifiant stable opaque fourni (jamais un id existant).</summary>
    void Creer(string roleId, string libelle);

    /// <summary>Affecte un nouveau libellé au rôle identifié de façon stable (l'id n'est jamais
    /// éditable — il est la clé) : dernière écriture gagne, aucun doublon (le même id reste un
    /// unique rôle).</summary>
    void Renommer(string roleId, string nouveauLibelle);

    /// <summary>Retire le rôle identifié de façon stable du référentiel : il cesse d'être énuméré.
    /// Tolérant à l'absence (un rôle déjà absent = no-op qui réussit — idempotence). Le repli
    /// « sans rôle » des acteurs porteurs est orchestré en amont par le use case.</summary>
    void Supprimer(string roleId);

    /// <summary>Pose ou retire le flag « est un rôle parent » sur le rôle identifié de façon stable
    /// (option) — surface DISTINCTE du libellé (le renommage ne réinitialise pas le flag, et
    /// inversement). Écriture bas niveau du flag (source de vérité de l'éligibilité) ; le use case
    /// idempotent avec vérification d'existence est <c>MarquerRoleParentHandler</c>.</summary>
    void MarquerParent(string roleId, bool estParent);
}
