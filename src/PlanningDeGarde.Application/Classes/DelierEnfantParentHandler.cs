using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de retrait du lien d'un enfant vers un <b>parent-acteur</b> (config, s34). Retire le lien
/// via le port d'écriture <see cref="IEditeurEnfants.DelierParent"/> — l'id de l'enfant et ses autres
/// liens restent inchangés. Relu ensuite par la query dans <see cref="EnfantFoyer.ParentsLies"/>.
/// </summary>
public sealed record DelierEnfantParentCommand(string EnfantId, string ActeurId);

/// <summary>Confirmation d'un retrait abouti : l'enfant (id stable inchangé) et le parent-acteur délié.</summary>
public sealed record DelierEnfantParentResultat(string EnfantId, string ActeurId);

/// <summary>
/// Use case : délier un enfant d'un parent-acteur. Retire le lien via le port d'écriture du référentiel.
/// <b>Idempotent</b> : délier un parent déjà non lié est neutre (aucune écriture, aucune erreur) — la
/// neutralité est portée par le store (retrait tolérant à l'absence).
/// </summary>
public sealed class DelierEnfantParentHandler
{
    private readonly IEditeurEnfants _referentiel;

    public DelierEnfantParentHandler(IEditeurEnfants referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<DelierEnfantParentResultat> Handle(DelierEnfantParentCommand commande)
    {
        _referentiel.DelierParent(commande.EnfantId, commande.ActeurId);
        return Result<DelierEnfantParentResultat>.Succes(new DelierEnfantParentResultat(commande.EnfantId, commande.ActeurId));
    }
}
