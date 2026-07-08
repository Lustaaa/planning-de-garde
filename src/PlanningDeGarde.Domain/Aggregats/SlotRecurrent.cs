using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'un slot récurrent — frontière publique pour les assertions / la persistance.
/// La récurrence est <b>hebdomadaire simple</b> : un jour de semaine + une plage horaire (début→fin,
/// sans date) + un lieu, pour un enfant. <paramref name="Id"/> est l'<b>identifiant stable</b> attribué
/// par le store (clé de suppression, jamais un libellé) ; vide tant que le slot n'a pas été persisté.
/// </summary>
public sealed record SlotRecurrentSnapshot(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin, string Id = "");

/// <summary>
/// Agrégat « où est l'enfant, chaque semaine » (axe localisation, orthogonal à la responsabilité —
/// décision SM s29 : le slot reste une LOCALISATION sans responsable). Miroir de
/// <see cref="SlotDeLocalisation"/>, mais la borne temporelle est un <b>jour de semaine récurrent</b>
/// et une plage horaire, jamais une date. Invariant porté : heure fin > heure début.
/// </summary>
public sealed class SlotRecurrent
{
    private readonly string _enfantId;
    private readonly string _lieuId;
    private readonly DayOfWeek _jourDeSemaine;
    private readonly TimeSpan _heureDebut;
    private readonly TimeSpan _heureFin;

    private SlotRecurrent(string enfantId, string lieuId, DayOfWeek jourDeSemaine, TimeSpan heureDebut, TimeSpan heureFin)
    {
        _enfantId = enfantId;
        _lieuId = lieuId;
        _jourDeSemaine = jourDeSemaine;
        _heureDebut = heureDebut;
        _heureFin = heureFin;
    }

    public static Result<SlotRecurrent> Poser(string enfantId, string lieuId, DayOfWeek jourDeSemaine, TimeSpan heureDebut, TimeSpan heureFin)
    {
        if (heureFin <= heureDebut)
            return Result<SlotRecurrent>.Echec("La durée du slot récurrent doit être strictement positive.");

        return Result<SlotRecurrent>.Succes(new SlotRecurrent(enfantId, lieuId, jourDeSemaine, heureDebut, heureFin));
    }

    public SlotRecurrentSnapshot ToSnapshot() => new(_enfantId, _lieuId, _jourDeSemaine, _heureDebut, _heureFin);
}
