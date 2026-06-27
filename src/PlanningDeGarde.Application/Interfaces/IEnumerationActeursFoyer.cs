using System.Collections.Generic;

namespace PlanningDeGarde.Application;

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
}
