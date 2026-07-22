using System;

namespace PlanningDeGarde.Application.Commun.Ports;

/// <summary>
/// Port (abstraction injectable) fournissant l'horloge « du jour ». Les adaptateurs de gauche
/// (formulaires WASM) pré-remplissent leurs dates depuis <see cref="Aujourdhui"/> plutôt que depuis
/// une date figée ou <c>DateTime.Today</c> en dur — symétrie avec <c>Projeter(dateReference)</c> côté
/// lecture. Doublé en test (date fixée) pour le déterminisme : jamais <c>DateTime.Now</c> dans la vue.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Instant courant (date + heure) — pour les cas qui ont besoin de l'heure.</summary>
    DateTime Maintenant { get; }

    /// <summary>La date du jour (« aujourd'hui »), sans heure.</summary>
    DateOnly Aujourdhui { get; }
}
