using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'un slot récurrent — frontière publique pour les assertions / la persistance.
/// La récurrence est <b>hebdomadaire</b> : un <b>set de jours</b> de semaine (<see cref="JoursDeSemaine"/>,
/// s54) + une plage horaire (début→fin, sans date) + un lieu, pour un enfant. <paramref name="Id"/> est
/// l'<b>identifiant stable</b> attribué par le store (clé de suppression, jamais un libellé) ; vide tant
/// que le slot n'a pas été persisté. <paramref name="JourDeSemaine"/> (positionnel, hérité s29) reste le
/// premier jour du set, pour les consommateurs qui lisent encore un jour unique.
/// </summary>
public sealed record SlotRecurrentSnapshot(
    string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
    bool ConditionneGarde = false, string PoseurId = "", string Id = "")
{
    /// <summary>Jours de semaine de la série (set multi-jours s54). Vide par défaut (snapshot mono-jour
    /// hérité : le jour effectif est porté par <see cref="JourDeSemaine"/>).</summary>
    public IReadOnlyList<DayOfWeek> JoursDeSemaine { get; init; } = Array.Empty<DayOfWeek>();
}

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
    private readonly IReadOnlyList<DayOfWeek> _joursDeSemaine;
    private readonly TimeSpan _heureDebut;
    private readonly TimeSpan _heureFin;
    private readonly bool _conditionneGarde;
    private readonly string _poseurId;

    private SlotRecurrent(
        string enfantId, string lieuId, IReadOnlyList<DayOfWeek> joursDeSemaine, TimeSpan heureDebut, TimeSpan heureFin,
        bool conditionneGarde, string poseurId)
    {
        _enfantId = enfantId;
        _lieuId = lieuId;
        _joursDeSemaine = joursDeSemaine;
        _heureDebut = heureDebut;
        _heureFin = heureFin;
        _conditionneGarde = conditionneGarde;
        _poseurId = poseurId;
    }

    /// <summary>
    /// Pose d'une série récurrente MULTI-JOURS (s54) : la récurrence porte un <b>set de jours</b> de la
    /// semaine. Invariants portés dans l'agrégat : le set doit cibler <b>au moins un jour</b> (set vide
    /// refusé AVANT écriture) et la plage horaire doit être strictement positive ; les jours dupliqués
    /// sont <b>dédoublonnés</b> (une seule occurrence par jour), en conservant l'ordre de première apparition.
    /// </summary>
    public static Result<SlotRecurrent> Poser(
        string enfantId, string lieuId, IReadOnlyList<DayOfWeek> joursDeSemaine, TimeSpan heureDebut, TimeSpan heureFin,
        bool conditionneGarde = false, string poseurId = "")
    {
        if (joursDeSemaine is null || joursDeSemaine.Count == 0)
            return Result<SlotRecurrent>.Echec("Un slot récurrent doit cibler au moins un jour de la semaine.");

        if (heureFin <= heureDebut)
            return Result<SlotRecurrent>.Echec("La durée du slot récurrent doit être strictement positive.");

        var joursUniques = joursDeSemaine.Distinct().ToList();
        return Result<SlotRecurrent>.Succes(
            new SlotRecurrent(enfantId, lieuId, joursUniques, heureDebut, heureFin, conditionneGarde, poseurId));
    }

    /// <summary>Surcharge hebdo mono-jour (s29) : délègue à la pose multi-jours avec un set d'un seul jour —
    /// comportement strictement inchangé (compatibilité des poses existantes).</summary>
    public static Result<SlotRecurrent> Poser(
        string enfantId, string lieuId, DayOfWeek jourDeSemaine, TimeSpan heureDebut, TimeSpan heureFin,
        bool conditionneGarde = false, string poseurId = "")
        => Poser(enfantId, lieuId, new[] { jourDeSemaine }, heureDebut, heureFin, conditionneGarde, poseurId);

    public SlotRecurrentSnapshot ToSnapshot()
        // JourDeSemaine (positionnel, mono-jour hérité) = premier jour du set — parité de persistance pour
        // les consommateurs qui lisent encore le jour unique ; JoursDeSemaine porte le set complet.
        => new(_enfantId, _lieuId, _joursDeSemaine[0], _heureDebut, _heureFin, _conditionneGarde, _poseurId)
        {
            JoursDeSemaine = _joursDeSemaine,
        };
}
