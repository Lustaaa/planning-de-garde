using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Activites.Handlers;

/// <summary>
/// Commande de retrait du lien d'un enfant vers une <b>activité</b>. Retire le lien via
/// <see cref="IEditeurActivites.DelierEnfant"/> — les ids de l'enfant et de l'activité et les autres
/// liens restent inchangés. Relu ensuite par la query dans <see cref="ActiviteFoyer.EnfantsLies"/>.
/// </summary>
public sealed record DelierEnfantActiviteCommand(string EnfantId, string ActiviteId);

/// <summary>Confirmation d'un retrait abouti : l'enfant et l'activité déliés (ids stables inchangés).</summary>
public sealed record DelierEnfantActiviteResultat(string EnfantId, string ActiviteId);

/// <summary>
/// Use case : délier un enfant d'une activité. <b>Idempotent</b> : délier un lien déjà absent
/// est neutre (aucune écriture, aucune erreur) — la neutralité est portée par le store (retrait tolérant
/// à l'absence).
/// </summary>
public sealed class DelierEnfantActiviteHandler
{
    private readonly IEditeurActivites _referentiel;

    public DelierEnfantActiviteHandler(IEditeurActivites referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<DelierEnfantActiviteResultat> Handle(DelierEnfantActiviteCommand commande)
    {
        _referentiel.DelierEnfant(commande.ActiviteId, commande.EnfantId);
        return Result<DelierEnfantActiviteResultat>.Succes(new DelierEnfantActiviteResultat(commande.EnfantId, commande.ActiviteId));
    }
}
