using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application.Activites.Ports;
using PlanningDeGarde.Application.Slots.Ports;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Slots.Queries;

/// <summary>
/// Une activité récurrente d'un enfant, telle que présentée dans le tableau de la config foyer (s54) :
/// le <see cref="ActiviteLibelle"/> résolu du référentiel (s35), le set de <see cref="Jours"/> de la
/// série, la plage horaire début→fin et l'<see cref="Id"/> stable de la série (clé d'édition / de
/// suppression).
/// </summary>
public sealed record ActiviteRecurrenteVue(
    string Id,
    string LieuId,
    string ActiviteLibelle,
    IReadOnlyList<DayOfWeek> Jours,
    TimeSpan HeureDebut,
    TimeSpan HeureFin);

/// <summary>
/// Read model (CQRS) des activités récurrentes d'UN enfant pour la config foyer (s54). Projection de
/// lecture pure : lit les slots récurrents persistés et le référentiel de lieux (libellé), filtre
/// STRICTEMENT sur l'enfant demandé (isolation s53) et rend une ligne par série. N'écrit jamais et ne
/// déclenche aucune diffusion (aucune dépendance vers un handler / notificateur).
/// </summary>
public sealed class SlotsRecurrentsParEnfantQuery
{
    private readonly ISlotRecurrentRepository _slots;
    private readonly IEnumerationActivites _activites;

    public SlotsRecurrentsParEnfantQuery(ISlotRecurrentRepository slots, IEnumerationActivites activites)
    {
        _slots = slots;
        _activites = activites;
    }

    /// <summary>Les activités récurrentes de l'enfant <paramref name="enfantId"/> — uniquement les siennes.</summary>
    public IReadOnlyList<ActiviteRecurrenteVue> PourEnfant(string enfantId)
    {
        var libelles = _activites.EnumererActivites().ToDictionary(a => a.Id, a => a.Libelle);
        return _slots.AllSnapshots()
            .Where(s => s.EnfantId == enfantId)
            .Select(s => new ActiviteRecurrenteVue(
                s.Id,
                s.LieuId,
                libelles.TryGetValue(s.LieuId, out var libelle) ? libelle : s.LieuId,
                JoursDeLaSerie(s),
                s.HeureDebut,
                s.HeureFin))
            .ToList();
    }

    /// <summary>Jours de récurrence effectifs d'une série : le SET (s54) s'il est renseigné, sinon la
    /// retombée mono-jour héritée sur <see cref="SlotRecurrentSnapshot.JourDeSemaine"/>.</summary>
    private static IReadOnlyList<DayOfWeek> JoursDeLaSerie(SlotRecurrentSnapshot s)
        => s.JoursDeSemaine.Count > 0 ? s.JoursDeSemaine : new[] { s.JourDeSemaine };
}
