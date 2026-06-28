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
    private readonly IReferentielCycleDeFond? _cycle;
    private readonly IEnumerationActeursFoyer? _acteurs;

    public GrilleAgendaQuery(
        ISlotRepository slots,
        IPeriodeRepository periodes,
        IPaletteCouleurs palette,
        IReferentielResponsables referentiel,
        IReferentielCycleDeFond? cycle = null,
        IEnumerationActeursFoyer? acteurs = null)
    {
        _slots = slots;
        _periodes = periodes;
        _palette = palette;
        _referentiel = referentiel;
        _cycle = cycle;
        _acteurs = acteurs;
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
        // Filtre d'existence appliqué INDÉPENDAMMENT à chaque source AVANT le repli, jamais au
        // responsableId combiné (un faux raccourci ferait retomber une surcharge orpheline sur le neutre
        // au lieu du fond) : une surcharge orpheline (Sc.2) retombe sur le fond ; un fond orphelin (Sc.4)
        // est traité comme un index non mappé → null → neutre, sans nom fantôme.
        var surcharge = Resolvable(periode?.ResponsableId);
        var fond = Resolvable(_cycle?.CycleCourant()?.ResponsableDeFond(date));
        // Priorité de résolution : surcharge (période saisie) > fond (cycle) > neutre. La surcharge
        // prime structurellement ; le fond ne s'applique que sans surcharge résolvable.
        var responsableId = surcharge ?? fond;
        var couleur = responsableId is null ? _palette.CouleurNeutre : _palette.CouleurDe(responsableId);
        var nom = responsableId is null ? "" : _referentiel.NomDe(responsableId);
        return new JourCase(date, couleur, nom, SlotsCasePour(slots));
    }

    private static bool CouvreLeJour(PeriodeSnapshot periode, DateOnly date)
        => date >= DateOnly.FromDateTime(periode.Debut) && date <= DateOnly.FromDateTime(periode.Fin);

    /// <summary>
    /// Contrat d'existence : restitue l'identifiant s'il désigne un acteur <b>existant</b> du foyer,
    /// sinon <c>null</c> (acteur supprimé = orphelin → neutralisé à la résolution). Contrat porté par
    /// le port de lecture EXISTANT <see cref="IEnumerationActeursFoyer"/> (décision CP) ; absent
    /// (<c>_acteurs is null</c>) → pas de filtrage (comportement antérieur préservé).
    /// </summary>
    private string? Resolvable(string? acteurId)
        => acteurId is not null && _acteurs is not null && !_acteurs.EnumererActeurs().Contains(acteurId)
            ? null
            : acteurId;

    /// <summary>
    /// Légende = responsables présents dans la fenêtre, dédoublonnés par identifiant stable (jamais
    /// le libellé — règle 17), avec nom et couleur résolus côte à côte. Présents = responsables des
    /// périodes intersectant l'intervalle affiché ET responsables de fond couvrant un jour de la
    /// fenêtre (« en case comme en légende »). Vide si aucun ne couvre la fenêtre.
    /// </summary>
    private IReadOnlyList<EntreeLegende> LegendeDesPresents(
        IReadOnlyList<PeriodeSnapshot> periodes, DateOnly premierJour, DateOnly dernierJour)
    {
        var idsPeriodes = periodes
            .Where(p => DateOnly.FromDateTime(p.Debut) <= dernierJour && DateOnly.FromDateTime(p.Fin) >= premierJour)
            .Select(p => p.ResponsableId);

        var cycle = _cycle?.CycleCourant();
        var idsFond = cycle is null
            ? Enumerable.Empty<string>()
            : Enumerable.Range(0, 35)
                .Select(offset => cycle.ResponsableDeFond(premierJour.AddDays(offset)))
                .Where(id => id is not null)
                .Select(id => id!);

        // « En case comme en légende » : un acteur supprimé (orphelin, en surcharge OU en fond) est
        // neutralisé en case (Resolvable) ET ne laisse aucune entrée fantôme en légende — même contrat
        // d'existence appliqué au flux des présents.
        return idsPeriodes.Concat(idsFond)
            .Where(id => Resolvable(id) is not null)
            .Distinct()
            .Select(id => new EntreeLegende(id, _referentiel.NomDe(id), _palette.CouleurDe(id)))
            .ToList();
    }

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
