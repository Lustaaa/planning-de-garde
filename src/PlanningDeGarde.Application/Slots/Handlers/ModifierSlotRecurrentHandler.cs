using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Handlers;

/// <summary>
/// Commande d'édition d'un slot récurrent existant (s54) désigné par son <paramref name="SlotId"/>
/// (identifiant stable, jamais un libellé). L'édition réécrit EN PLACE l'activité (<paramref name="LieuId"/>),
/// le set de jours, la plage horaire et le conditionnement à la garde — <b>l'enfant rattaché n'est jamais
/// changé</b> (il est relu depuis le slot existant, un slot ne migre jamais d'enfant).
/// </summary>
public sealed record ModifierSlotRecurrentCommand(
    string SlotId, string LieuId, IReadOnlyList<DayOfWeek> JoursDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
    bool ConditionneGarde = false, string PoseurId = "");

/// <summary>
/// Use case : éditer un slot récurrent existant. Relit le slot par son identifiant stable pour en
/// conserver l'enfant rattaché, reconstruit l'agrégat avec le nouvel état (invariants portés par
/// <see cref="SlotRecurrent.Poser"/>), le réécrit EN PLACE (même identifiant) puis diffuse la mise à
/// jour temps réel — la grille re-projette les nouvelles occurrences sans rechargement.
/// </summary>
public sealed class ModifierSlotRecurrentHandler
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly IEnumerationActivites _lieux;
    private readonly INotificateurPlanning _notificateur;

    public ModifierSlotRecurrentHandler(ISlotRecurrentRepository slots, IEnumerationActivites lieux, INotificateurPlanning notificateur)
    {
        _slots = slots;
        _lieux = lieux;
        _notificateur = notificateur;
    }

    public Result<SlotRecurrentSnapshot> Handle(ModifierSlotRecurrentCommand commande)
    {
        // Refus AVANT écriture (s54 Sc.5), dans l'ordre — aucune écriture partielle, le slot reste intact :
        //  1) identifiant absent du store → l'édition échoue explicitement (pas d'exception muette).
        var existant = _slots.AllSnapshots().FirstOrDefault(s => s.Id == commande.SlotId);
        if (existant is null)
            return Result<SlotRecurrentSnapshot>.Echec("Le slot récurrent à modifier n'existe pas.");

        //  2) activité inconnue du référentiel VIVANT (miroir strict de la pose) → refus + motif.
        if (_lieux.EnumererActivites().All(lieu => lieu.Id != commande.LieuId))
            return Result<SlotRecurrentSnapshot>.Echec("Le lieu visé n'existe pas dans les lieux du foyer.");

        // L'enfant rattaché est relu depuis le slot existant : une édition ne migre JAMAIS un slot vers un
        // autre enfant (isolation transverse s53).  3) plage invalide / set vide → refus porté par l'agrégat.
        var pose = SlotRecurrent.Poser(
            existant.EnfantId, commande.LieuId, commande.JoursDeSemaine, commande.HeureDebut, commande.HeureFin,
            commande.ConditionneGarde, commande.PoseurId);
        if (!pose.EstSucces)
            return Result<SlotRecurrentSnapshot>.Echec(pose.Motif!);

        _slots.Remplacer(commande.SlotId, pose.Valeur!);
        _notificateur.NotifierMiseAJour();
        return Result<SlotRecurrentSnapshot>.Succes(pose.Valeur!.ToSnapshot() with { Id = commande.SlotId });
    }
}
