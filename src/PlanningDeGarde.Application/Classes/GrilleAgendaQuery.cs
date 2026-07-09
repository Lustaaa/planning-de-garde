using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Projection de lecture (CQRS) de la grille agenda du hub /planning. Construit la fenêtre
/// par défaut de 4 semaines glissantes (28 jours datés) à partir de la semaine de la date de
/// référence injectée — ou une fenêtre dimensionnée par la <c>VuePlanning</c> choisie — en
/// lisant les slots et périodes enregistrés. N'écrit jamais : aucune dépendance vers un
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
    private readonly ISlotRecurrentRepository? _slotsRecurrents;
    private readonly ITransfertRepository? _transferts;

    public GrilleAgendaQuery(
        ISlotRepository slots,
        IPeriodeRepository periodes,
        IPaletteCouleurs palette,
        IReferentielResponsables referentiel,
        IReferentielCycleDeFond? cycle = null,
        IEnumerationActeursFoyer? acteurs = null,
        ISlotRecurrentRepository? slotsRecurrents = null,
        ITransfertRepository? transferts = null)
    {
        _slots = slots;
        _periodes = periodes;
        _palette = palette;
        _referentiel = referentiel;
        _cycle = cycle;
        _acteurs = acteurs;
        _slotsRecurrents = slotsRecurrents;
        _transferts = transferts;
    }

    /// <summary>
    /// Projette la grille agenda à la <paramref name="dateReference"/> donnée (« aujourd'hui »,
    /// injecté pour le déterminisme — jamais <c>DateTime.Now</c>).
    /// </summary>
    /// <summary>
    /// Projette la grille à l'<paramref name="ancre"/> donnée selon la <paramref name="vue"/>
    /// choisie (span : Semaine 7 j / 4 semaines 28 j / Mois = semaines ISO du mois). Re-projection
    /// pure : chaque case se re-résout à sa propre date (surcharge &gt; fond &gt; neutre).
    /// </summary>
    public GrilleAgenda Projeter(DateOnly ancre, VuePlanning vue)
    {
        if (vue == VuePlanning.Mois)
        {
            var premierDuMois = new DateOnly(ancre.Year, ancre.Month, 1);
            var dernierDuMois = premierDuMois.AddMonths(1).AddDays(-1);
            var premierLundi = LundiDeLaSemaineDe(premierDuMois);
            var dernierDimanche = LundiDeLaSemaineDe(dernierDuMois).AddDays(6);
            return ProjeterFenetre(premierLundi, dernierDimanche.DayNumber - premierLundi.DayNumber + 1);
        }

        var nbJours = vue == VuePlanning.Semaine ? 7 : 28;
        return ProjeterFenetre(LundiDeLaSemaineDe(ancre), nbJours);
    }

    public GrilleAgenda Projeter(DateOnly dateReference)
        => Projeter(dateReference, VuePlanning.QuatreSemaines);

    private GrilleAgenda ProjeterFenetre(DateOnly premierJour, int nbJours)
    {
        // Un slot est rendu sur CHAQUE jour calendaire qu'il couvre : un slot franchissant minuit
        // (début un jour, fin le lendemain) apparaît donc dans la case de ses deux jours.
        var slotsParJour = _slots.AllSnapshots()
            .SelectMany(snapshot => JoursCouverts(snapshot).Select(jour => (jour, snapshot)))
            .ToLookup(x => x.jour, x => x.snapshot);

        var periodes = _periodes.AllSnapshots();

        // Un slot RÉCURRENT hebdo se matérialise sur CHAQUE jour de la fenêtre dont le jour de semaine
        // correspond : chaque occurrence est un slot « virtuel » daté (date + plage horaire) qui rejoint
        // le flux des slots ponctuels de sa case (empilement en ordre horaire assuré par SlotsCasePour).
        var recurrents = _slotsRecurrents?.AllSnapshots() ?? (IReadOnlyList<SlotRecurrentSnapshot>)Array.Empty<SlotRecurrentSnapshot>();

        // Transferts saisis : rendus en présentation bicolore sur la case de leur jour (aucun changement
        // du modèle de transfert ni de la résolution de responsabilité — décision SM s29 volet 2).
        var transferts = _transferts?.AllSnapshots() ?? (IReadOnlyList<TransfertSnapshot>)Array.Empty<TransfertSnapshot>();

        var jours = Enumerable.Range(0, nbJours)
            .Select(offset => premierJour.AddDays(offset))
            .Select(date => CaseJourAu(date, periodes, slotsParJour[date].Concat(OccurrencesRecurrentes(recurrents, date)), transferts))
            .ToList();

        var semaines = jours
            .Chunk(7)
            .Select(septJours => new SemaineLigne(septJours.ToList()))
            .ToList();

        var legende = LegendeDesPresents(periodes, premierJour, premierJour.AddDays(nbJours - 1));
        var legendeMotifs = LegendeDesMotifs(transferts, premierJour, premierJour.AddDays(nbJours - 1));

        return new GrilleAgenda(jours, semaines, legende, legendeMotifs);
    }

    private JourCase CaseJourAu(
        DateOnly date, IReadOnlyList<PeriodeSnapshot> periodes, IEnumerable<SlotSnapshot> slots, IReadOnlyList<TransfertSnapshot> transferts)
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
        return new JourCase(date, couleur, nom, SlotsCasePour(slots), InfoTransfertDuJour(transferts, periodes, date));
    }

    /// <summary>
    /// Information bicolore de la case si un transfert est saisi ce jour-là, sinon <c>null</c> (case
    /// unicolore inchangée). Couleur de départ = déposant, couleur d'arrivée = récupérant, résolues sur
    /// le référentiel acteurs par identifiant stable ; un acteur supprimé (orphelin) retombe sur le
    /// neutre (même contrat d'existence <see cref="Resolvable"/> que la responsabilité — pas de fantôme).
    /// </summary>
    private InfoTransfert? InfoTransfertDuJour(
        IReadOnlyList<TransfertSnapshot> transferts, IReadOnlyList<PeriodeSnapshot> periodes, DateOnly date)
    {
        // Priorité SAISI > DÉRIVÉ : un transfert saisi ce jour-là prime et est seul retenu (Sc.6).
        var transfert = transferts.FirstOrDefault(t => DateOnly.FromDateTime(t.Date) == date);
        if (transfert is not null)
            return new InfoTransfert(CouleurActeurResolue(transfert.DeposeParId), CouleurActeurResolue(transfert.RecupereParId));

        // À défaut de saisie, on DÉRIVE la bascule depuis la succession de périodes (D3, Sc.5).
        return TransfertDeriveDuJour(periodes, date);
    }

    /// <summary>
    /// Transfert AUTO-dérivé (D3, Sc.5) de la succession de périodes : si une période <b>débute</b> le jour
    /// <paramref name="date"/> ET qu'une période <b>se termine la veille</b>, la responsabilité bascule du
    /// Cédant (déposant, période finissante) vers le Recevant (récupérant, période débutante) — c'est le
    /// jour de bascule. Dérivation de LECTURE pure : aucune écriture, aucun transfert persisté. Le jour de
    /// bascule est le premier jour du successeur ; s'il tombe hors de la fenêtre projetée, il n'est
    /// simplement pas rendu (pas de dérivation fantôme, Sc.8). Retombée <c>null</c> (neutre) sans successeur
    /// (fin de garde, Sc.7). Les couleurs orphelines (acteur supprimé) retombent sur le neutre (Sc.9).
    /// </summary>
    private InfoTransfert? TransfertDeriveDuJour(IReadOnlyList<PeriodeSnapshot> periodes, DateOnly date)
    {
        var debutant = periodes.FirstOrDefault(p => DateOnly.FromDateTime(p.Debut) == date);
        if (debutant is null)
            return null; // aucun successeur ne débute ce jour-là → aucune bascule (retombée neutre)

        var finissant = periodes.FirstOrDefault(p => DateOnly.FromDateTime(p.Fin) == date.AddDays(-1));
        if (finissant is null)
            return null; // aucune période ne se termine la veille → pas de bascule dérivée

        return new InfoTransfert(
            CouleurActeurResolue(finissant.ResponsableId), CouleurActeurResolue(debutant.ResponsableId));
    }

    /// <summary>Couleur d'un acteur existant, ou la couleur neutre s'il est orphelin (supprimé).</summary>
    private string CouleurActeurResolue(string acteurId)
        => Resolvable(acteurId) is null ? _palette.CouleurNeutre : _palette.CouleurDe(acteurId);

    /// <summary>
    /// Occurrences d'un slot récurrent tombant sur la <paramref name="date"/> donnée : un slot virtuel
    /// daté (même enfant / lieu, bornes = date + plage horaire) par récurrent dont le jour de semaine
    /// correspond. Ne persiste rien : matérialisation de lecture pure, réévaluée à chaque projection.
    /// </summary>
    private static IEnumerable<SlotSnapshot> OccurrencesRecurrentes(IReadOnlyList<SlotRecurrentSnapshot> recurrents, DateOnly date)
        => recurrents
            .Where(r => r.JourDeSemaine == date.DayOfWeek)
            .Select(r => new SlotSnapshot(
                r.EnfantId, r.LieuId, date.ToDateTime(TimeOnly.FromTimeSpan(r.HeureDebut)), date.ToDateTime(TimeOnly.FromTimeSpan(r.HeureFin))));

    /// <summary>Jours calendaires couverts par un slot, du jour de son début à celui de sa fin (inclus).</summary>
    private static IEnumerable<DateOnly> JoursCouverts(SlotSnapshot slot)
    {
        var premier = DateOnly.FromDateTime(slot.Debut);
        var dernier = DateOnly.FromDateTime(slot.Fin);
        for (var jour = premier; jour <= dernier; jour = jour.AddDays(1))
            yield return jour;
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

    /// <summary>
    /// Légende des motifs de rendu : une entrée « Transfert » (motif bicolore) si au moins un transfert
    /// est saisi dans la fenêtre [<paramref name="premierJour"/>, <paramref name="dernierJour"/>], sinon
    /// vide (« en case comme en légende » : signalé seulement quand le motif est effectivement présent).
    /// </summary>
    private static IReadOnlyList<EntreeLegendeMotif> LegendeDesMotifs(
        IReadOnlyList<TransfertSnapshot> transferts, DateOnly premierJour, DateOnly dernierJour)
    {
        var transfertDansLaFenetre = transferts
            .Any(t => DateOnly.FromDateTime(t.Date) >= premierJour && DateOnly.FromDateTime(t.Date) <= dernierJour);

        return transfertDansLaFenetre
            ? new[] { new EntreeLegendeMotif("Transfert") }
            : Array.Empty<EntreeLegendeMotif>();
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
