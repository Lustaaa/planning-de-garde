using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port du cycle de fond. Seedée au constructeur avec un cycle courant
/// (ou aucun) ; la dernière définition écrase le cycle (dernière écriture gagne, sans version).
/// </summary>
public sealed class FakeReferentielCycleDeFond : IReferentielCycleDeFond
{
    private CycleDeFond? _cycle;

    public FakeReferentielCycleDeFond(CycleDeFond? cycle = null)
    {
        _cycle = cycle;
    }

    public CycleDeFond? CycleCourant() => _cycle;

    public void DefinirCycle(CycleDeFond cycle) => _cycle = cycle;
}
