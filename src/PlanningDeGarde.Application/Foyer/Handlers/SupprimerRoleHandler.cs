using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

/// <summary>
/// Commande de suppression d'un rôle du référentiel du foyer (config). Miroir du repli acteur
/// orphelin (s13/s19) : les acteurs qui portaient ce rôle retombent « sans rôle » (repli neutre),
/// jamais de rôle fantôme. Idempotente : supprimer un rôle absent est un no-op qui réussit.
/// </summary>
public sealed record SupprimerRoleCommand(string RoleId);

/// <summary>Confirmation d'une suppression aboutie (ou d'un no-op idempotent) : l'id du rôle visé.</summary>
public sealed record SupprimerRoleResultat(string RoleId);

/// <summary>
/// Use case : supprimer un rôle du référentiel. Fait d'abord retomber « sans rôle » les acteurs qui
/// le portaient (repli neutre, aucun rôle fantôme), puis retire le rôle du référentiel. Tolérant à
/// l'absence (idempotence).
/// </summary>
public sealed class SupprimerRoleHandler
{
    private readonly IEditeurReferentielRoles _referentiel;
    private readonly IEnumerationActeursFoyer _acteurs;
    private readonly IEditeurConfigurationFoyer _configuration;

    public SupprimerRoleHandler(
        IEditeurReferentielRoles referentiel,
        IEnumerationActeursFoyer acteurs,
        IEditeurConfigurationFoyer configuration)
    {
        _referentiel = referentiel;
        _acteurs = acteurs;
        _configuration = configuration;
    }

    public Result<SupprimerRoleResultat> Handle(SupprimerRoleCommand commande)
    {
        // Repli neutre : chaque acteur portant ce rôle retombe « sans rôle » (aucun rôle fantôme). Miroir
        // du repli acteur orphelin (s13/s19). Aucun porteur → aucune écriture (idempotence).
        foreach (var acteurId in _acteurs.EnumererActeurs())
            if (_acteurs.RoleDe(acteurId) == commande.RoleId)
                _configuration.RetirerRole(acteurId);

        // Le rôle disparaît du référentiel. Tolérant à l'absence (un rôle déjà supprimé = no-op réussit).
        _referentiel.Supprimer(commande.RoleId);
        return Result<SupprimerRoleResultat>.Succes(new SupprimerRoleResultat(commande.RoleId));
    }
}
