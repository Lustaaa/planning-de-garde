using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main de <see cref="IEnumerationActeursFoyer"/> portant un <b>type explicite par
/// acteur</b> (Parent / Autre / …), pour piloter l'invariant admin=parent (Sc.4/Sc.5). Les acteurs
/// déclarés sont les clés de la table ; <see cref="TypeDe"/> résout le type déclaré (défaut Parent
/// pour un id absent, contrat s14). Le rôle n'est pas porté par ces scénarios (« sans rôle »).
/// </summary>
public sealed class FakeActeursTypes : IEnumerationActeursFoyer
{
    private readonly IReadOnlyDictionary<string, TypeActeur> _types;

    public FakeActeursTypes(IReadOnlyDictionary<string, TypeActeur> types)
    {
        _types = types;
    }

    public IReadOnlyCollection<string> EnumererActeurs() => _types.Keys.ToList();

    public TypeActeur TypeDe(string acteurId)
        => _types.TryGetValue(acteurId, out var type) ? type : TypeActeur.Parent;

    public string? RoleDe(string acteurId) => null;
}
