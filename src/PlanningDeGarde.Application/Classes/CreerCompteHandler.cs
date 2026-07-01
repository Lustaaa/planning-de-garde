using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de création d'un compte utilisateur dans le référentiel du foyer (config). Le handler
/// génère un identifiant stable neuf opaque (jamais dérivé de l'email, anti-pattern s06) et persiste
/// l'email, le statut « inactif » par défaut et l'id de l'acteur associé (association 1-1) via le
/// port d'écriture <see cref="IEditeurComptes"/>.
/// </summary>
public sealed record CreerCompteCommand(string Email, string ActeurId);

/// <summary>Confirmation d'une création aboutie : l'identifiant stable neuf généré pour le compte.</summary>
public sealed record CreerCompteResultat(string CompteId);

/// <summary>
/// Use case : créer un compte utilisateur dans le référentiel du foyer, associé 1-1 à un acteur
/// déclaré. Génère un identifiant stable neuf opaque, statut « inactif » par défaut, puis persiste
/// le compte via le port d'écriture du référentiel.
/// </summary>
public sealed class CreerCompteHandler
{
    private readonly IEnumerationComptes _comptes;
    private readonly IEditeurComptes _editeur;

    public CreerCompteHandler(IEnumerationComptes comptes, IEditeurComptes editeur)
    {
        _comptes = comptes;
        _editeur = editeur;
    }

    public Result<CreerCompteResultat> Handle(CreerCompteCommand commande)
    {
        // Garde « email requis » (Sc.2) : un email vide ou tout-espaces est refusé AVANT toute
        // génération d'id et toute écriture — aucun compte vide persisté, référentiel inchangé.
        if (string.IsNullOrWhiteSpace(commande.Email))
            return Result<CreerCompteResultat>.Echec("email requis");

        // Garde « email déjà utilisé » (Sc.2) : refus si un compte porte déjà cet email — aucun
        // doublon persisté, référentiel inchangé (unicité de l'email lue sur le référentiel courant).
        if (_comptes.EnumererComptes().Any(c => c.Email == commande.Email))
            return Result<CreerCompteResultat>.Echec("email déjà utilisé");

        // Identifiant stable neuf OPAQUE, généré (jamais dérivé de l'email, anti-pattern s06) et
        // unique (GUID → jamais un id existant). L'email se résout ensuite sur cet id.
        var compteId = $"compte-{Guid.NewGuid():N}";
        _editeur.Creer(compteId, commande.Email, StatutCompte.Inactif, commande.ActeurId);
        return Result<CreerCompteResultat>.Succes(new CreerCompteResultat(compteId));
    }
}
