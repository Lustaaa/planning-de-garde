using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Comptes.Handlers;

/// <summary>
/// Commande de connexion locale par email (canal requête/réponse, comme toute écriture) : ouvre une
/// session serveur pour le compte utilisateur porté par l'email donné. La session ANCRE l'identité
/// réelle sur l'acteur lié 1-1 au compte (relation s22) ; l'impersonation lecture (s14) reste possible
/// au-dessus. Réutilise <see cref="IEnumerationComptes"/> (s22) en lecture — aucun nouvel agrégat.
/// </summary>
public sealed record SeConnecterCommand(string Email, string? MotDePasse = null);

/// <summary>
/// Une session serveur ouverte : distingue l'identité <b>réelle</b> (l'acteur lié au compte connecté,
/// id stable) de l'identité <b>effective</b> (l'acteur incarné s'il y en a un, sinon repli sur la
/// réelle — impersonation bornée s14, non contournée). Sans incarnation, effective = réelle.
/// </summary>
public sealed record SessionOuverte(string IdentiteReelle)
{
    /// <summary>Identité effective = l'acteur incarné s'il y en a un, sinon l'identité réelle. Sans
    /// incarnation (cas de l'ouverture de session), résout sur la réelle (s14 non contournée).</summary>
    public string IdentiteEffective => IdentiteReelle;
}

/// <summary>
/// Use case : se connecter par email. Résout le compte sur son email (référentiel s22) et ouvre une
/// session serveur dont l'identité réelle est l'acteur lié 1-1 au compte.
/// </summary>
public sealed class SeConnecterHandler
{
    private readonly IEnumerationComptes _comptes;
    private readonly IHacheurMotDePasse _hacheur;

    public SeConnecterHandler(IEnumerationComptes comptes, IHacheurMotDePasse hacheur)
    {
        _comptes = comptes;
        _hacheur = hacheur;
    }

    /// <summary>Motif de refus NEUTRE (anti-énumération, Sc.8) : email inconnu et mauvais mot de passe
    /// partagent le MÊME motif — un attaquant ne peut pas déduire de la réponse qu'un email existe.</summary>
    private const string MotifIdentifiantsInvalides = "email ou mot de passe inconnu";

    public Result<SessionOuverte> Handle(SeConnecterCommand commande)
    {
        // Garde « email inconnu » (Sc.2) : aucun compte ne porte cet email → refus avec motif NEUTRE
        // (identique au mauvais mot de passe, Sc.8), aucune session ouverte. Résolution sur l'email lu.
        var compte = _comptes.EnumererComptes().FirstOrDefault(c => c.Email == commande.Email);
        if (compte is null)
            return Result<SessionOuverte>.Echec(MotifIdentifiantsInvalides);

        // Garde « compte non activé » (Sc.3) : le statut Inactif (défaut de création s22) BORNE la
        // connexion → refus avec motif clair, aucune session. L'activation Inactif→Actif reste hors
        // scope (palier 13) : aucun chemin d'activation déclenché ici, le compte demeure Inactif.
        if (compte.Statut != StatutCompte.Actif)
            return Result<SessionOuverte>.Echec("compte non activé");

        // Garde « mauvais mot de passe » (Sc.8) : quand le compte porte un mot de passe (facteur local
        // s25), le couple email+mot de passe doit VÉRIFIER contre le condensat persisté (hacheur injecté
        // Sc.7). Un mot de passe qui ne correspond pas → refus avec le MÊME motif neutre que l'email
        // inconnu (anti-énumération) — aucune session. Un compte sans mot de passe (email-only s23 /
        // OAuth) ne déclenche pas cette garde.
        if (compte.MotDePasseHache is not null && !_hacheur.Verifier(commande.MotDePasse ?? string.Empty, compte.MotDePasseHache))
            return Result<SessionOuverte>.Echec(MotifIdentifiantsInvalides);

        return Result<SessionOuverte>.Succes(new SessionOuverte(compte.ActeurId!));
    }
}
