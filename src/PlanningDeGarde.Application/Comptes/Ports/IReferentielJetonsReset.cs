using System;

namespace PlanningDeGarde.Application.Comptes.Ports;

/// <summary>
/// Port de droite du <b>stockage serveur des jetons de réinitialisation</b> de mot de passe (volet 5,
/// s25) : enregistre un jeton émis (Sc.11), le retrouve pour validation, et le consomme (usage unique,
/// Sc.13). Réalisé par un store en Infrastructure (doublé à la main dans les tests) ; l'Application n'en
/// dépend que par ce port. Le contrôle d'expiration se fait à la lecture, contre l'horloge injectée
/// (<see cref="IDateTimeProvider"/>), à partir de l'instant d'expiration porté par le jeton.
/// </summary>
public interface IReferentielJetonsReset
{
    /// <summary>Enregistre un jeton de réinitialisation émis (jeton opaque, compte visé, expiration).</summary>
    void Enregistrer(JetonReset jeton);

    /// <summary>Retrouve un jeton par sa valeur opaque, ou <c>null</c> s'il est inconnu du store.</summary>
    JetonReset? Trouver(string jeton);

    /// <summary>Consomme le jeton (usage unique) : une utilisation ultérieure est rejetée. Tolérant à
    /// l'absence (no-op).</summary>
    void Consommer(string jeton);
}

/// <summary>Un jeton de réinitialisation persisté côté serveur : valeur opaque (clé), id du compte
/// visé, instant d'expiration, et drapeau de consommation (usage unique). Un jeton est <b>valide</b>
/// s'il n'est ni consommé ni expiré (expiration comparée à l'horloge injectée).</summary>
public sealed record JetonReset(string Jeton, string CompteId, DateTime Expiration, bool Consomme);
