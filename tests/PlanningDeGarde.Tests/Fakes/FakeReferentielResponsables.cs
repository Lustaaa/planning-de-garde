using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port d'accès au référentiel des responsables (identifiant stable → nom).
/// Miroir de <see cref="FakePaletteCouleurs"/> : référentiel explicite passé au constructeur ;
/// repli déterministe sur l'identifiant lui-même pour tout responsable absent du référentiel.
/// </summary>
public sealed class FakeReferentielResponsables : IReferentielResponsables
{
    private readonly IReadOnlyDictionary<string, string> _noms;

    public FakeReferentielResponsables(IReadOnlyDictionary<string, string> noms)
    {
        _noms = noms;
    }

    public string NomDe(string responsableId)
        => _noms.TryGetValue(responsableId, out var nom) ? nom : responsableId;
}
