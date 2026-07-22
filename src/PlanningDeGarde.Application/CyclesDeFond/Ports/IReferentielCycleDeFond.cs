using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.CyclesDeFond.Ports;

/// <summary>
/// Port du cycle de fond : lecture du cycle courant (consommé par <see cref="GrilleAgendaQuery"/>
/// pour résoudre la responsabilité par défaut d'un jour sans période explicite) et surface
/// d'écriture (<see cref="DefinirCycle"/>, alimentée par <see cref="DefinirCycleHandler"/>).
/// Réalisé par un adaptateur InMemory singleton en Infrastructure (cycle volatile ici — sa
/// durabilité est portée par un palier ultérieur). <c>null</c> = aucun cycle défini → pas de fond.
/// </summary>
public interface IReferentielCycleDeFond
{
    /// <summary>
    /// Cycle de fond courant de l'enfant <paramref name="enfantId"/> (: un cycle par enfant), ou
    /// <c>null</c> si aucun n'a été défini. <paramref name="enfantId"/> absent (<c>null</c>) = cycle
    /// partagé/legacy (mono-enfant antérieur). Un enfant sans cycle propre retombe sur le cycle partagé.
    /// </summary>
    CycleDeFond? CycleCourant(string? enfantId = null);

    /// <summary>
    /// Remplace le cycle courant de l'enfant <paramref name="enfantId"/> (dernière écriture gagne, sans
    /// version ni rejet). <paramref name="enfantId"/> absent = cycle partagé/legacy.
    /// </summary>
    void DefinirCycle(CycleDeFond cycle, string? enfantId = null);
}
