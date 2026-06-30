using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Port de persistance des slots de localisation (planning partagé du foyer).</summary>
public interface ISlotRepository
{
    void Enregistrer(SlotDeLocalisation slot);
    IReadOnlyList<SlotSnapshot> AllSnapshots();

    /// <summary>Remplace un slot existant (même enfant, même début) par sa version déplacée.</summary>
    void Remplacer(SlotSnapshot ancien, SlotDeLocalisation nouveau);

    /// <summary>
    /// Retire du store le slot d'identifiant stable <paramref name="slotId"/>. Idempotent :
    /// un identifiant absent / déjà supprimé est un no-op (jamais une erreur). Clé = l'identifiant
    /// stable, jamais un libellé.
    /// </summary>
    void Supprimer(string slotId);
}
