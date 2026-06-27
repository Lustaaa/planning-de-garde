using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>Port de persistance des périodes de garde (planning partagé du foyer).</summary>
public interface IPeriodeRepository
{
    void Enregistrer(PeriodeDeGarde periode);
    IReadOnlyList<PeriodeSnapshot> AllSnapshots();

    /// <summary>
    /// Sauvegarde optimiste : remplace la période par <paramref name="modification"/> seulement si
    /// <paramref name="baseObservee"/> correspond encore à l'état courant (jeton de version =
    /// snapshot affiché par l'auteur). Renvoie false si l'écriture est périmée (état devancé).
    /// </summary>
    bool Modifier(PeriodeSnapshot baseObservee, PeriodeSnapshot modification);
}
