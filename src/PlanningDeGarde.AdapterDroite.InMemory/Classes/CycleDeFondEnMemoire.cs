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
    // Cycle partagé (clé "") + surcharges par enfant (s53) : un enfant sans cycle propre retombe sur le partagé.
    private readonly System.Collections.Generic.Dictionary<string, CycleDeFond> _cycles = new();

    public CycleDeFond? CycleCourant(string? enfantId = null)
        => _cycles.TryGetValue(enfantId ?? "", out var propre) ? propre
            : _cycles.TryGetValue("", out var partage) ? partage
            : null;

    public void DefinirCycle(CycleDeFond cycle, string? enfantId = null) => _cycles[enfantId ?? ""] = cycle;
}
