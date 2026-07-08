using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du store du référentiel d'enfants : réalise le port de lecture
/// <see cref="IEnumerationEnfants"/> (énumérer) et le port d'écriture <see cref="IEditeurEnfants"/>
/// (ajouter / éditer) sur un dictionnaire id→prénom. Sert à piloter la validation de pose (enfant
/// existant / inconnu) et à asserter que le handler a bien persisté l'enfant via le port d'écriture.
/// </summary>
public sealed class FakeReferentielEnfants : IEnumerationEnfants, IEditeurEnfants
{
    private readonly Dictionary<string, string> _prenoms = new();

    /// <summary>Amorce un enfant existant sur un identifiant stable + prénom (par défaut id = prénom).</summary>
    public FakeReferentielEnfants AvecEnfant(string enfantId, string? prenom = null)
    {
        _prenoms[enfantId] = prenom ?? enfantId;
        return this;
    }

    public void Ajouter(string enfantId, string prenom) => _prenoms[enfantId] = prenom;

    public void Editer(string enfantId, string nouveauPrenom) => _prenoms[enfantId] = nouveauPrenom;

    public IReadOnlyCollection<EnfantFoyer> EnumererEnfants()
        => _prenoms.Select(kv => new EnfantFoyer(kv.Key, kv.Value)).ToList();
}
