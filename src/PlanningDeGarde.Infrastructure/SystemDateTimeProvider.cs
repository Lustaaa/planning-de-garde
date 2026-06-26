using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Implémentation système du port <see cref="IDateTimeProvider"/> : lit l'horloge réelle de la
/// machine. Seul cet adaptateur d'infrastructure touche <c>DateTime.Now</c> ; le reste de
/// l'application (vues, projections) dépend de l'abstraction, doublée en test pour le déterminisme.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Maintenant => DateTime.Now;

    public DateOnly Aujourdhui => DateOnly.FromDateTime(DateTime.Now);
}
