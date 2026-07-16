using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>Doublure à la main du port <see cref="IEtatLectureNotifications"/> (idempotent, par utilisateur).</summary>
public sealed class FakeEtatLectureNotifications : IEtatLectureNotifications
{
    private readonly Dictionary<string, HashSet<string>> _lus = new();

    public void MarquerLu(string utilisateurId, string evenementId)
    {
        if (!_lus.TryGetValue(utilisateurId, out var lus))
            _lus[utilisateurId] = lus = new HashSet<string>();
        lus.Add(evenementId);
    }

    public IReadOnlyCollection<string> EvenementsLus(string utilisateurId)
        => _lus.TryGetValue(utilisateurId, out var lus) ? lus.ToList() : new List<string>();
}
