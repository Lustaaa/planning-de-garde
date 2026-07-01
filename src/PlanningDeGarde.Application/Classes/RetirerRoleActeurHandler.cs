using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de retrait du rôle d'un acteur du foyer (config) : l'attribut rôle redevient non renseigné
/// (« sans rôle » = neutre assumé). Réutilise le chemin d'écriture de la config acteur.
/// </summary>
public sealed record RetirerRoleActeurCommand(string ActeurId);

/// <summary>Confirmation d'un retrait abouti : l'acteur, désormais « sans rôle ».</summary>
public sealed record RetirerRoleActeurResultat(string ActeurId);

/// <summary>
/// Use case : retirer le rôle d'un acteur. Ramène l'acteur à « sans rôle » (neutre) via le port
/// d'écriture de la config acteur — aucun rôle fantôme, tolérant à l'absence.
/// </summary>
public sealed class RetirerRoleActeurHandler
{
    private readonly IEditeurConfigurationFoyer _configuration;

    public RetirerRoleActeurHandler(IEditeurConfigurationFoyer configuration)
    {
        _configuration = configuration;
    }

    public Result<RetirerRoleActeurResultat> Handle(RetirerRoleActeurCommand commande)
    {
        _configuration.RetirerRole(commande.ActeurId);
        return Result<RetirerRoleActeurResultat>.Succes(new RetirerRoleActeurResultat(commande.ActeurId));
    }
}
