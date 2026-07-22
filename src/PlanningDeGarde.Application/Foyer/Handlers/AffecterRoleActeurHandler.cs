using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

/// <summary>
/// Commande d'affectation d'un rôle du référentiel à un acteur du foyer (config). Le rôle est désigné
/// par son <b>id de rôle</b> (jamais un libellé en dur) : la valeur provient exclusivement du
/// référentiel. Réutilise le chemin d'écriture de la config acteur augmenté d'un id de rôle.
/// </summary>
public sealed record AffecterRoleActeurCommand(string ActeurId, string RoleId);

/// <summary>Confirmation d'une affectation aboutie : l'acteur et l'id de rôle qu'il porte désormais.</summary>
public sealed record AffecterRoleActeurResultat(string ActeurId, string RoleId);

/// <summary>
/// Use case : affecter un rôle du référentiel à un acteur. <b>Borne dure</b> : un id de rôle absent
/// du référentiel est rejeté (le champ est fermé sur le référentiel, jamais un rôle en dur) — aucune
/// écriture, l'acteur conserve son rôle précédent. Sinon, persiste l'affectation via le port
/// d'écriture de la config acteur.
/// </summary>
public sealed class AffecterRoleActeurHandler
{
    private readonly IEnumerationRoles _roles;
    private readonly IEditeurConfigurationFoyer _configuration;

    public AffecterRoleActeurHandler(IEnumerationRoles roles, IEditeurConfigurationFoyer configuration)
    {
        _roles = roles;
        _configuration = configuration;
    }

    public Result<AffecterRoleActeurResultat> Handle(AffecterRoleActeurCommand commande)
    {
        // Borne dure : la valeur doit provenir du référentiel. Un id de rôle inconnu est rejeté AVANT
        // toute écriture — aucun rôle en dur, l'acteur conserve son rôle précédent.
        if (!_roles.EnumererRoles().Any(r => r.Id == commande.RoleId))
            return Result<AffecterRoleActeurResultat>.Echec("rôle hors référentiel");

        _configuration.AffecterRole(commande.ActeurId, commande.RoleId);
        return Result<AffecterRoleActeurResultat>.Succes(new AffecterRoleActeurResultat(commande.ActeurId, commande.RoleId));
    }
}
