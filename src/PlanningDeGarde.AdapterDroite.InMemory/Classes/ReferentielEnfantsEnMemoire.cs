using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire du référentiel d'enfants du foyer (petit agrégat de config foyer hissé en
/// 1er rang, s30 — miroir strict du référentiel de lieux s27). Réalise le port de lecture
/// <see cref="IEnumerationEnfants"/> (validation de pose + sélecteur d'enfant) et le port d'écriture
/// <see cref="IEditeurEnfants"/> (ajouter) sur un dictionnaire id→prénom. Volatile (re-parti au
/// redémarrage) ; le remplaçant durable est <c>ReferentielEnfantsMongo</c> (sans seed, S6).
/// </summary>
public sealed class ReferentielEnfantsEnMemoire : IEnumerationEnfants, IEditeurEnfants
{
    private readonly Dictionary<string, string> _prenoms = new();

    public void Ajouter(string enfantId, string prenom)
        => _prenoms[enfantId] = prenom; // l'enfant neuf existe désormais sur son id stable

    public IReadOnlyCollection<EnfantFoyer> EnumererEnfants()
        => _prenoms.Select(kv => new EnfantFoyer(kv.Key, kv.Value)).ToList();
}
