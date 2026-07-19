using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port du cycle de fond. Seedée au constructeur avec un cycle courant
/// (ou aucun) ; la dernière définition écrase le cycle (dernière écriture gagne, sans version).
/// </summary>
public sealed class FakeReferentielCycleDeFond : IReferentielCycleDeFond
{
    // Cycle partagé (clé "") + surcharges par enfant (s53) : un enfant sans cycle propre retombe sur le partagé.
    private readonly System.Collections.Generic.Dictionary<string, CycleDeFond> _cycles = new();

    public FakeReferentielCycleDeFond(CycleDeFond? cycle = null)
    {
        if (cycle is not null)
            _cycles[""] = cycle;
    }

    public CycleDeFond? CycleCourant(string? enfantId = null)
        => _cycles.TryGetValue(enfantId ?? "", out var propre) ? propre
            : _cycles.TryGetValue("", out var partage) ? partage
            : null;

    public void DefinirCycle(CycleDeFond cycle, string? enfantId = null) => _cycles[enfantId ?? ""] = cycle;
}
