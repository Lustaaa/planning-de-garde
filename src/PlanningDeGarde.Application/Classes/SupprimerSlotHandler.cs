using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de suppression d'un slot de localisation du planning partagé. Le <paramref name="SlotId"/>
/// est l'identifiant stable du slot (clé du store, jamais un libellé). La suppression est
/// <b>idempotente</b> : un identifiant absent / déjà supprimé est un no-op qui réussit (Sc.5).
/// </summary>
public sealed record SupprimerSlotCommand(string SlotId);

/// <summary>Confirmation d'une suppression aboutie : l'identifiant stable du slot retiré.</summary>
public sealed record SupprimerSlotResultat(string SlotId);

/// <summary>
/// Use case : supprimer un slot de localisation. Retire le slot du store via le port d'écriture.
/// La suppression d'un slot n'ouvre <b>aucune règle de résolution</b> (un slot est une localisation,
/// pas une responsabilité) : le seul effet est le retrait du slot de sa (ses) case(s).
/// </summary>
public sealed class SupprimerSlotHandler
{
    private readonly ISlotRepository _slots;

    public SupprimerSlotHandler(ISlotRepository slots) => _slots = slots;

    public Result<SupprimerSlotResultat> Handle(SupprimerSlotCommand commande)
    {
        _slots.Supprimer(commande.SlotId);
        return Result<SupprimerSlotResultat>.Succes(new SupprimerSlotResultat(commande.SlotId));
    }
}
