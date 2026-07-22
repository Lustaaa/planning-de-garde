using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Periodes.Handlers;

/// <summary>Commande d'affectation de la responsabilité d'une période de garde. <paramref name="EnfantId"/>
/// SCOPE la période à l'enfant courant (hérité du sélecteur de vue, Option A) : elle n'entre que dans
/// la résolution de CET enfant. Absent (<c>""</c>) = période partagée/legacy (mono-enfant antérieur).</summary>
public sealed record AffecterPeriodeCommand(string ResponsableId, DateTime Debut, DateTime Fin, string EnfantId = "");

/// <summary>Use case : affecter un responsable sur un intervalle (période de garde).</summary>
public sealed class AffecterPeriodeHandler
{
    private readonly IPeriodeRepository _periodes;
    private readonly IResponsableRepository _responsables;

    public AffecterPeriodeHandler(IPeriodeRepository periodes, IResponsableRepository responsables)
    {
        _periodes = periodes;
        _responsables = responsables;
    }

    public Result<PeriodeSnapshot> Handle(AffecterPeriodeCommand commande)
    {
        var affectation = PeriodeDeGarde.Affecter(commande.ResponsableId, commande.Debut, commande.Fin, commande.EnfantId);
        if (!affectation.EstSucces)
            return Result<PeriodeSnapshot>.Echec(affectation.Motif!);

        var periode = affectation.Valeur!;
        _periodes.Enregistrer(periode);
        return Result<PeriodeSnapshot>.Succes(periode.ToSnapshot());
    }
}
