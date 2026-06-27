using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Port de persistance des transferts de bascule (planning partagé du foyer).</summary>
public interface ITransfertRepository
{
    void Enregistrer(Transfert transfert);
    IReadOnlyList<TransfertSnapshot> AllSnapshots();
}
