using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application;

/// <summary>
/// Read model (CQRS) sur la journée d'un enfant : calcule les avertissements de
/// chevauchement entre slots du même enfant le même jour, par lecture du planning
/// partagé. N'écrit rien, ne touche jamais l'agrégat.
/// </summary>
public sealed class JourneeEnfantQuery
{
    private readonly ISlotRepository _slots;

    public JourneeEnfantQuery(ISlotRepository slots) => _slots = slots;

    public IReadOnlyList<AvertissementChevauchement> Chevauchements(string enfantId, System.DateTime jour)
    {
        var duJour = _slots.AllSnapshots()
            .Where(s => s.EnfantId == enfantId && s.Debut.Date == jour.Date)
            .ToList();

        var auMoinsUnRecouvrement = duJour
            .SelectMany((a, i) => duJour.Skip(i + 1).Select(b => (a, b)))
            .Any(p => p.a.Debut < p.b.Fin && p.b.Debut < p.a.Fin);

        return auMoinsUnRecouvrement
            ? new List<AvertissementChevauchement> { new(enfantId, jour.Date) }
            : new List<AvertissementChevauchement>();
    }
}
