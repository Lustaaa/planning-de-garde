using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de <b>lecture</b> du référentiel de rôles du foyer (nouveau petit agrégat de config foyer,
/// miroir du CRUD acteurs) : énumère les rôles définis — chacun porté par un identifiant stable
/// opaque et un libellé éditable. Réalisé par le store en Infrastructure (InMemory tests / Mongo
/// runtime, bornés à la config foyer) ; l'Application n'en dépend pas. Alimente le sélecteur de
/// rôle borné au référentiel (jamais de rôle en dur).
/// </summary>
public interface IEnumerationRoles
{
    /// <summary>Les rôles du référentiel (id stable opaque + libellé), tels que persistés.</summary>
    IReadOnlyCollection<RoleFoyer> EnumererRoles();
}

/// <summary>Un rôle du référentiel du foyer : identifiant stable opaque (clé, jamais dérivé du
/// libellé) et libellé d'affichage éditable.</summary>
public sealed record RoleFoyer(string Id, string Libelle);
