using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de liaison d'un enfant du foyer à un <b>parent-acteur</b> (config, s34). Le lien est un
/// enrichissement du modèle enfant (jamais une recréation — l'id de l'enfant reste la clé) : le handler
/// persiste le lien via le port d'écriture <see cref="IEditeurEnfants.LierParent"/>, relu ensuite par
/// la query de configuration dans <see cref="EnfantFoyer.ParentsLies"/>.
/// </summary>
public sealed record LierEnfantParentCommand(string EnfantId, string ActeurId);

/// <summary>Confirmation d'un lien abouti : l'enfant (id stable inchangé) et le parent-acteur lié.</summary>
public sealed record LierEnfantParentResultat(string EnfantId, string ActeurId);

/// <summary>
/// Use case : lier un enfant à un parent-acteur. Persiste le lien via le port d'écriture du référentiel,
/// sous les règles du lien (S2) — TOUTES vérifiées AVANT toute écriture (aucune écriture partielle en
/// cas de refus) : l'acteur doit exister, porter le rôle « Parent » (référentiel de rôles), lier un
/// parent déjà lié est neutre (idempotent, pas de doublon), et borne « 2 parents max ». Familles
/// recomposées / exactement-2-parents restent hors scope (spec R2/R3).
/// </summary>
public sealed class LierEnfantParentHandler
{
    private readonly IEnumerationEnfants _enfants;
    private readonly IEnumerationActeursFoyer _acteurs;
    private readonly IEnumerationRoles _roles;
    private readonly IEditeurEnfants _referentiel;

    public LierEnfantParentHandler(
        IEnumerationEnfants enfants,
        IEnumerationActeursFoyer acteurs,
        IEnumerationRoles roles,
        IEditeurEnfants referentiel)
    {
        _enfants = enfants;
        _acteurs = acteurs;
        _roles = roles;
        _referentiel = referentiel;
    }

    private const string RoleParent = "Parent";

    public Result<LierEnfantParentResultat> Handle(LierEnfantParentCommand commande)
    {
        // Règle 1 — l'acteur désigné doit EXISTER dans le référentiel des acteurs (jamais un lien fantôme).
        if (!_acteurs.EnumererActeurs().Contains(commande.ActeurId))
            return Result<LierEnfantParentResultat>.Echec("acteur inexistant");

        // Règle 2 — l'acteur doit porter le rôle « Parent » (référentiel de rôles, cadrage SM) : son id de
        // rôle est résolu en libellé sur le référentiel — seul un « Parent » est liable comme parent.
        var roleId = _acteurs.RoleDe(commande.ActeurId);
        var estParent = roleId is not null
            && _roles.EnumererRoles().Any(r => r.Id == roleId && r.Libelle == RoleParent);
        if (!estParent)
            return Result<LierEnfantParentResultat>.Echec("acteur non parent");

        // État courant du lien (relu par la query) : sert la neutralité « déjà lié » et la borne « 2 max ».
        var parentsCourants = _enfants.EnumererEnfants()
            .FirstOrDefault(e => e.Id == commande.EnfantId)?.ParentsLies
            ?? (IReadOnlyCollection<string>)System.Array.Empty<string>();

        // Règle 3 — parent DÉJÀ lié : opération NEUTRE (idempotente), aucune écriture, aucun doublon.
        if (parentsCourants.Contains(commande.ActeurId))
            return Result<LierEnfantParentResultat>.Succes(new LierEnfantParentResultat(commande.EnfantId, commande.ActeurId));

        // Règle 4 — borne « 2 parents max » : un 3ᵉ parent est refusé (les 2 liens existants intacts).
        if (parentsCourants.Count >= 2)
            return Result<LierEnfantParentResultat>.Echec("2 parents max");

        // Toutes les règles satisfaites : on persiste le lien (première écriture, après toutes les gardes).
        _referentiel.LierParent(commande.EnfantId, commande.ActeurId);
        return Result<LierEnfantParentResultat>.Succes(new LierEnfantParentResultat(commande.EnfantId, commande.ActeurId));
    }
}
