namespace PlanningDeGarde.Application.Comptes.Ports;

/// <summary>
/// Port d'<b>écriture</b> du référentiel des comptes utilisateurs du foyer (miroir écriture
/// d'<see cref="IEnumerationComptes"/>) : crée un compte neuf sur un identifiant stable opaque
/// (jamais dérivé de l'email — l'id est la clé), avec son email, son statut, et l'id stable de
/// l'acteur associé (association 1-1). Réalisé par le store mutable en Infrastructure (InMemory
/// tests / Mongo runtime, bornés à la config foyer), consommé par le handler de création de compte.
/// Association / désignation-admin / désassociation seront ajoutées aux scénarios suivants (borne
/// YAGNI : ne crée que <see cref="Creer"/>).
/// </summary>
public interface IEditeurComptes
{
    /// <summary>Enregistre un compte <b>neuf</b> dans le référentiel : persiste son email, son statut,
    /// l'id de l'acteur associé (<c>null</c> pour un compte <b>sans acteur</b>, ex. inscription
    /// libre-service — association ultérieure) et — le cas échéant — le condensat de son mot de
    /// passe (facteur local, ; <c>null</c> pour un compte sans mot de passe) sur
    /// l'identifiant stable opaque fourni (jamais un id existant).</summary>
    void Creer(string compteId, string email, StatutCompte statut, string? acteurId, string? motDePasseHache = null);

    /// <summary>Désassocie le compte identifié : il cesse de référencer un acteur (repli après
    /// suppression de l'acteur associé) — le compte survit, énuméré, sans acteur. Tolérant à
    /// l'absence / à un compte déjà désassocié (no-op qui réussit — idempotence).</summary>
    void Desassocier(string compteId);

    /// <summary>Active le compte identifié : persiste son statut « Actif » (mutation portée par
    /// l'agrégat <see cref="CompteUtilisateur"/>, Domain pur). Ne touche que le statut (email et
    /// acteur associé inchangés). Tolérant à l'absence (no-op).</summary>
    void Activer(string compteId);

    /// <summary>Désactive le compte identifié (sens OFF du toggle actif) : persiste son statut
    /// « Inactif » (mutation portée par l'agrégat <see cref="CompteUtilisateur"/>, Domain pur). Ne touche
    /// que le statut (email et acteur associé inchangés). Tolérant à l'absence / à un compte déjà Inactif
    /// (no-op qui réussit — idempotence).</summary>
    void Desactiver(string compteId);

    /// <summary>Redéfinit le mot de passe du compte identifié : persiste le nouveau condensat (facteur
    /// local, — récupération). Ne touche que le mot de passe (email, statut, acteur associé
    /// inchangés). Tolérant à l'absence (no-op).</summary>
    void RedefinirMotDePasse(string compteId, string motDePasseHache);
}
