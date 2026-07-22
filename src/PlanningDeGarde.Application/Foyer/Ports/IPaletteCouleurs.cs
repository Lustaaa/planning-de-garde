namespace PlanningDeGarde.Application.Foyer.Ports;

/// <summary>
/// Port d'accès au set de couleurs acteur → couleur (source de vérité avant
/// personnalisation par utilisateur). L'Infrastructure fournit l'implémentation depuis
/// le <c>Foyer</c> ; l'Application n'en dépend pas. Tout acteur absent du set reçoit la
/// couleur neutre (repli déterministe).
/// </summary>
public interface IPaletteCouleurs
{
    /// <summary>Couleur de repli pour un acteur absent du set.</summary>
    string CouleurNeutre { get; }

    /// <summary>Couleur associée à l'acteur, ou la couleur neutre s'il est absent du set.</summary>
    string CouleurDe(string acteurId);
}
