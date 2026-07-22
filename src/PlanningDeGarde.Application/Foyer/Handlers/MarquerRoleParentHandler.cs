using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

/// <summary>
/// Commande de bascule du flag « est un rôle parent » d'un rôle du référentiel (s36, B1) : l'identifiant
/// stable désigne le rôle, <see cref="EstParent"/> l'état voulu (coche/décoche pilotée par l'utilisateur).
/// Le flag est la <b>source de vérité</b> de l'éligibilité au lien enfant↔parent (jamais le libellé,
/// anti-piège s35). Mute le référentiel via le port d'écriture <see cref="IEditeurReferentielRoles"/>.
/// </summary>
public sealed record MarquerRoleParentCommand(string RoleId, bool EstParent);

/// <summary>Confirmation d'une bascule aboutie : le rôle (id stable) et son flag « est rôle parent » posé.</summary>
public sealed record MarquerRoleParentResultat(string RoleId, bool EstParent);

/// <summary>
/// Use case : marquer (ou démarquer) un rôle comme « rôle parent ». Le rôle doit EXISTER au référentiel
/// (sinon refus, aucune écriture). Poser le flag est idempotent (ré-émettre la même valeur est neutre —
/// dernière écriture gagne, aucun doublon). La décoche (<c>EstParent = false</c>) est pilotée par
/// l'utilisateur : le flag reste la seule source de vérité (jamais une reconnaissance de libellé).
/// </summary>
public sealed class MarquerRoleParentHandler
{
    private readonly IEnumerationRoles _roles;
    private readonly IEditeurReferentielRoles _referentiel;

    public MarquerRoleParentHandler(IEnumerationRoles roles, IEditeurReferentielRoles referentiel)
    {
        _roles = roles;
        _referentiel = referentiel;
    }

    public Result<MarquerRoleParentResultat> Handle(MarquerRoleParentCommand commande)
    {
        // Garde « rôle inexistant » : la bascule ne peut désigner qu'un rôle EXISTANT du référentiel —
        // sinon refus AVANT toute écriture (aucun flag fantôme posé sur un id absent), motif restitué.
        if (_roles.EnumererRoles().All(r => r.Id != commande.RoleId))
            return Result<MarquerRoleParentResultat>.Echec("rôle inexistant");

        // Pose le flag sur son id stable (write-through) : idempotent (même valeur ré-émise = neutre).
        _referentiel.MarquerParent(commande.RoleId, commande.EstParent);
        return Result<MarquerRoleParentResultat>.Succes(new MarquerRoleParentResultat(commande.RoleId, commande.EstParent));
    }
}
