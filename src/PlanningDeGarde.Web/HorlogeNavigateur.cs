using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web;

/// <summary>
/// Implémentation du port <see cref="IDateTimeProvider"/> côté front <b>WASM</b> : lit l'horloge
/// réelle du navigateur. Le front ne référençant pas l'Infrastructure (aucun code serveur dans le
/// navigateur), il porte sa propre horloge système. Doublée en test pour le déterminisme — la vue
/// dépend de l'abstraction, jamais de <c>DateTime.Today</c> en dur.
/// </summary>
public sealed class HorlogeNavigateur : IDateTimeProvider
{
    public DateTime Maintenant => DateTime.Now;

    public DateOnly Aujourdhui => DateOnly.FromDateTime(DateTime.Now);
}
