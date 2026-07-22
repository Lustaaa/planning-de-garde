using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.InMemory.Enfants.Repositories;

/// <summary>
/// Store mutable en mémoire du référentiel d'enfants du foyer (petit agrégat de config foyer hissé en
/// 1er rang, — miroir strict du référentiel de lieux). Réalise le port de lecture
/// <see cref="IEnumerationEnfants"/> (validation de pose + sélecteur d'enfant) et le port d'écriture
/// <see cref="IEditeurEnfants"/> (ajouter) sur un dictionnaire id→prénom. Volatile (re-parti au
/// redémarrage) ; le remplaçant durable est <c>ReferentielEnfantsMongo</c> (sans seed).
/// </summary>
public sealed class ReferentielEnfantsEnMemoire : IEnumerationEnfants, IEditeurEnfants
{
    private readonly Dictionary<string, string> _prenoms = new();
    private readonly Dictionary<string, List<ParentLie>> _parents = new();

    public void Ajouter(string enfantId, string prenom)
        => _prenoms[enfantId] = prenom; // l'enfant neuf existe désormais sur son id stable

    public void Editer(string enfantId, string nouveauPrenom)
        => _prenoms[enfantId] = nouveauPrenom; // dernière écriture gagne sur l'id stable (clé inchangée)

    public void LierParent(string enfantId, string acteurId, RoleDuLien role = RoleDuLien.ParentLibre)
    {
        // Enrichissement du modèle enfant : le parent-acteur lié + son rôle-du-lien (s37). Upsert par
        // acteur : re-lier un parent déjà lié MET À JOUR son rôle-du-lien sans dupliquer le lien.
        if (!_parents.TryGetValue(enfantId, out var parents))
            _parents[enfantId] = parents = new List<ParentLie>();
        var index = parents.FindIndex(p => p.ActeurId == acteurId);
        if (index >= 0)
            parents[index] = new ParentLie(acteurId, role);
        else
            parents.Add(new ParentLie(acteurId, role));
    }

    public void DelierParent(string enfantId, string acteurId)
    {
        // Retrait du lien (id de l'enfant et autres liens inchangés). Tolérant à l'absence (idempotent).
        if (_parents.TryGetValue(enfantId, out var parents))
            parents.RemoveAll(p => p.ActeurId == acteurId);
    }

    public IReadOnlyCollection<EnfantFoyer> EnumererEnfants()
        => _prenoms.Select(kv => new EnfantFoyer(kv.Key, kv.Value, ParentsDe(kv.Key))).ToList();

    private IReadOnlyCollection<ParentLie> ParentsDe(string enfantId)
        => _parents.TryGetValue(enfantId, out var parents) ? parents.ToList() : System.Array.Empty<ParentLie>();
}
