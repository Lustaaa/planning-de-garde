using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Store mutable en mémoire du référentiel de lieux du foyer (petit agrégat de config foyer, miroir
/// strict du référentiel de rôles s21). <b>Seedé</b> à la construction depuis le <see cref="Foyer.Lieux"/>
/// en dur (parité seed acteurs InMemory) — les lieux historiques du foyer restent disponibles à la
/// saisie —, puis éditable en session. Réalise le port de lecture <see cref="IEnumerationLieux"/> et
/// le port d'écriture <see cref="IEditeurLieux"/> sur un dictionnaire id→libellé. Volatile (re-seedé
/// au redémarrage) ; le remplaçant durable est <c>ReferentielLieuxMongo</c> (sans seed, S4). Les lieux
/// historiques portent leur libellé comme identifiant stable (préserve les slots déjà posés).
/// </summary>
public sealed class ReferentielLieuxEnMemoire : IEnumerationLieux, IEditeurLieux
{
    private readonly Dictionary<string, string> _libelles;

    public ReferentielLieuxEnMemoire()
        => _libelles = Foyer.Lieux.ToDictionary(lieu => lieu, lieu => lieu);

    public void Ajouter(string lieuId, string libelle)
        => _libelles[lieuId] = libelle; // le lieu neuf existe désormais sur son id stable

    public void Supprimer(string lieuId)
        => _libelles.Remove(lieuId); // cesse d'être énuméré ; tolérant à l'absence (idempotence)

    public IReadOnlyCollection<LieuFoyer> EnumererLieux()
        => _libelles.Select(kv => new LieuFoyer(kv.Key, kv.Value)).ToList();
}
