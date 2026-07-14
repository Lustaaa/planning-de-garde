using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de <b>désactivation</b> d'un compte utilisateur du foyer (sens OFF du toggle actif, s41 —
/// débloque le verrou ON s33). Cible un compte par son id stable opaque (s22) et fait passer son statut
/// Actif→Inactif via le port d'écriture <see cref="IEditeurComptes"/>. La mutation de statut est portée
/// par l'agrégat <see cref="CompteUtilisateur"/> (Domain pur, Tell-Don't-Ask). Aucun nouvel agrégat,
/// aucun store neuf : réutilise le référentiel de comptes s22. Le sens ON
/// (<see cref="ActiverCompteHandler"/>) reste strictement inchangé.
/// </summary>
public sealed record DesactiverCompteCommand(string CompteId);

/// <summary>Confirmation d'une désactivation aboutie : l'identifiant stable du compte désactivé.</summary>
public sealed record DesactiverCompteResultat(string CompteId);

/// <summary>
/// Use case : désactiver un compte. Refuse un compte inconnu AVANT toute écriture (aucune mutation),
/// puis persiste le statut « Inactif » du compte identifié via le port d'écriture du référentiel
/// (mutation ciblée du seul statut). Idempotent : désactiver un compte déjà Inactif est un no-op qui
/// réussit.
/// </summary>
public sealed class DesactiverCompteHandler
{
    private readonly IEnumerationComptes _comptes;
    private readonly IEditeurComptes _editeur;

    public DesactiverCompteHandler(IEnumerationComptes comptes, IEditeurComptes editeur)
    {
        _comptes = comptes;
        _editeur = editeur;
    }

    public Result<DesactiverCompteResultat> Handle(DesactiverCompteCommand commande)
    {
        // Garde « compte introuvable » (Sc.3, miroir de l'activation) : un id qui ne correspond à aucun
        // compte est refusé AVANT toute écriture — aucune mutation, aucun compte fantôme. Résolution sur
        // l'id lu du référentiel.
        if (!_comptes.EnumererComptes().Any(c => c.Id == commande.CompteId))
            return Result<DesactiverCompteResultat>.Echec("compte introuvable");

        _editeur.Desactiver(commande.CompteId);
        return Result<DesactiverCompteResultat>.Succes(new DesactiverCompteResultat(commande.CompteId));
    }
}
