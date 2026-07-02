using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port de stockage serveur des jetons de réinitialisation (volet 5, s25).
/// Réalise <see cref="IReferentielJetonsReset"/> sur un dictionnaire jeton→enregistrement. Permet de
/// SEMER un jeton (Given des tests de redéfinition) et d'ASSERTER sa consommation. Le remplaçant durable
/// (Mongo) suivra le même port ; ici, volatile, pour prouver la logique Application/frontière.
/// </summary>
public sealed class FakeReferentielJetonsReset : IReferentielJetonsReset
{
    private readonly Dictionary<string, JetonReset> _jetons = new();

    public void Enregistrer(JetonReset jeton) => _jetons[jeton.Jeton] = jeton;

    public JetonReset? Trouver(string jeton) => _jetons.TryGetValue(jeton, out var j) ? j : null;

    public void Consommer(string jeton)
    {
        if (_jetons.TryGetValue(jeton, out var j))
            _jetons[jeton] = j with { Consomme = true };
    }
}
