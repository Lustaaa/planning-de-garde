using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Projection de lecture (CQRS) de la grille agenda du hub /planning. Construit la fenêtre
/// de 5 semaines (35 jours datés) à partir de la semaine de la date de référence injectée,
/// en lisant les slots et périodes enregistrés. N'écrit jamais : aucune dépendance vers un
/// handler ou un agrégat d'écriture (invariant « lecture seule » garanti par construction).
/// </summary>
public sealed class GrilleAgendaQuery
{
    private readonly ISlotRepository _slots;
    private readonly IPeriodeRepository _periodes;
    private readonly IPaletteCouleurs _palette;

    public GrilleAgendaQuery(ISlotRepository slots, IPeriodeRepository periodes, IPaletteCouleurs palette)
    {
        _slots = slots;
        _periodes = periodes;
        _palette = palette;
    }

    /// <summary>
    /// Projette la grille agenda à la <paramref name="dateReference"/> donnée (« aujourd'hui »,
    /// injecté pour le déterminisme — jamais <c>DateTime.Now</c>).
    /// </summary>
    public GrilleAgenda Projeter(DateOnly dateReference)
    {
        var lundiDeLaSemaine = LundiDeLaSemaineDe(dateReference);

        var slotsParJour = _slots.AllSnapshots()
            .ToLookup(snapshot => DateOnly.FromDateTime(snapshot.Debut));

        var periodes = _periodes.AllSnapshots();

        var jours = Enumerable.Range(0, 35)
            .Select(offset => lundiDeLaSemaine.AddDays(offset))
            .Select(date => new JourCase(date, CouleurResponsableAu(date, periodes), SlotsCasePour(slotsParJour[date])))
            .ToList();

        var semaines = jours
            .Chunk(7)
            .Select(septJours => new SemaineLigne(septJours.ToList()))
            .ToList();

        return new GrilleAgenda(jours, semaines);
    }

    private string CouleurResponsableAu(DateOnly date, IReadOnlyList<PeriodeSnapshot> periodes)
    {
        var periode = periodes.FirstOrDefault(p => CouvreLeJour(p, date));
        return periode is null ? _palette.CouleurNeutre : _palette.CouleurDe(periode.ResponsableId);
    }

    private static bool CouvreLeJour(PeriodeSnapshot periode, DateOnly date)
        => date >= DateOnly.FromDateTime(periode.Debut) && date <= DateOnly.FromDateTime(periode.Fin);

    private IReadOnlyList<SlotCase> SlotsCasePour(IEnumerable<SlotSnapshot> snapshots)
        => snapshots
            .Select(s => new SlotCase(
                s.LieuId,
                TimeOnly.FromDateTime(s.Debut),
                TimeOnly.FromDateTime(s.Fin),
                _palette.CouleurDe(s.LieuId)))
            .ToList();

    private static DateOnly LundiDeLaSemaineDe(DateOnly date)
    {
        var joursDepuisLundi = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-joursDepuisLundi);
    }
}
