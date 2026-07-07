using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande de pose d'un slot de localisation par un Parent.</summary>
public sealed record PoserSlotCommand(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

/// <summary>Use case : poser un slot de localisation dans le planning partagé du foyer.</summary>
public sealed class PoserSlotHandler
{
    private readonly ISlotRepository _slots;
    private readonly IEnumerationLieux _lieux;
    private readonly INotificateurPlanning _notificateur;

    public PoserSlotHandler(ISlotRepository slots, IEnumerationLieux lieux, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _lieux = lieux;
        _notificateur = notificateur;
    }

    public Result<SlotSnapshot> Handle(PoserSlotCommand commande)
    {
        // Existence lue sur le référentiel de lieux VIVANT (IEnumerationLieux), plus la liste en dur
        // Foyer.Lieux (trou s27) : un lieu fraîchement ajouté est immédiatement acceptable à la saisie.
        if (_lieux.EnumererLieux().All(lieu => lieu.Id != commande.LieuId))
            return Result<SlotSnapshot>.Echec("Le lieu visé n'existe pas dans les lieux du foyer.");

        var pose = SlotDeLocalisation.Poser(commande.EnfantId, commande.LieuId, commande.Debut, commande.Fin);
        if (!pose.EstSucces)
            return Result<SlotSnapshot>.Echec(pose.Motif!);

        var slot = pose.Valeur!;
        _slots.Enregistrer(slot);
        _notificateur.NotifierMiseAJour();
        return Result<SlotSnapshot>.Succes(slot.ToSnapshot());
    }
}
