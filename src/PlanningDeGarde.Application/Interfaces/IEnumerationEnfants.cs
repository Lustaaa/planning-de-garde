using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de <b>lecture</b> du référentiel d'enfants du foyer (petit agrégat de config foyer hissé en
/// 1er rang, s30 — miroir strict du référentiel de lieux s27) : énumère les enfants du foyer, chacun
/// porté par un identifiant stable opaque (jamais dérivé du prénom) et un prénom. Réalisé par le store
/// en Infrastructure (InMemory seedé tests-runtime / Mongo durable runtime, bornés à la config foyer) ;
/// l'Application n'en dépend pas. Alimente À LA FOIS la validation de pose d'un slot (l'enfant visé
/// existe-t-il ?, s30 S7) et le sélecteur d'enfant des dialogs de pose (jamais d'enfant en dur / fantôme).
/// </summary>
public interface IEnumerationEnfants
{
    /// <summary>Les enfants du référentiel (id stable opaque + prénom), tels que persistés.</summary>
    IReadOnlyCollection<EnfantFoyer> EnumererEnfants();
}

/// <summary>Un enfant du référentiel du foyer : identifiant stable opaque (clé, jamais dérivé du
/// prénom) et prénom d'affichage éditable.</summary>
public sealed record EnfantFoyer(string Id, string Prenom);
