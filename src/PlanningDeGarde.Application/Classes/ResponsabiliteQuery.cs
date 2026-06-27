using System;
using System.Linq;

namespace PlanningDeGarde.Application;

/// <summary>
/// Read model (CQRS) : qui est responsable à un instant donné ? Lit les périodes de garde
/// (intervalle [début, fin[). Au point de transfert entre deux périodes contiguës, la
/// responsabilité bascule du déposant (période antérieure) au récupérant (période postérieure).
/// </summary>
public sealed class ResponsabiliteQuery
{
    private readonly IPeriodeRepository _periodes;

    public ResponsabiliteQuery(IPeriodeRepository periodes) => _periodes = periodes;

    public string? ResponsableAu(DateTime instant)
        => _periodes.AllSnapshots()
            .Where(p => p.Debut <= instant && instant < p.Fin)
            .Select(p => p.ResponsableId)
            .FirstOrDefault();
}
