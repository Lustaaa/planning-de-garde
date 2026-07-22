using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Periodes.Handlers;

/// <summary>
/// Commande de suppression d'une période de garde du planning partagé. L'<paramref name="PeriodeId"/>
/// est l'identifiant stable de la période (clé du store, jamais un libellé). La suppression est
/// <b>idempotente</b> : un identifiant absent / déjà supprimé est un no-op qui réussit.
/// </summary>
public sealed record SupprimerPeriodeCommand(string PeriodeId);

/// <summary>Confirmation d'une suppression aboutie : l'identifiant stable de la période retirée.</summary>
public sealed record SupprimerPeriodeResultat(string PeriodeId);

/// <summary>
/// Use case : supprimer une période de garde. Retire la période du store via le port d'écriture,
/// faisant re-résoudre sa case (repli surcharge &gt; fond &gt; neutre, acquis).
/// </summary>
public sealed class SupprimerPeriodeHandler
{
    private readonly IPeriodeRepository _periodes;

    public SupprimerPeriodeHandler(IPeriodeRepository periodes) => _periodes = periodes;

    public Result<SupprimerPeriodeResultat> Handle(SupprimerPeriodeCommand commande)
    {
        _periodes.Supprimer(commande.PeriodeId);
        return Result<SupprimerPeriodeResultat>.Succes(new SupprimerPeriodeResultat(commande.PeriodeId));
    }
}
