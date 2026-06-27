using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port du cycle de fond : lecture du cycle courant (consommé par <see cref="GrilleAgendaQuery"/>
/// pour résoudre la responsabilité par défaut d'un jour sans période explicite) et surface
/// d'écriture (<see cref="DefinirCycle"/>, alimentée par <see cref="DefinirCycleHandler"/>).
/// Réalisé par un adaptateur InMemory singleton en Infrastructure (cycle volatile ici — sa
/// durabilité est portée par un palier ultérieur). <c>null</c> = aucun cycle défini → pas de fond.
/// </summary>
public interface IReferentielCycleDeFond
{
    /// <summary>Cycle de fond courant, ou <c>null</c> si aucun n'a été défini.</summary>
    CycleDeFond? CycleCourant();

    /// <summary>Remplace le cycle courant (dernière écriture gagne, sans version ni rejet).</summary>
    void DefinirCycle(CycleDeFond cycle);
}
