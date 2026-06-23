using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Port de persistance des slots de localisation (planning partagé du foyer).</summary>
public interface ISlotRepository
{
    void Enregistrer(SlotDeLocalisation slot);
    IReadOnlyList<SlotSnapshot> AllSnapshots();
}
