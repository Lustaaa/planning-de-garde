using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Builders;

/// <summary>
/// Builder de la commande d'affectation d'une période de garde.
/// Valeurs par défaut = scénario nominal (Parent A responsable du 14/07 au 21/07).
/// </summary>
public sealed class PeriodeBuilder
{
    private string _responsableId = "parent-a";
    private DateTime _debut = new(2025, 7, 14, 0, 0, 0);
    private DateTime _fin = new(2025, 7, 21, 0, 0, 0);

    public PeriodeBuilder PourResponsable(string responsableId) { _responsableId = responsableId; return this; }
    public PeriodeBuilder Du(DateTime debut) { _debut = debut; return this; }
    public PeriodeBuilder Au(DateTime fin) { _fin = fin; return this; }

    public AffecterPeriodeCommand Build() => new(_responsableId, _debut, _fin);
}
