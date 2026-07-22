using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Handlers;

/// <summary>
/// Commande de suppression d'un slot récurrent du planning partagé. Le <paramref name="SlotId"/> est
/// l'identifiant stable du slot récurrent (clé du store, jamais un libellé). La suppression est
/// <b>idempotente</b> : un identifiant absent / déjà supprimé est un no-op qui réussit (S7).
/// </summary>
public sealed record SupprimerSlotRecurrentCommand(string SlotId);

/// <summary>Confirmation d'une suppression aboutie : l'identifiant stable du slot récurrent retiré.</summary>
public sealed record SupprimerSlotRecurrentResultat(string SlotId);

/// <summary>
/// Use case : supprimer un slot récurrent. Retire le slot du store via le port d'écriture et diffuse la
/// mise à jour temps réel — ses occurrences disparaissent de toutes les cases à la re-projection.
/// </summary>
public sealed class SupprimerSlotRecurrentHandler
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly INotificateurPlanning _notificateur;

    public SupprimerSlotRecurrentHandler(ISlotRecurrentRepository slots, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _notificateur = notificateur;
    }

    public Result<SupprimerSlotRecurrentResultat> Handle(SupprimerSlotRecurrentCommand commande)
    {
        // Retrait idempotent (le port ne lève pas sur un identifiant absent) puis diffusion : les
        // occurrences du récurrent disparaissent de toutes les cases à la re-projection.
        _slots.Supprimer(commande.SlotId);
        _notificateur.NotifierMiseAJour();
        return Result<SupprimerSlotRecurrentResultat>.Succes(new SupprimerSlotRecurrentResultat(commande.SlotId));
    }
}
