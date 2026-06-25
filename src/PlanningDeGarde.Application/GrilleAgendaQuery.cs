using System;
using System.Collections.Generic;
using System.Linq;

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

    public GrilleAgendaQuery(ISlotRepository slots, IPeriodeRepository periodes)
    {
        _slots = slots;
        _periodes = periodes;
    }

    /// <summary>
    /// Projette la grille agenda à la <paramref name="dateReference"/> donnée (« aujourd'hui »,
    /// injecté pour le déterminisme — jamais <c>DateTime.Now</c>).
    /// </summary>
    public GrilleAgenda Projeter(DateOnly dateReference)
    {
        var lundiDeLaSemaine = LundiDeLaSemaineDe(dateReference);

        var jours = Enumerable.Range(0, 35)
            .Select(offset => new JourCase(lundiDeLaSemaine.AddDays(offset)))
            .ToList();

        var semaines = jours
            .Chunk(7)
            .Select(septJours => new SemaineLigne(septJours.ToList()))
            .ToList();

        return new GrilleAgenda(jours, semaines);
    }

    private static DateOnly LundiDeLaSemaineDe(DateOnly date)
    {
        var joursDepuisLundi = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-joursDepuisLundi);
    }
}
