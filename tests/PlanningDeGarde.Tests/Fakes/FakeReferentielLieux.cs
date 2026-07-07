using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store du référentiel de lieux : réalise le port de lecture
/// <see cref="IEnumerationLieux"/> (énumérer) et le port d'écriture <see cref="IEditeurLieux"/>
/// (ajouter) sur un dictionnaire id→libellé. Sert à piloter la validation de pose (lieu existant /
/// inexistant) et à asserter que le handler a bien persisté le lieu via le port d'écriture.
/// </summary>
public sealed class FakeReferentielLieux : IEnumerationLieux, IEditeurLieux
{
    private readonly Dictionary<string, string> _libelles = new();

    /// <summary>Amorce un lieu existant (id = libellé pour les lieux historiques).</summary>
    public FakeReferentielLieux AvecLieu(string lieuId) { _libelles[lieuId] = lieuId; return this; }

    public void Ajouter(string lieuId, string libelle) => _libelles[lieuId] = libelle;

    public IReadOnlyCollection<LieuFoyer> EnumererLieux()
        => _libelles.Select(kv => new LieuFoyer(kv.Key, kv.Value)).ToList();
}
