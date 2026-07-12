using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de <b>lecture</b> du référentiel de lieux du foyer (petit agrégat de config foyer, miroir
/// strict du référentiel de rôles s21) : énumère les lieux disponibles à la saisie — chacun porté
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
/// <b>adresse</b> (s35 Sc.2, miroir strict de l'adresse acteur s33 — champ <b>optionnel</b>, vide accepté,
/// défaut <see cref="string.Empty"/>) et les <b>enfants liés</b> (s35 Sc.3 — lien N-M enfant↔activité :
/// identifiants stables des enfants du référentiel s30 liés à cette activité, 0..N). Pas de slots imbriqués
/// (hors périmètre, borne épic 6). Propriété <c>init</c> pour <see cref="EnfantsLies"/> (constructeurs
/// positionnels 2/3-args préservés — la validation de pose et l'énumération existantes restent inchangées).</summary>
public sealed record ActiviteFoyer(string Id, string Libelle, string Adresse = "")
{
    /// <summary>Identifiants stables des enfants (référentiel s30) liés à cette activité (lien N-M, s35 Sc.3) —
    /// résolus en prénoms par la colonne « Enfants liés » (Sc.4). Liste vide par défaut (lien optionnel).</summary>
    public IReadOnlyCollection<string> EnfantsLies { get; init; } = System.Array.Empty<string>();
}
