using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande de pose d'un slot récurrent hebdomadaire par un Parent.</summary>
public sealed record PoserSlotRecurrentCommand(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin);

/// <summary>
/// Use case : poser un slot récurrent hebdomadaire dans le planning partagé du foyer. Miroir de
/// <see cref="PoserSlotHandler"/> : même validation d'existence du lieu (référentiel vivant), même
/// délégation de l'invariant temporel à l'agrégat, même diffusion temps réel.
/// </summary>
public sealed class PoserSlotRecurrentHandler
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly IEnumerationLieux _lieux;
    private readonly INotificateurPlanning _notificateur;

    public PoserSlotRecurrentHandler(ISlotRecurrentRepository slots, IEnumerationLieux lieux, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _lieux = lieux;
        _notificateur = notificateur;
    }

    public Result<SlotRecurrentSnapshot> Handle(PoserSlotRecurrentCommand commande)
    {
        // Existence lue sur le référentiel de lieux VIVANT (IEnumerationLieux) — miroir strict de
        // PoserSlotHandler : un lieu inconnu du foyer refuse la pose, sans écriture ni diffusion.
        if (_lieux.EnumererLieux().All(lieu => lieu.Id != commande.LieuId))
            return Result<SlotRecurrentSnapshot>.Echec("Le lieu visé n'existe pas dans les lieux du foyer.");

        var pose = SlotRecurrent.Poser(
            commande.EnfantId, commande.LieuId, commande.JourDeSemaine, commande.HeureDebut, commande.HeureFin);
        if (!pose.EstSucces)
            return Result<SlotRecurrentSnapshot>.Echec(pose.Motif!);

        var slot = pose.Valeur!;
        _slots.Enregistrer(slot);
        _notificateur.NotifierMiseAJour();
        return Result<SlotRecurrentSnapshot>.Succes(slot.ToSnapshot());
    }
}
