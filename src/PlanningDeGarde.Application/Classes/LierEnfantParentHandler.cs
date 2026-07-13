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
/// cas de refus) : l'acteur doit exister, être de type <see cref="TypeActeur.Parent"/> (option A, s36 —
/// même source de vérité que l'invariant admin=Parent, <see cref="AdministrationFoyer.DesignerAdmin"/>,
/// s22), lier un parent déjà lié est neutre (idempotent, pas de doublon), et borne « 2 parents max ».
/// Le libellé de rôle du référentiel (s21) ne qualifie PLUS l'éligibilité. Familles recomposées /
/// exactement-2-parents restent hors scope (spec R2/R3).
/// </summary>
public sealed class LierEnfantParentHandler
{
    private readonly IEnumerationEnfants _enfants;
    private readonly IEnumerationActeursFoyer _acteurs;
    private readonly IEditeurEnfants _referentiel;

    public LierEnfantParentHandler(
        IEnumerationEnfants enfants,
        IEnumerationActeursFoyer acteurs,
        IEditeurEnfants referentiel)
    {
        _enfants = enfants;
        _acteurs = acteurs;
        _referentiel = referentiel;
    }

    public Result<LierEnfantParentResultat> Handle(LierEnfantParentCommand commande)
    {
        // Règle 1 — l'acteur désigné doit EXISTER dans le référentiel des acteurs (jamais un lien fantôme).
        if (!_acteurs.EnumererActeurs().Contains(commande.ActeurId))
            return Result<LierEnfantParentResultat>.Echec("acteur inexistant");

        // Règle 2 (option A, s36) — l'acteur doit être PARENT PAR NATURE : TypeActeur.Parent. C'est
        // EXACTEMENT le prédicat déjà porté par l'invariant admin=Parent (DesignerAdmin, s22) — source de
        // vérité UNIQUE. Le libellé de rôle du référentiel (s21) est redevenu un attribut d'affichage libre
        // (Papa/Maman) : il ne qualifie PLUS l'éligibilité « parent liable ».
        var estParent = _acteurs.TypeDe(commande.ActeurId) == TypeActeur.Parent;
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
