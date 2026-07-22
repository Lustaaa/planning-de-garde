using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'un slot récurrent — frontière publique pour les assertions / la persistance.
/// La récurrence est <b>hebdomadaire simple</b> : un jour de semaine + une plage horaire (début→fin,
/// sans date) + un lieu, pour un enfant. <paramref name="Id"/> est l'<b>identifiant stable</b> attribué
/// par le store (clé de suppression, jamais un libellé) ; vide tant que le slot n'a pas été persisté.
/// </summary>
public sealed record SlotRecurrentSnapshot(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
    bool ConditionneGarde = false, string PoseurId = "", string Id = "");

/// <summary>
/// Agrégat « où est l'enfant, chaque semaine » (axe localisation, orthogonal à la responsabilité —
/// le slot reste une LOCALISATION sans responsable). Miroir de
/// <see cref="SlotDeLocalisation"/>, mais la borne temporelle est un <b>jour de semaine récurrent</b>
/// et une plage horaire, jamais une date. Invariant porté : heure fin > heure début.
///
/// <para> : un slot peut être <b>conditionné à la garde</b> (« seulement les jours où l'enfant est
/// chez moi ») — <see cref="_conditionneGarde"/> — auquel cas il porte l'identité du <b>parent poseur</b>
/// (<see cref="_poseurId"/>). La projection ne matérialise alors ses occurrences que les jours où la
/// résolution de responsabilité (surcharge &gt; fond) désigne ce poseur. Un slot non conditionné (défaut)
/// conserve le comportement strictement inchangé (matérialisé sur tous ses jours de récurrence).</para>
/// </summary>
public sealed class SlotRecurrent
{
    private readonly string _enfantId;
    private readonly string _lieuId;
    private readonly DayOfWeek _jourDeSemaine;
    private readonly TimeSpan _heureDebut;
    private readonly TimeSpan _heureFin;
    private readonly bool _conditionneGarde;
    private readonly string _poseurId;

    private SlotRecurrent(
        string enfantId, string lieuId, DayOfWeek jourDeSemaine, TimeSpan heureDebut, TimeSpan heureFin,
        bool conditionneGarde, string poseurId)
    {
        _enfantId = enfantId;
        _lieuId = lieuId;
        _jourDeSemaine = jourDeSemaine;
        _heureDebut = heureDebut;
        _heureFin = heureFin;
        _conditionneGarde = conditionneGarde;
        _poseurId = poseurId;
    }

    public static Result<SlotRecurrent> Poser(
        string enfantId, string lieuId, DayOfWeek jourDeSemaine, TimeSpan heureDebut, TimeSpan heureFin,
        bool conditionneGarde = false, string poseurId = "")
    {
        if (heureFin <= heureDebut)
            return Result<SlotRecurrent>.Echec("La durée du slot récurrent doit être strictement positive.");

        return Result<SlotRecurrent>.Succes(
            new SlotRecurrent(enfantId, lieuId, jourDeSemaine, heureDebut, heureFin, conditionneGarde, poseurId));
    }

    public SlotRecurrentSnapshot ToSnapshot()
        => new(_enfantId, _lieuId, _jourDeSemaine, _heureDebut, _heureFin, _conditionneGarde, _poseurId);
}
