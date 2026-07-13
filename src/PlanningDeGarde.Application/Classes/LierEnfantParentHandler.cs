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
public sealed record LierEnfantParentCommand(string EnfantId, string ActeurId, RoleDuLien Role = RoleDuLien.ParentLibre);

/// <summary>Confirmation d'un lien abouti : l'enfant (id stable inchangé) et le parent-acteur lié.</summary>
public sealed record LierEnfantParentResultat(string EnfantId, string ActeurId);

/// <summary>
/// Use case : lier un enfant à un parent-acteur. Persiste le lien via le port d'écriture du référentiel,
/// sous les règles du lien (S2) — TOUTES vérifiées AVANT toute écriture (aucune écriture partielle en
/// cas de refus) : l'acteur doit exister, <b>porter un rôle marqué « est rôle parent »</b> (option B1,
/// s36 — le <b>flag</b> du rôle est la source de vérité de l'éligibilité, jamais le libellé ni le
/// <see cref="TypeActeur"/>), lier un parent déjà lié est neutre (idempotent, pas de doublon), et borne
/// « 2 parents max ». Un acteur sans rôle, ou à rôle non marqué (Nounou/Grand-parent), n'est PAS liable.
/// Le <see cref="TypeActeur"/> reste au seul service du gating d'écriture R8/R9 (inchangé). Familles
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

    public Result<LierEnfantParentResultat> Handle(LierEnfantParentCommand commande)
    {
        // Règle 1 — l'acteur désigné doit EXISTER dans le référentiel des acteurs (jamais un lien fantôme).
        if (!_acteurs.EnumererActeurs().Contains(commande.ActeurId))
            return Result<LierEnfantParentResultat>.Echec("acteur inexistant");

        // Règle 2 (option B1, s36) — l'acteur doit PORTER UN RÔLE MARQUÉ « est rôle parent ». Le FLAG du
        // rôle (source de vérité unique, jamais le libellé — anti-piège s35 — ni le TypeActeur) qualifie
        // l'éligibilité : un acteur sans rôle, ou à rôle non marqué (Nounou/Grand-parent), n'est pas liable.
        var roleId = _acteurs.RoleDe(commande.ActeurId);
        var porteUnRoleParent = roleId is not null
            && _roles.EnumererRoles().Any(r => r.Id == roleId && r.EstRoleParent);
        if (!porteUnRoleParent)
            return Result<LierEnfantParentResultat>.Echec("acteur sans rôle-parent");

        // État courant du lien (relu par la query) : sert la neutralité « déjà lié » et la borne « 2 max ».
        var parentsCourants = _enfants.EnumererEnfants()
            .FirstOrDefault(e => e.Id == commande.EnfantId)?.ParentsLies
            ?? (IReadOnlyCollection<ParentLie>)System.Array.Empty<ParentLie>();

        // Parent DÉJÀ lié (s34) : re-lier n'AJOUTE pas un parent — c'est une mise à jour du rôle-du-lien
        // (s37, upsert par acteur, sans doublon). Ré-émettre le MÊME rôle reste neutre (idempotent).
        var dejaLie = parentsCourants.Any(p => p.ActeurId == commande.ActeurId);

        // Règle borne « 2 parents max » : un 3ᵉ parent NOUVEAU est refusé (les 2 liens existants intacts).
        // La mise à jour du rôle d'un parent déjà lié n'est PAS un ajout → la borne ne s'y applique pas.
        if (!dejaLie && parentsCourants.Count >= 2)
            return Result<LierEnfantParentResultat>.Echec("2 parents max");

        // Toutes les règles satisfaites : on persiste le lien avec son rôle-du-lien (après toutes les gardes).
        _referentiel.LierParent(commande.EnfantId, commande.ActeurId, commande.Role);
        return Result<LierEnfantParentResultat>.Succes(new LierEnfantParentResultat(commande.EnfantId, commande.ActeurId));
    }
}
