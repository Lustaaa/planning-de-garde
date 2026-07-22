using System.Collections.Generic;

namespace PlanningDeGarde.Application.Activites.Ports;

/// <summary>
/// Port de <b>lecture</b> du référentiel de lieux du foyer (petit agrégat de config foyer, miroir
/// strict du référentiel de rôles) : énumère les lieux disponibles à la saisie — chacun porté
/// par un identifiant stable et un libellé. Réalisé par le store en Infrastructure (InMemory seedé
/// tests / Mongo durable runtime, bornés à la config foyer) ; l'Application n'en dépend pas.
/// Remplace la liste en dur <c>Foyer.Activites</c> (static) : alimente À LA FOIS la validation de pose
/// d'un slot (le lieu visé existe-t-il ?) et les sélecteurs de lieu des dialogs, via ce seul canal
/// de lecture (jamais de lieu en dur).
/// </summary>
public interface IEnumerationActivites
{
    /// <summary>Les lieux du référentiel (id stable + libellé), tels que persistés.</summary>
    IReadOnlyCollection<ActiviteFoyer> EnumererActivites();
}

/// <summary>Une activité du référentiel du foyer : identifiant stable (clé), libellé d'affichage,
/// <b>adresse</b> (miroir strict de l'adresse acteur — champ <b>optionnel</b>, vide accepté,
/// défaut <see cref="string.Empty"/>) et les <b>enfants liés</b> (lien N-M enfant↔activité :
/// identifiants stables des enfants du référentiel liés à cette activité, 0.N). Pas de slots imbriqués
/// (hors périmètre). Propriété <c>init</c> pour <see cref="EnfantsLies"/> (constructeurs
/// positionnels 2/3-args préservés — la validation de pose et l'énumération existantes restent inchangées).</summary>
public sealed record ActiviteFoyer(string Id, string Libelle, string Adresse = "")
{
    /// <summary>Identifiants stables des enfants (référentiel) liés à cette activité (lien N-M) —
    /// résolus en prénoms par la colonne « Enfants liés ». Liste vide par défaut (lien optionnel).</summary>
    public IReadOnlyCollection<string> EnfantsLies { get; init; } = System.Array.Empty<string>();
}
