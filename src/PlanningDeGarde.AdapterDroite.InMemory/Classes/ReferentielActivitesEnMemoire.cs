using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire du référentiel de lieux du foyer (petit agrégat de config foyer, miroir
/// strict du référentiel de rôles s21). <b>Seedé</b> à la construction depuis le <see cref="Foyer.Activites"/>
/// en dur (parité seed acteurs InMemory) — les lieux historiques du foyer restent disponibles à la
/// saisie —, puis éditable en session. Réalise le port de lecture <see cref="IEnumerationActivites"/> et
/// le port d'écriture <see cref="IEditeurActivites"/> sur un dictionnaire id→libellé. Volatile (re-seedé
/// au redémarrage) ; le remplaçant durable est <c>ReferentielActivitesMongo</c> (sans seed, S4). Les lieux
/// historiques portent leur libellé comme identifiant stable (préserve les slots déjà posés).
/// </summary>
public sealed class ReferentielActivitesEnMemoire : IEnumerationActivites, IEditeurActivites
{
    private readonly Dictionary<string, string> _libelles;
    // Adresse (s35 Sc.2) : surface OPTIONNELLE distincte du libellé, dans un dictionnaire séparé (miroir
    // acteur s33) — la changer ne touche jamais le libellé. Absente = adresse vide à l'énumération.
    private readonly Dictionary<string, string> _adresses = new();

    public ReferentielActivitesEnMemoire()
        => _libelles = Foyer.Activites.ToDictionary(lieu => lieu, lieu => lieu);

    public void Ajouter(string lieuId, string libelle)
        => _libelles[lieuId] = libelle; // le lieu neuf existe désormais sur son id stable

    public void Supprimer(string lieuId)
    {
        _libelles.Remove(lieuId); // cesse d'être énuméré ; tolérant à l'absence (idempotence)
        _adresses.Remove(lieuId); // l'adresse suit la suppression (miroir d'Ajouter)
    }

    public void Renommer(string activiteId, string libelle)
        => _libelles[activiteId] = libelle; // libellé seul ; l'adresse (dict séparé) reste intacte

    public void ChangerAdresse(string activiteId, string adresse)
        => _adresses[activiteId] = adresse; // surface optionnelle distincte ; vide licite ; libellé intact

    public IReadOnlyCollection<ActiviteFoyer> EnumererActivites()
        => _libelles.Select(kv => new ActiviteFoyer(
            kv.Key, kv.Value, _adresses.TryGetValue(kv.Key, out var a) ? a : "")).ToList();
}
