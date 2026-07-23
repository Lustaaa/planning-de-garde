using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Ports;

/// <summary>
/// Port de persistance des slots récurrents hebdomadaires (planning partagé du foyer). Miroir de
/// <see cref="ISlotRepository"/> pour la récurrence : l'enregistrement attribue un identifiant stable,
/// la suppression est idempotente (identifiant absent = no-op).
/// </summary>
public interface ISlotRecurrentRepository
{
    void Enregistrer(SlotRecurrent slot);
    IReadOnlyList<SlotRecurrentSnapshot> AllSnapshots();

    /// <summary>
    /// Réécrit EN PLACE le slot récurrent d'identifiant stable <paramref name="slotId"/> avec l'état de
    /// <paramref name="slot"/> — l'identifiant stable est <b>conservé</b> (édition d'une série s54, jamais
    /// une réinsertion sous un nouvel identifiant). Un identifiant absent est un no-op (parité idempotente
    /// avec <see cref="Supprimer"/>).
    /// </summary>
    void Remplacer(string slotId, SlotRecurrent slot);

    /// <summary>
    /// Retire du store le slot récurrent d'identifiant stable <paramref name="slotId"/>. Idempotent :
    /// un identifiant absent / déjà supprimé est un no-op (jamais une erreur). Clé = l'identifiant
    /// stable, jamais un libellé.
    /// </summary>
    void Supprimer(string slotId);
}
