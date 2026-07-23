using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Handlers;

/// <summary>Commande de pose d'un slot récurrent hebdomadaire par un Parent. Le slot peut être
/// <b>conditionné à la garde</b> — « seulement les jours où l'enfant est chez moi » — auquel cas
/// il porte l'identité du <paramref name="PoseurId"/> (parent courant) qui pilote son conditionnement. Non
/// conditionné par défaut (comportement strictement inchangé).
///
/// <para><see cref="JoursDeSemaine"/> (s54) porte la récurrence MULTI-JOURS : <c>null</c> (défaut) = mode
/// mono-jour hérité (le jour est <see cref="JourDeSemaine"/>) ; une liste NON nulle (même <b>vide</b>)
/// bascule en mode set — une liste vide est alors un set explicitement vide, refusé par l'agrégat AVANT
/// écriture.</para></summary>
public sealed record PoserSlotRecurrentCommand(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
    bool ConditionneGarde = false, string PoseurId = "", IReadOnlyList<DayOfWeek>? JoursDeSemaine = null);

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

        // MULTI-JOURS (s54) : une liste NON nulle pilote le set (même vide → refus par l'agrégat) ; null =
        // mode mono-jour hérité (le set devient le jour unique). L'agrégat porte l'invariant (set non vide,
        // dédoublonnage).
        var jours = commande.JoursDeSemaine ?? new[] { commande.JourDeSemaine };
        var pose = SlotRecurrent.Poser(
            commande.EnfantId, commande.LieuId, jours, commande.HeureDebut, commande.HeureFin,
            commande.ConditionneGarde, commande.PoseurId);
        if (!pose.EstSucces)
            return Result<SlotRecurrentSnapshot>.Echec(pose.Motif!);

        var slot = pose.Valeur!;
        _slots.Enregistrer(slot);
        _notificateur.NotifierMiseAJour();
        return Result<SlotRecurrentSnapshot>.Succes(slot.ToSnapshot());
    }
}
