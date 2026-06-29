using System;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Normalisation des <see cref="DateTime"/> du domaine pour Mongo. Les dates du planning de garde sont
/// des valeurs <b>wall-clock</b> sans fuseau (date + heure locales). BSON stocke un instant UTC en
/// millisecondes : sérialiser une valeur <c>Kind=Unspecified</c> déclenche une conversion locale→UTC
/// qui <b>décale la date d'un jour</b> près de minuit (selon le fuseau de la machine). On marque donc la
/// valeur <c>Kind=Utc</c> avant écriture (même horloge murale, aucune conversion) — durabilité stable,
/// indépendante du fuseau de l'hôte.
/// </summary>
internal static class DateTimeMongo
{
    public static DateTime WallClock(DateTime valeur) => DateTime.SpecifyKind(valeur, DateTimeKind.Utc);
}
