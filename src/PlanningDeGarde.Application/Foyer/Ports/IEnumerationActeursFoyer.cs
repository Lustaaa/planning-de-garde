using System.Collections.Generic;

namespace PlanningDeGarde.Application.Foyer.Ports;

/// <summary>
/// Accès de lecture d'énumération des acteurs du foyer <b>depuis le store</b> (et non la liste
/// statique front <c>Foyer.ActeursEditables</c>) : restitue les identifiants stables de tous les
/// acteurs — seeds <b>et</b> acteurs ajoutés en session — pour que l'écran de configuration liste
/// un acteur fraîchement ajouté (Sc.1 / Sc.6). Les noms et couleurs se résolvent ensuite sur
/// l'identifiant via <see cref="IReferentielResponsables"/> / <see cref="IPaletteCouleurs"/>
/// (résolution sur l'identifiant stable, jamais sur le libellé). Réalisé par le store en
/// Infrastructure ; l'Application n'en dépend pas.
/// </summary>
public interface IEnumerationActeursFoyer
{
    /// <summary>Identifiants stables de tous les acteurs du foyer (seeds + acteurs ajoutés).</summary>
    IReadOnlyCollection<string> EnumererActeurs();

    /// <summary>
    /// Type de l'acteur (Admin / Parent / Autre) résolu sur son identifiant stable, <b>en lecture
    /// seule</b> depuis la déclaration seed du foyer (D3, sprint 14). Un acteur absent de la
    /// déclaration de types — typiquement un acteur ajouté en session — retombe sur
    /// <see cref="TypeActeur.Parent"/> par défaut (aucune saisie ni persistance neuve de type, borne
    /// anti-cliquet règle 30). Sert à piloter le rôle de l'identité effective lors d'une impersonation
    /// bornée ; aucune écriture, aucun recalcul métier.
    /// </summary>
    TypeActeur TypeDe(string acteurId);

    /// <summary>
    /// Id de rôle porté par l'acteur (résolu sur son identifiant stable), ou <c>null</c> s'il n'en
    /// porte aucun (« sans rôle » = neutre assumé, attribut optionnel). L'id de rôle provient
    /// exclusivement du référentiel de rôles ; sa résolution en libellé se fait via
    /// <see cref="IEnumerationRoles"/>. Sert notamment au repli neutre des porteurs d'un rôle supprimé
    /// (Sc.6) et à l'affichage du rôle courant d'un acteur (Sc.8).
    /// </summary>
    string? RoleDe(string acteurId);

    /// <summary>
    /// Adresse de résidence de l'acteur (résolue sur son identifiant stable), ou <c>null</c> s'il n'en
    /// porte aucune (attribut <b>optionnel</b>). Champ de modèle neuf (s33) persisté au même titre que le
    /// nom / la couleur : relu par la query de configuration pour l'afficher, jamais recalculé. Une
    /// adresse vide écrite explicitement est restituée telle quelle (<see cref="string.Empty"/>).
    /// </summary>
    string? AdresseDe(string acteurId);
}
