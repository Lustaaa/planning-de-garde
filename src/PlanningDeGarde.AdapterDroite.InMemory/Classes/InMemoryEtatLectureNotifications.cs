using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>État LU / non-lu des notifications par utilisateur, en mémoire (process unique). Idempotent (set).</summary>
public sealed class InMemoryEtatLectureNotifications : IEtatLectureNotifications
{
    private readonly object _verrou = new();
    private readonly Dictionary<string, HashSet<string>> _lusParUtilisateur = new();

    public void MarquerLu(string utilisateurId, string evenementId)
    {
        lock (_verrou)
        {
            if (!_lusParUtilisateur.TryGetValue(utilisateurId, out var lus))
                _lusParUtilisateur[utilisateurId] = lus = new HashSet<string>();
            lus.Add(evenementId); // idempotent : HashSet, aucun doublon
        }
    }

    public IReadOnlyCollection<string> EvenementsLus(string utilisateurId)
    {
        lock (_verrou)
            return _lusParUtilisateur.TryGetValue(utilisateurId, out var lus) ? lus.ToList() : new List<string>();
    }
}
