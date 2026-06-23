using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Builders;

/// <summary>
/// Builder de la commande de pose d'un slot de localisation.
/// Valeurs par défaut = scénario nominal (Léa à l'école 8h30–16h30 le 15/07).
/// </summary>
public sealed class SlotBuilder
{
    private string _enfantId = "lea";
    private string _lieuId = "ecole";
    private DateTime _debut = new(2025, 7, 15, 8, 30, 0);
    private DateTime _fin = new(2025, 7, 15, 16, 30, 0);

    public SlotBuilder PourEnfant(string enfantId) { _enfantId = enfantId; return this; }
    public SlotBuilder DansLieu(string lieuId) { _lieuId = lieuId; return this; }
    public SlotBuilder De(DateTime debut) { _debut = debut; return this; }
    public SlotBuilder A(DateTime fin) { _fin = fin; return this; }

    public PoserSlotCommand Build() => new(_enfantId, _lieuId, _debut, _fin);
}
