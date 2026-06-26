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

    /// <summary>
    /// Responsables du foyer comme paires (identifiant stable, libellé). Le sélecteur affiche le
    /// <see cref="Responsable.Libelle"/> mais bind l'<see cref="Responsable.Id"/> : le canal reçoit
    /// l'identifiant stable (<c>parent-a</c>/<c>parent-b</c>), clé atteignable du set de couleurs — et
    /// non le libellé qui retombait sur le gris neutre (cadrage (B), Sc.6).
    /// </summary>
    public static readonly IReadOnlyList<Responsable> Responsables = new[]
    {
        new Responsable("parent-a", "Parent A"),
        new Responsable("parent-b", "Parent B"),
    };
}

/// <summary>Un responsable du foyer : identifiant stable (bindé) et libellé (affiché).</summary>
public sealed record Responsable(string Id, string Libelle);
