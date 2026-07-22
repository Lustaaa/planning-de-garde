namespace PlanningDeGarde.Application.Planning.Models;

/// <summary>
/// Vue prédéfinie de la grille agenda : détermine le span (nombre de jours / lignes) projeté à
/// partir d'une ancre. Type framework-free de l'Application (CQRS lecture seule).
/// <list type="bullet">
///   <item><see cref="Semaine"/> — 7 jours / 1 ligne, ancrée au lundi de la semaine de l'ancre.</item>
///   <item><see cref="QuatreSemaines"/> — 28 jours / 4 lignes glissantes (défaut), ancrée au lundi.</item>
///   <item><see cref="Mois"/> — semaines ISO entières recouvrant le mois calendaire de l'ancre.</item>
/// </list>
/// </summary>
public enum VuePlanning
{
    Semaine,
    QuatreSemaines,
    Mois,
}
