using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de connexion locale par email (canal requête/réponse, comme toute écriture) : ouvre une
/// session serveur pour le compte utilisateur porté par l'email donné. La session ANCRE l'identité
/// réelle sur l'acteur lié 1-1 au compte (relation s22) ; l'impersonation lecture (s14) reste possible
/// au-dessus. Réutilise <see cref="IEnumerationComptes"/> (s22) en lecture — aucun nouvel agrégat.
/// </summary>
public sealed record SeConnecterCommand(string Email);

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

    public SeConnecterHandler(IEnumerationComptes comptes) => _comptes = comptes;

    public Result<SessionOuverte> Handle(SeConnecterCommand commande)
    {
        var compte = _comptes.EnumererComptes().First(c => c.Email == commande.Email);
        return Result<SessionOuverte>.Succes(new SessionOuverte(compte.ActeurId!));
    }
}
