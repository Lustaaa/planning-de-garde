using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de <b>lecture</b> du référentiel des comptes utilisateurs du foyer (nouveau petit agrégat de
/// config foyer, miroir du référentiel de rôles s21) : énumère les comptes déclarés — chacun porté
/// par un identifiant stable opaque, un email, un statut, et l'id stable de l'acteur associé
/// (association 1-1). Réalisé par le store en Infrastructure (InMemory tests / Mongo runtime, bornés
/// à la config foyer) ; l'Application n'en dépend pas.
/// </summary>
public interface IEnumerationComptes
{
    /// <summary>Les comptes du référentiel (id stable opaque + email + statut + acteur associé),
    /// tels que persistés.</summary>
    IReadOnlyCollection<CompteUtilisateur> EnumererComptes();
}

/// <summary>Statut d'un compte utilisateur du foyer. Un compte neuf est <see cref="Inactif"/> par
/// défaut (l'activation viendra avec la prise en main de compte, palier 13).</summary>
public enum StatutCompte
{
    Inactif,
    Actif
}

/// <summary>Un compte utilisateur du référentiel du foyer : identifiant stable opaque (clé, jamais
/// dérivé de l'email), email, statut, et id stable de l'acteur associé (association 1-1).</summary>
public sealed record CompteUtilisateur(string Id, string Email, StatutCompte Statut, string ActeurId);
