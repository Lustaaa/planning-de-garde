using System;

namespace PlanningDeGarde.Application.Foyer.Models;

/// <summary>
/// Avertissement de lecture (CQRS) : deux slots du même enfant le même jour se recouvrent.
/// Projection de lecture — n'est pas un invariant de l'agrégat SlotDeLocalisation.
/// </summary>
public sealed record AvertissementChevauchement(string EnfantId, DateTime Jour);
