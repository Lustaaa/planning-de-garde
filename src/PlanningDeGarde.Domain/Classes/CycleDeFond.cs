using System;
using System.Collections.Generic;
using System.Globalization;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Cycle de fond : répartition par défaut de la responsabilité de garde sur un cycle de
/// <see cref="NombreSemaines"/> semaines, une responsabilité par semaine. L'index de semaine
/// est dérivé du numéro de semaine ISO 8601 de la date (<c>index = ISOWeek mod N</c>), fonction
/// pure de la date (déterministe, sans horloge). Le mapping associe un index 0..N-1 à un
/// identifiant stable de responsable (jamais le libellé). Index non mappé = pas de fond.
/// </summary>
public sealed class CycleDeFond
{
    private readonly IReadOnlyDictionary<int, string> _affectations;

    public CycleDeFond(int nombreSemaines, IReadOnlyDictionary<int, string> affectations)
    {
        NombreSemaines = nombreSemaines;
        _affectations = affectations;
    }

    /// <summary>Longueur du cycle (nombre de semaines avant répétition).</summary>
    public int NombreSemaines { get; }

    /// <summary>
    /// Responsable de fond résolu pour la date donnée, ou <c>null</c> si aucun fond ne s'applique.
    /// Fonction pure de la date : <c>index = ISOWeek(date) mod N</c> (parité ISO 8601), résolu sur
    /// le mapping index→responsableId. Index non mappé → <c>null</c> (pas de fond → neutre), contrat
    /// de repli miroir de <c>IPaletteCouleurs.CouleurDe</c> / <c>IReferentielResponsables.NomDe</c>.
    /// </summary>
    public string? ResponsableDeFond(DateOnly date)
    {
        var index = ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)) % NombreSemaines;
        return _affectations.TryGetValue(index, out var responsableId) ? responsableId : null;
    }
}
