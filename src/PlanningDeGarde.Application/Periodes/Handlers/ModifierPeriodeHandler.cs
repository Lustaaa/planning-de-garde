using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Periodes.Handlers;

/// <summary>
/// Commande de modification d'une période existante. <paramref name="BaseObservee"/> est la
/// version affichée par l'auteur (jeton optimiste) ; <paramref name="Modification"/> l'état voulu.
/// </summary>
public sealed record ModifierPeriodeCommand(PeriodeSnapshot BaseObservee, PeriodeSnapshot Modification);

/// <summary>Use case : modifier une période avec contrôle d'écriture périmée (concurrence optimiste).</summary>
public sealed class ModifierPeriodeHandler
{
    private readonly IPeriodeRepository _periodes;

    public ModifierPeriodeHandler(IPeriodeRepository periodes) => _periodes = periodes;

    public Result<PeriodeSnapshot> Handle(ModifierPeriodeCommand commande)
    {
        var enregistree = _periodes.Modifier(commande.BaseObservee, commande.Modification);
        if (!enregistree)
            return Result<PeriodeSnapshot>.Echec(
                "Modification rejetée : l'état affiché est périmé, veuillez recharger la période à jour.");

        return Result<PeriodeSnapshot>.Succes(commande.Modification);
    }
}
