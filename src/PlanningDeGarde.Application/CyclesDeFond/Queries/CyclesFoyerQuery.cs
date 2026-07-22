using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application.CyclesDeFond.Queries;

/// <summary>
/// Vue de lecture d'un cycle de fond <b>déclaré</b> pour la configuration du foyer : une affectation
/// d'index de semaine au responsable de fond. L'<see cref="IndexSemaine"/> (0.N-1) est l'identité
/// <b>stable</b> de l'entrée ; le <see cref="ResponsableId"/> est l'attribut persisté (identifiant
/// stable du responsable, jamais un libellé).
/// </summary>
public sealed record CycleDeclareVue(int IndexSemaine, string ResponsableId);

/// <summary>
/// Query de configuration du foyer côté cycle : restitue l'<b>intégralité</b> des cycles
/// (affectations de semaine) DÉCLARÉS du cycle de fond courant — corrige le trou de lecture (des
/// cycles déclarés n'apparaissaient pas dans la config). Lecture seule, jamais de
/// diffusion. Un foyer sans cycle déclaré renvoie une liste <b>vide</b> (pas d'erreur).
/// </summary>
public sealed class CyclesFoyerQuery
{
    private readonly IReferentielCycleDeFond _cycle;

    public CyclesFoyerQuery(IReferentielCycleDeFond cycle)
    {
        _cycle = cycle;
    }

    /// <summary>Lit le cycle DÉCLARÉ de l'enfant <paramref name="enfantId"/> — cycle propre à l'enfant si
    /// défini, sinon repli sur le cycle partagé. <c>null</c> (défaut) = mono-enfant antérieur (cycle partagé).</summary>
    public IReadOnlyList<CycleDeclareVue> Lire(string? enfantId = null)
    {
        var cycle = _cycle.CycleCourant(enfantId);
        if (cycle is null)
            return Array.Empty<CycleDeclareVue>(); // foyer sans cycle déclaré → liste vide, pas d'erreur

        return cycle.Affectations
            .Select(a => new CycleDeclareVue(a.Key, a.Value))
            .ToList();
    }
}
