using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'activation d'un compte utilisateur du foyer (canal requête/réponse, comme toute
/// écriture) : cible un compte par son identifiant stable opaque (s22) et fait passer son statut
/// Inactif→Actif via le port d'écriture <see cref="IEditeurComptes"/>. La mutation de statut est
/// portée par l'agrégat <see cref="CompteUtilisateur"/> (Domain pur, Tell-Don't-Ask). Aucun nouvel
/// agrégat, aucun store neuf : réutilise le référentiel de comptes s22 (InMemory + Mongo config foyer).
/// </summary>
public sealed record ActiverCompteCommand(string CompteId);

/// <summary>
/// Use case : activer un compte. Persiste le statut « Actif » du compte identifié via le port
/// d'écriture du référentiel (mutation ciblée du seul statut).
/// </summary>
public sealed class ActiverCompteHandler
{
    private readonly IEditeurComptes _editeur;

    public ActiverCompteHandler(IEditeurComptes editeur) => _editeur = editeur;

    public Result<ActiverCompteResultat> Handle(ActiverCompteCommand commande)
    {
        _editeur.Activer(commande.CompteId);
        return Result<ActiverCompteResultat>.Succes(new ActiverCompteResultat(commande.CompteId));
    }
}

/// <summary>Confirmation d'une activation aboutie : l'identifiant stable du compte activé.</summary>
public sealed record ActiverCompteResultat(string CompteId);
