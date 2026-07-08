using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire des jetons de réinitialisation de mot de passe (s28, volet 1), réalisant
/// <see cref="IReferentielJetonsReset"/> sur un dictionnaire jeton→enregistrement : émission
/// (<c>Enregistrer</c>), relecture (<c>Trouver</c>) et consommation usage-unique (<c>Consommer</c>).
/// Volatile (re-parti vide au redémarrage) — le remplaçant durable est <c>ReferentielJetonsResetMongo</c>.
/// La clé est la valeur opaque du jeton, jamais l'email ni l'id du compte.
/// </summary>
public sealed class ReferentielJetonsResetEnMemoire : IReferentielJetonsReset
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
