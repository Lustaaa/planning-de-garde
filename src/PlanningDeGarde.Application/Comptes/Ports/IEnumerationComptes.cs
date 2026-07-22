using System.Collections.Generic;

namespace PlanningDeGarde.Application.Comptes.Ports;

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
/// dérivé de l'email), email, statut, id stable de l'acteur associé (association 1-1), et condensat
/// du mot de passe (facteur local, volet 3 s25 — jamais le clair ; <c>null</c> pour un compte sans
/// mot de passe, ex. email-only s23 / OAuth). L'acteur est <c>null</c> quand le compte est
/// <b>désassocié</b> (repli après suppression de l'acteur associé, Sc.6) — jamais un compte fantôme
/// pointant un acteur absent.</summary>
public sealed record CompteUtilisateur(string Id, string Email, StatutCompte Statut, string? ActeurId, string? MotDePasseHache = null)
{
    /// <summary>Active le compte : le statut passe à <see cref="StatutCompte.Actif"/> (règle métier
    /// portée par l'agrégat, Tell-Don't-Ask). Seul le statut change ; email et acteur associé sont
    /// conservés (mutation immuable ciblée).</summary>
    public CompteUtilisateur Activer() => this with { Statut = StatutCompte.Actif };

    /// <summary>Désactive le compte : le statut passe à <see cref="StatutCompte.Inactif"/> (règle métier
    /// portée par l'agrégat, Tell-Don't-Ask — sens OFF s41). Seul le statut change ; email et acteur
    /// associé sont conservés (mutation immuable ciblée).</summary>
    public CompteUtilisateur Desactiver() => this with { Statut = StatutCompte.Inactif };
}
