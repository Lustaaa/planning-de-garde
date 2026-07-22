using System.Collections.Generic;

namespace PlanningDeGarde.Application.Enfants.Ports;

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
/// prénom), prénom d'affichage éditable et la <b>liste de ses parents liés</b> (0..2 liens, s34),
/// chacun enrichi de son <b>rôle-du-lien</b> (père / mère / parent-libre, s37). Le lien est
/// <b>optionnel</b> : un enfant sans aucun parent lié est valide (<see cref="ParentsLies"/> vide).</summary>
public sealed record EnfantFoyer(string Id, string Prenom, IReadOnlyCollection<ParentLie> ParentsLies)
{
    /// <summary>Enfant sans aucun parent lié (lien optionnel, 0 parent accepté).</summary>
    public EnfantFoyer(string Id, string Prenom) : this(Id, Prenom, System.Array.Empty<ParentLie>()) { }
}

/// <summary>Un lien enfant→parent (s34) enrichi de son <b>rôle-du-lien</b> (s37) : l'identifiant stable
/// du parent-acteur lié + le rôle-du-lien (père / mère / parent-libre) qui distingue les deux parents.
/// Défaut neutre <see cref="RoleDuLien.ParentLibre"/> (compat des liens déjà persistés sans attribut).
/// Constructeur unique (sérialisation JSON du canal de lecture : un seul ctor paramétré, pas d'ambiguïté).</summary>
public sealed record ParentLie(string ActeurId, RoleDuLien Role);
