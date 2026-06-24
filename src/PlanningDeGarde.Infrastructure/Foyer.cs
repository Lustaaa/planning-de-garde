using System.Collections.Generic;

namespace PlanningDeGarde.Infrastructure;

/// <summary>Référentiel de départ du foyer (enfants, lieux, responsables) pour l'IHM.</summary>
public static class Foyer
{
    public static readonly IReadOnlyList<string> Enfants = new[] { "Léa" };

    public static readonly IReadOnlyList<string> Lieux = new[]
    {
        "école", "domicile A", "domicile B", "nounou"
    };

    public static readonly IReadOnlyList<string> Responsables = new[]
    {
        "Parent A", "Parent B"
    };
}
