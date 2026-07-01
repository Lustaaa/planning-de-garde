using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de renommage d'un rôle du référentiel du foyer (config). L'identifiant stable est la
/// clé (jamais éditable) ; seul le libellé change. Mute le référentiel via le port d'écriture
/// <see cref="IEditeurReferentielRoles"/> — dernière écriture gagne, aucun doublon.
/// </summary>
public sealed record RenommerRoleCommand(string RoleId, string NouveauLibelle);

/// <summary>Confirmation d'un renommage abouti : le rôle (id stable inchangé) et son nouveau libellé.</summary>
public sealed record RenommerRoleResultat(string RoleId, string Libelle);

/// <summary>
/// Use case : renommer un rôle du référentiel du foyer. Applique le nouveau libellé sur
/// l'identifiant stable via le port d'écriture — l'id reste inchangé.
/// </summary>
public sealed class RenommerRoleHandler
{
    private readonly IEditeurReferentielRoles _referentiel;

    public RenommerRoleHandler(IEditeurReferentielRoles referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<RenommerRoleResultat> Handle(RenommerRoleCommand commande)
    {
        _referentiel.Renommer(commande.RoleId, commande.NouveauLibelle);
        return Result<RenommerRoleResultat>.Succes(new RenommerRoleResultat(commande.RoleId, commande.NouveauLibelle));
    }
}
