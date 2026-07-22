using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

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
    private readonly IEnumerationRoles _roles;
    private readonly IEditeurReferentielRoles _referentiel;

    public CreerRoleHandler(IEnumerationRoles roles, IEditeurReferentielRoles referentiel)
    {
        _roles = roles;
        _referentiel = referentiel;
    }

    public Result<CreerRoleResultat> Handle(CreerRoleCommand commande)
    {
        // Garde « libellé requis » (Sc.3) : un libellé vide ou tout-espaces est refusé AVANT toute
        // génération d'id et toute écriture — aucun rôle vide persisté, référentiel inchangé.
        if (string.IsNullOrWhiteSpace(commande.Libelle))
            return Result<CreerRoleResultat>.Echec("libellé requis");

        // Garde « libellé déjà défini » (Sc.3) : refus si un rôle porte déjà ce libellé — aucun
        // doublon persisté, référentiel inchangé (unicité du libellé lue sur le référentiel courant).
        if (_roles.EnumererRoles().Any(r => r.Libelle == commande.Libelle))
            return Result<CreerRoleResultat>.Echec("libellé déjà défini");

        // Identifiant stable neuf OPAQUE, généré (jamais dérivé du libellé, anti-pattern s06) et
        // unique (GUID → jamais un id existant). Le libellé se résout ensuite sur cet id.
        var roleId = $"role-{Guid.NewGuid():N}";
        _referentiel.Creer(roleId, commande.Libelle);
        return Result<CreerRoleResultat>.Succes(new CreerRoleResultat(roleId));
    }
}
