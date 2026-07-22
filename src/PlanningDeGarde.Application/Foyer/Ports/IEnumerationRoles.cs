using System.Collections.Generic;

namespace PlanningDeGarde.Application.Foyer.Ports;

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
/// libellé) + libellé d'affichage éditable + flag <see cref="EstRoleParent"/> « est un rôle parent »
/// (option). Le flag est la <b>source de vérité</b> de l'éligibilité au lien enfant↔parent
/// (jamais le libellé, anti-piège) : défaut <c>false</c> = neutre (une donnée antérieure sans flag
/// se relit non-parent, sans crash).</summary>
public sealed record RoleFoyer(string Id, string Libelle, bool EstRoleParent = false);
