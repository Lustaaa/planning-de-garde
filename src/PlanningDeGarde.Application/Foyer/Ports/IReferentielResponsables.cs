namespace PlanningDeGarde.Application.Foyer.Ports;

/// <summary>
/// Port d'accès au référentiel des responsables : identifiant stable → nom humain (libellé
/// d'affichage). Miroir strict d'<see cref="IPaletteCouleurs.CouleurDe"/> : la résolution se
/// fait sur l'identifiant stable (jamais sur le libellé — règle 17), avec repli déterministe
/// pour un identifiant absent du référentiel. L'Infrastructure fournit l'implémentation depuis
/// le <c>Foyer</c> ; l'Application n'en dépend pas.
/// </summary>
public interface IReferentielResponsables
{
    /// <summary>Nom humain associé à l'identifiant stable, ou un repli déterministe s'il est absent.</summary>
    string NomDe(string responsableId);
}
