using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Projection de lecture (CQRS) des périodes de garde <b>couvrant</b> une date donnée — alimente la
/// dialog de suppression (4ᵉ usage du menu clic-case). Chaque période est rendue avec son identifiant
/// stable, ses bornes et son responsable (le <see cref="PeriodeSnapshot"/> de la frontière de
/// persistance). N'écrit jamais et ne déclenche <b>jamais</b> la diffusion temps réel.
/// </summary>
public sealed class PeriodesDuJourQuery
{
    private readonly IPeriodeRepository _periodes;

    public PeriodesDuJourQuery(IPeriodeRepository periodes) => _periodes = periodes;

    /// <summary>Périodes dont l'intervalle [Debut..Fin] couvre la <paramref name="date"/> (bornes incluses).</summary>
    public IReadOnlyList<PeriodeSnapshot> Lister(DateOnly date)
    {
        return _periodes.AllSnapshots()
            .Where(p => date >= DateOnly.FromDateTime(p.Debut) && date <= DateOnly.FromDateTime(p.Fin))
            .ToList();
    }
}
