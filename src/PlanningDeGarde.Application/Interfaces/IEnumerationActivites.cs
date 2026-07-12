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

/// <summary>Une activité du référentiel du foyer : identifiant stable (clé), libellé d'affichage et
/// <b>adresse</b> (s35 Sc.2, miroir strict de l'adresse acteur s33 — champ <b>optionnel</b>, vide accepté,
/// défaut <see cref="string.Empty"/>). Pas de slots imbriqués (hors périmètre, borne épic 6).</summary>
public sealed record ActiviteFoyer(string Id, string Libelle, string Adresse = "");
