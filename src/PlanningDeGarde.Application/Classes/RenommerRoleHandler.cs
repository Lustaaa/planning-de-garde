using System.Linq;
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
    private readonly IEnumerationRoles _roles;
    private readonly IEditeurReferentielRoles _referentiel;

    public RenommerRoleHandler(IEnumerationRoles roles, IEditeurReferentielRoles referentiel)
    {
        _roles = roles;
        _referentiel = referentiel;
    }

    public Result<RenommerRoleResultat> Handle(RenommerRoleCommand commande)
    {
        // Garde « libellé requis » (Sc.3) : un nouveau libellé vide ou tout-espaces est refusé sans
        // muter le store — l'ancien libellé est conservé (référentiel inchangé).
        if (string.IsNullOrWhiteSpace(commande.NouveauLibelle))
            return Result<RenommerRoleResultat>.Echec("libellé requis");

        // Garde « libellé déjà défini » (Sc.3) : refus si un AUTRE rôle porte déjà ce libellé (le rôle
        // renommé s'exclut lui-même — renommer sur son propre libellé n'est pas un doublon). Aucun
        // doublon persisté, référentiel inchangé.
        if (_roles.EnumererRoles().Any(r => r.Id != commande.RoleId && r.Libelle == commande.NouveauLibelle))
            return Result<RenommerRoleResultat>.Echec("libellé déjà défini");

        _referentiel.Renommer(commande.RoleId, commande.NouveauLibelle);
        return Result<RenommerRoleResultat>.Succes(new RenommerRoleResultat(commande.RoleId, commande.NouveauLibelle));
    }
}
