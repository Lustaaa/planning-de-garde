using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Comptes.Handlers;

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
    private readonly IEnumerationComptes _comptes;
    private readonly IEditeurComptes _editeur;

    public ActiverCompteHandler(IEnumerationComptes comptes, IEditeurComptes editeur)
    {
        _comptes = comptes;
        _editeur = editeur;
    }

    public Result<ActiverCompteResultat> Handle(ActiverCompteCommand commande)
    {
        // Garde « compte introuvable » (Sc.3) : un id qui ne correspond à aucun compte est refusé
        // AVANT toute écriture — aucune mutation, aucun compte fantôme créé (Result échec, pas
        // d'exception silencieuse). Résolution sur l'id lu du référentiel.
        if (!_comptes.EnumererComptes().Any(c => c.Id == commande.CompteId))
            return Result<ActiverCompteResultat>.Echec("compte introuvable");

        _editeur.Activer(commande.CompteId);
        return Result<ActiverCompteResultat>.Succes(new ActiverCompteResultat(commande.CompteId));
    }
}

/// <summary>Confirmation d'une activation aboutie : l'identifiant stable du compte activé.</summary>
public sealed record ActiverCompteResultat(string CompteId);
