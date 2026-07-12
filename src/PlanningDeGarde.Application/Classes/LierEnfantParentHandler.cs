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
/// Use case : lier un enfant à un parent-acteur. Chemin heureux du lien (S1) : persiste le lien via le
/// port d'écriture du référentiel. Les règles/rejets (2 parents max, acteur inexistant / non-parent /
/// déjà lié) relèvent de S2.
/// </summary>
public sealed class LierEnfantParentHandler
{
    private readonly IEditeurEnfants _referentiel;

    public LierEnfantParentHandler(IEditeurEnfants referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<LierEnfantParentResultat> Handle(LierEnfantParentCommand commande)
    {
        _referentiel.LierParent(commande.EnfantId, commande.ActeurId);
        return Result<LierEnfantParentResultat>.Succes(new LierEnfantParentResultat(commande.EnfantId, commande.ActeurId));
    }
}
