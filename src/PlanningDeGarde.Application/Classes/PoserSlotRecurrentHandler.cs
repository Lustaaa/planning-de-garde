using System;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Commande de pose d'un slot récurrent hebdomadaire par un Parent. Le slot peut être
/// <b>conditionné à la garde</b> (D1, s31) — « seulement les jours où l'enfant est chez moi » — auquel cas
/// il porte l'identité du <paramref name="PoseurId"/> (parent courant) qui pilote son conditionnement. Non
/// conditionné par défaut (comportement s29 strictement inchangé).</summary>
public sealed record PoserSlotRecurrentCommand(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
    bool ConditionneGarde = false, string PoseurId = "");

/// <summary>
/// Use case : poser un slot récurrent hebdomadaire dans le planning partagé du foyer. Miroir de
/// <see cref="PoserSlotHandler"/> : même validation d'existence du lieu (référentiel vivant), même
/// délégation de l'invariant temporel à l'agrégat, même diffusion temps réel.
/// </summary>
public sealed class PoserSlotRecurrentHandler
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly IEnumerationActivites _lieux;
    private readonly IEnumerationEnfants _enfants;
    private readonly INotificateurPlanning _notificateur;

    public PoserSlotRecurrentHandler(ISlotRecurrentRepository slots, IEnumerationActivites lieux, IEnumerationEnfants enfants, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _lieux = lieux;
        _enfants = enfants;
        _notificateur = notificateur;
    }

    public Result<SlotRecurrentSnapshot> Handle(PoserSlotRecurrentCommand commande)
    {
        // Existence lue sur le référentiel de lieux VIVANT (IEnumerationActivites) — miroir strict de
        // PoserSlotHandler : un lieu inconnu du foyer refuse la pose, sans écriture ni diffusion.
        if (_lieux.EnumererActivites().All(lieu => lieu.Id != commande.LieuId))
            return Result<SlotRecurrentSnapshot>.Echec("Le lieu visé n'existe pas dans les lieux du foyer.");

        // Existence de l'enfant lue sur le référentiel d'enfants VIVANT (IEnumerationEnfants, s30 S7) —
        // miroir strict de PoserSlotHandler : un enfant inconnu du foyer refuse la pose, sans écriture
        // ni diffusion (l'enfant n'est plus un fantôme transmis à l'aveugle).
        if (_enfants.EnumererEnfants().All(enfant => enfant.Id != commande.EnfantId))
            return Result<SlotRecurrentSnapshot>.Echec("L'enfant visé n'existe pas dans les enfants du foyer.");

        var pose = SlotRecurrent.Poser(
            commande.EnfantId, commande.LieuId, commande.JourDeSemaine, commande.HeureDebut, commande.HeureFin,
            commande.ConditionneGarde, commande.PoseurId);
        if (!pose.EstSucces)
            return Result<SlotRecurrentSnapshot>.Echec(pose.Motif!);

        var slot = pose.Valeur!;
        _slots.Enregistrer(slot);
        _notificateur.NotifierMiseAJour();
        return Result<SlotRecurrentSnapshot>.Succes(slot.ToSnapshot());
    }
}
