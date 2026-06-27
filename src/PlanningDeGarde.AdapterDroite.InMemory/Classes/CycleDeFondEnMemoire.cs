using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur InMemory singleton du port <see cref="IReferentielCycleDeFond"/> : conserve le
/// cycle de fond courant en mémoire (volatile — durabilité portée par un palier ultérieur,
/// borne anti-cliquet règle 30, PAS Mongo). Source de vérité partagée du foyer ; une nouvelle
/// définition écrase la précédente (dernière écriture gagne, sans version ni rejet).
/// </summary>
public sealed class CycleDeFondEnMemoire : IReferentielCycleDeFond
{
    private CycleDeFond? _cycle;

    public CycleDeFond? CycleCourant() => _cycle;

    public void DefinirCycle(CycleDeFond cycle) => _cycle = cycle;
}
