using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store du référentiel de lieux : réalise le port de lecture
/// <see cref="IEnumerationActivites"/> (énumérer) et le port d'écriture <see cref="IEditeurActivites"/>
/// (ajouter) sur un dictionnaire id→libellé. Sert à piloter la validation de pose (lieu existant /
/// inexistant) et à asserter que le handler a bien persisté le lieu via le port d'écriture.
/// </summary>
public sealed class FakeReferentielActivites : IEnumerationActivites, IEditeurActivites
{
    private readonly Dictionary<string, string> _libelles = new();
    private readonly Dictionary<string, string> _adresses = new(); // adresse optionnelle (s35 Sc.2), dict séparé

    /// <summary>Amorce une activité existante (id = libellé pour les activités historiques).</summary>
    public FakeReferentielActivites AvecActivite(string lieuId) { _libelles[lieuId] = lieuId; return this; }

    public void Ajouter(string lieuId, string libelle) => _libelles[lieuId] = libelle;

    public void Supprimer(string lieuId) { _libelles.Remove(lieuId); _adresses.Remove(lieuId); } // tolérant à l'absence

    public void Renommer(string activiteId, string libelle) => _libelles[activiteId] = libelle; // adresse intacte

    public void ChangerAdresse(string activiteId, string adresse) => _adresses[activiteId] = adresse; // libellé intact ; vide licite

    public IReadOnlyCollection<ActiviteFoyer> EnumererActivites()
        => _libelles.Select(kv => new ActiviteFoyer(
            kv.Key, kv.Value, _adresses.TryGetValue(kv.Key, out var a) ? a : "")).ToList();
}
