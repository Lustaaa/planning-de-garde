using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de création d'un rôle dans le référentiel du foyer (config). Le handler génère un
/// identifiant stable neuf opaque (jamais dérivé du libellé, anti-pattern s06) et persiste le
/// libellé via le port d'écriture <see cref="IEditeurReferentielRoles"/>.
/// </summary>
public sealed record CreerRoleCommand(string Libelle);

/// <summary>Confirmation d'une création aboutie : l'identifiant stable neuf généré pour le rôle.</summary>
public sealed record CreerRoleResultat(string RoleId);

/// <summary>
/// Use case : créer un rôle dans le référentiel du foyer. Génère un identifiant stable neuf opaque,
/// puis persiste le rôle via le port d'écriture du référentiel.
/// </summary>
public sealed class CreerRoleHandler
{
    private readonly IEditeurReferentielRoles _referentiel;

    public CreerRoleHandler(IEditeurReferentielRoles referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<CreerRoleResultat> Handle(CreerRoleCommand commande)
    {
        // Identifiant stable neuf OPAQUE, généré (jamais dérivé du libellé, anti-pattern s06) et
        // unique (GUID → jamais un id existant). Le libellé se résout ensuite sur cet id.
        var roleId = $"role-{Guid.NewGuid():N}";
        _referentiel.Creer(roleId, commande.Libelle);
        return Result<CreerRoleResultat>.Succes(new CreerRoleResultat(roleId));
    }
}
