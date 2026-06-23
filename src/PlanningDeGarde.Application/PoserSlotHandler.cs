using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande de pose d'un slot de localisation par un Parent.</summary>
public sealed record PoserSlotCommand(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

/// <summary>Use case : poser un slot de localisation dans le planning partagé du foyer.</summary>
public sealed class PoserSlotHandler
{
    private readonly ISlotRepository _slots;
    private readonly ILieuRepository _lieux;
    private readonly INotificateurPlanning _notificateur;

    public PoserSlotHandler(ISlotRepository slots, ILieuRepository lieux, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _lieux = lieux;
        _notificateur = notificateur;
    }

    public Result<SlotSnapshot> Handle(PoserSlotCommand commande)
    {
        var slot = SlotDeLocalisation.Poser(commande.EnfantId, commande.LieuId, commande.Debut, commande.Fin);
        _slots.Enregistrer(slot);
        _notificateur.NotifierMiseAJour();
        return Result<SlotSnapshot>.Succes(slot.ToSnapshot());
    }
}
