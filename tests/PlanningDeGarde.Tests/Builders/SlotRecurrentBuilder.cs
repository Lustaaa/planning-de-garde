using System;
using System.Collections.Generic;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Builders;

/// <summary>
/// Builder de la commande de pose d'un slot récurrent hebdomadaire.
/// Valeurs par défaut = exemple PO (« Piscine tous les samedis de 11h30 à 12h15 » pour Léa).
/// <see cref="LesJours"/> (s54) bascule en mode MULTI-JOURS (set explicite, même vide) ; sans lui, la
/// commande reste mono-jour (le jour est celui de <see cref="LeJour"/>).
/// </summary>
public sealed class SlotRecurrentBuilder
{
    private string _enfantId = "lea";
    private string _lieuId = "piscine";
    private DayOfWeek _jour = DayOfWeek.Saturday;
    private TimeSpan _debut = new(11, 30, 0);
    private TimeSpan _fin = new(12, 15, 0);
    private IReadOnlyList<DayOfWeek>? _jours;

    public SlotRecurrentBuilder PourEnfant(string enfantId) { _enfantId = enfantId; return this; }
    public SlotRecurrentBuilder DansLieu(string lieuId) { _lieuId = lieuId; return this; }
    public SlotRecurrentBuilder LeJour(DayOfWeek jour) { _jour = jour; return this; }
    public SlotRecurrentBuilder LesJours(params DayOfWeek[] jours) { _jours = jours; return this; }
    public SlotRecurrentBuilder De(TimeSpan debut) { _debut = debut; return this; }
    public SlotRecurrentBuilder A(TimeSpan fin) { _fin = fin; return this; }

    public PoserSlotRecurrentCommand Build() => new(_enfantId, _lieuId, _jour, _debut, _fin, JoursDeSemaine: _jours);
}
