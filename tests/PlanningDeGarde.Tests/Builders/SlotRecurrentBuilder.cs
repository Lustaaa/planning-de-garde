using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Builders;

/// <summary>
/// Builder de la commande de pose d'un slot récurrent hebdomadaire.
/// Valeurs par défaut = exemple PO (« Piscine tous les samedis de 11h30 à 12h15 » pour Léa).
/// </summary>
public sealed class SlotRecurrentBuilder
{
    private string _enfantId = "lea";
    private string _lieuId = "piscine";
    private DayOfWeek _jour = DayOfWeek.Saturday;
    private TimeSpan _debut = new(11, 30, 0);
    private TimeSpan _fin = new(12, 15, 0);

    public SlotRecurrentBuilder PourEnfant(string enfantId) { _enfantId = enfantId; return this; }
    public SlotRecurrentBuilder DansLieu(string lieuId) { _lieuId = lieuId; return this; }
    public SlotRecurrentBuilder LeJour(DayOfWeek jour) { _jour = jour; return this; }
    public SlotRecurrentBuilder De(TimeSpan debut) { _debut = debut; return this; }
    public SlotRecurrentBuilder A(TimeSpan fin) { _fin = fin; return this; }

    public PoserSlotRecurrentCommand Build() => new(_enfantId, _lieuId, _jour, _debut, _fin);
}
