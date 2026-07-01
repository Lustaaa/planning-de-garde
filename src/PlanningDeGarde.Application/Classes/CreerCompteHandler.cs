using System;
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
    private readonly IEditeurComptes _comptes;

    public CreerCompteHandler(IEditeurComptes comptes)
    {
        _comptes = comptes;
    }

    public Result<CreerCompteResultat> Handle(CreerCompteCommand commande)
    {
        // Identifiant stable neuf OPAQUE, généré (jamais dérivé de l'email, anti-pattern s06) et
        // unique (GUID → jamais un id existant). L'email se résout ensuite sur cet id.
        var compteId = $"compte-{Guid.NewGuid():N}";
        _comptes.Creer(compteId, commande.Email, StatutCompte.Inactif, commande.ActeurId);
        return Result<CreerCompteResultat>.Succes(new CreerCompteResultat(compteId));
    }
}
