using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Projection de lecture (CQRS) de la cloche (s47) : le FLUX de notifications d'un utilisateur, dérivé du
/// JOURNAL DE CHANGEMENTS (trace de lecture). Retient les événements où l'utilisateur figure comme cédant
/// OU recevant (« le concernant »), triés par RÉCENCE de l'écriture (le plus récent en tête). N'écrit
/// jamais : lecture pure du journal, jamais de la résolution.
/// </summary>
public sealed class FluxNotificationsQuery
{
    private readonly IJournalChangements _journal;

    public FluxNotificationsQuery(IJournalChangements journal) => _journal = journal;

    /// <summary>Flux des événements concernant <paramref name="utilisateurId"/>, du plus récent au plus ancien.</summary>
    public IReadOnlyList<EvenementChangementSnapshot> Flux(string utilisateurId)
        => _journal.Tout()
            .Where(e => e.CedantId == utilisateurId || e.RecevantId == utilisateurId)
            .OrderByDescending(e => e.Horodatage)
            .ToList();
}
