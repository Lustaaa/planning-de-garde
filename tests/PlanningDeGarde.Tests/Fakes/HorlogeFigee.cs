using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>Doublure à la main du port <see cref="IDateTimeProvider"/> : fige l'instant courant pour
/// prouver de façon DÉTERMINISTE l'expiration des jetons de réinitialisation (Sc.13) sans attendre le
/// temps réel. On avance l'horloge en mutant <see cref="Maintenant"/>.</summary>
public sealed class HorlogeFigee : IDateTimeProvider
{
    public HorlogeFigee(DateTime maintenant) => Maintenant = maintenant;

    public DateTime Maintenant { get; set; }

    public DateOnly Aujourdhui => DateOnly.FromDateTime(Maintenant);
}
