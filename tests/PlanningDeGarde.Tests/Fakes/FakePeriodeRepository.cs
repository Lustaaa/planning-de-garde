using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de persistance des périodes de garde.
/// Copy-on-read : on stocke et on rend des snapshots, jamais l'agrégat vivant.
/// L'enregistrement attribue un identifiant stable (clé de suppression / d'édition).
/// </summary>
public sealed class FakePeriodeRepository : IPeriodeRepository
{
    private readonly List<PeriodeSnapshot> _periodes = new();

    public void Enregistrer(PeriodeDeGarde periode)
        => _periodes.Add(periode.ToSnapshot() with { Id = Guid.NewGuid().ToString() });

    public IReadOnlyList<PeriodeSnapshot> AllSnapshots() => _periodes.ToList();

    public void Supprimer(string periodeId) => _periodes.RemoveAll(p => p.Id == periodeId);

    public bool Modifier(PeriodeSnapshot baseObservee, PeriodeSnapshot modification)
    {
        var index = _periodes.IndexOf(baseObservee);
        if (index < 0)
            return false; // état périmé : la base observée n'est plus l'état courant

        _periodes[index] = modification;
        return true;
    }
}
