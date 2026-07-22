using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.InMemory.Activites.Repositories;

// Le segment de namespace « .Foyer » masque la classe seed Foyer (Application.Foyer.Seed) : alias
// scopé au namespace (gagne sur le membre de namespace externe) pour relire le référentiel d'amorçage.
using Foyer = PlanningDeGarde.Application.Foyer.Seed.Foyer;

/// <summary>
/// Store mutable en mémoire du référentiel de lieux du foyer (petit agrégat de config foyer, miroir
/// strict du référentiel de rôles). <b>Seedé</b> à la construction depuis le <see cref="Foyer.Activites"/>
/// en dur (parité seed acteurs InMemory) — les lieux historiques du foyer restent disponibles à la
/// saisie, puis éditable en session. Réalise le port de lecture <see cref="IEnumerationActivites"/> et
/// le port d'écriture <see cref="IEditeurActivites"/> sur un dictionnaire id→libellé. Volatile (re-seedé
/// au redémarrage) ; le remplaçant durable est <c>ReferentielActivitesMongo</c> (sans seed). Les lieux
/// historiques portent leur libellé comme identifiant stable (préserve les slots déjà posés).
/// </summary>
public sealed class ReferentielActivitesEnMemoire : IEnumerationActivites, IEditeurActivites
{
    private readonly Dictionary<string, string> _libelles;
    // Adresse (s35 Sc.2) : surface OPTIONNELLE distincte du libellé, dans un dictionnaire séparé (miroir
    // acteur s33) — la changer ne touche jamais le libellé. Absente = adresse vide à l'énumération.
    private readonly Dictionary<string, string> _adresses = new();
    // Enfants liés (s35 Sc.3, lien N-M) : activiteId → ids d'enfants, dict séparé (surface indépendante).
    private readonly Dictionary<string, List<string>> _enfantsLies = new();

    public ReferentielActivitesEnMemoire()
        => _libelles = Foyer.Activites.ToDictionary(lieu => lieu, lieu => lieu);

    public void Ajouter(string lieuId, string libelle)
        => _libelles[lieuId] = libelle; // le lieu neuf existe désormais sur son id stable

    public void Supprimer(string lieuId)
    {
        _libelles.Remove(lieuId);     // cesse d'être énuméré ; tolérant à l'absence (idempotence)
        _adresses.Remove(lieuId);     // l'adresse suit la suppression (miroir d'Ajouter)
        _enfantsLies.Remove(lieuId);  // les liens enfant suivent la suppression
    }

    public void Renommer(string activiteId, string libelle)
        => _libelles[activiteId] = libelle; // libellé seul ; l'adresse (dict séparé) reste intacte

    public void ChangerAdresse(string activiteId, string adresse)
        => _adresses[activiteId] = adresse; // surface optionnelle distincte ; vide licite ; libellé intact

    public void LierEnfant(string activiteId, string enfantId)
    {
        var lies = _enfantsLies.TryGetValue(activiteId, out var l) ? l : (_enfantsLies[activiteId] = new List<string>());
        if (!lies.Contains(enfantId)) lies.Add(enfantId); // sans doublon (déjà lié = no-op) ; libellé/adresse intacts
    }

    public void DelierEnfant(string activiteId, string enfantId)
    {
        if (_enfantsLies.TryGetValue(activiteId, out var lies))
            lies.Remove(enfantId); // tolérant à l'absence (idempotence) ; autres liens/champs intacts
    }

    public IReadOnlyCollection<ActiviteFoyer> EnumererActivites()
        => _libelles.Select(kv => new ActiviteFoyer(
            kv.Key, kv.Value, _adresses.TryGetValue(kv.Key, out var a) ? a : "")
        {
            EnfantsLies = _enfantsLies.TryGetValue(kv.Key, out var e) ? e.ToList() : new List<string>()
        }).ToList();
}
