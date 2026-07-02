using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

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

    public Result<SessionOuverte> Handle(SeConnecterCommand commande)
    {
        // Garde « email inconnu » (Sc.2) : aucun compte ne porte cet email → refus avec motif clair,
        // aucune session ouverte (le visiteur reste non connecté). Résolution sur l'email lu du référentiel.
        var compte = _comptes.EnumererComptes().FirstOrDefault(c => c.Email == commande.Email);
        if (compte is null)
            return Result<SessionOuverte>.Echec("email inconnu");

        // Garde « compte non activé » (Sc.3) : le statut Inactif (défaut de création s22) BORNE la
        // connexion → refus avec motif clair, aucune session. L'activation Inactif→Actif reste hors
        // scope (palier 13) : aucun chemin d'activation déclenché ici, le compte demeure Inactif.
        if (compte.Statut != StatutCompte.Actif)
            return Result<SessionOuverte>.Echec("compte non activé");

        return Result<SessionOuverte>.Succes(new SessionOuverte(compte.ActeurId!));
    }
}
