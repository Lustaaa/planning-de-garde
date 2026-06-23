using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Port de persistance des périodes de garde (planning partagé du foyer).</summary>
public interface IPeriodeRepository
{
    void Enregistrer(PeriodeDeGarde periode);
    IReadOnlyList<PeriodeSnapshot> AllSnapshots();
}
