using System.Collections.Generic;

namespace PlanningDeGarde.Web;

/// <summary>
/// Référentiel d'affichage du foyer (lieux, responsables) pour peupler les sélecteurs de l'IHM
/// WASM. Côté front, ce n'est qu'une aide de saisie : la validation réelle (existence du lieu, du
/// responsable) reste celle des use cases derrière l'API distante — l'UI ne décide rien.
/// </summary>
public static class Foyer
{
    public static readonly IReadOnlyList<string> Lieux = new[]
    {
        "école", "domicile A", "domicile B", "nounou"
    };

    public static readonly IReadOnlyList<string> Responsables = new[]
    {
        "Parent A", "Parent B"
    };
}
