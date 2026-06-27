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
    private readonly IReferentielResponsables _referentiel;

    public GrilleAgendaQuery(
        ISlotRepository slots,
        IPeriodeRepository periodes,
        IPaletteCouleurs palette,
        IReferentielResponsables referentiel)
    {
        _slots = slots;
        _periodes = periodes;
        _palette = palette;
        _referentiel = referentiel;
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
            .Select(date => CaseJourAu(date, periodes, slotsParJour[date]))
            .ToList();

        var semaines = jours
            .Chunk(7)
            .Select(septJours => new SemaineLigne(septJours.ToList()))
            .ToList();

        var legende = LegendeDesPresents(periodes, lundiDeLaSemaine, lundiDeLaSemaine.AddDays(34));

        return new GrilleAgenda(jours, semaines, legende);
    }

    private JourCase CaseJourAu(DateOnly date, IReadOnlyList<PeriodeSnapshot> periodes, IEnumerable<SlotSnapshot> slots)
    {
        var periode = periodes.FirstOrDefault(p => CouvreLeJour(p, date));
        var couleur = periode is null ? _palette.CouleurNeutre : _palette.CouleurDe(periode.ResponsableId);
        var nom = periode is null ? "" : _referentiel.NomDe(periode.ResponsableId);
        return new JourCase(date, couleur, nom, SlotsCasePour(slots));
    }

    private static bool CouvreLeJour(PeriodeSnapshot periode, DateOnly date)
        => date >= DateOnly.FromDateTime(periode.Debut) && date <= DateOnly.FromDateTime(periode.Fin);

    /// <summary>
    /// Légende = responsables présents dans la fenêtre (périodes intersectant l'intervalle affiché),
    /// dédoublonnés par identifiant stable (jamais le libellé — règle 17), avec nom et couleur résolus
    /// côte à côte. Vide si aucune période ne couvre la fenêtre.
    /// </summary>
    private IReadOnlyList<EntreeLegende> LegendeDesPresents(
        IReadOnlyList<PeriodeSnapshot> periodes, DateOnly premierJour, DateOnly dernierJour)
        => periodes
            .Where(p => DateOnly.FromDateTime(p.Debut) <= dernierJour && DateOnly.FromDateTime(p.Fin) >= premierJour)
            .Select(p => p.ResponsableId)
            .Distinct()
            .Select(id => new EntreeLegende(id, _referentiel.NomDe(id), _palette.CouleurDe(id)))
            .ToList();

    private IReadOnlyList<SlotCase> SlotsCasePour(IEnumerable<SlotSnapshot> snapshots)
        => snapshots
            .OrderBy(s => s.Debut.TimeOfDay)
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
