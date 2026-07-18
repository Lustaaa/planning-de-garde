using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application;

/// <summary>
/// Query PURE de composition (CQRS) du digest « immédiat » de la cloche (s50) : (a) « qui récupère
/// aujourd'hui / ce soir » + (b) les « transferts à venir » des N prochains jours de la fenêtre de grille
/// chargée. Miroir des ex-read-models <c>CarteDuJourQuery</c> (s42) et <c>AVenirQuery</c> (s43) retirés s44 :
/// elle COMPOSE la résolution EXISTANTE (surcharge &gt; fond &gt; neutre, transferts saisis/dérivés s31,
/// slots s29) portée par <see cref="GrilleAgendaQuery"/>, sans la réimplémenter — aucune mutation, aucun
/// store neuf, aucune persistance neuve. Résultat IDENTIQUE sur les deux adaptateurs (InMemory / Mongo), la
/// grille composée étant elle-même adaptateur-agnostique. Lecture stricte : aucune action.
/// </summary>
public sealed class DigestImmediatQuery
{
    private readonly GrilleAgendaQuery _grille;

    public DigestImmediatQuery(GrilleAgendaQuery grille) => _grille = grille;

    /// <summary>
    /// Compose le digest pour l'<paramref name="enfantId"/> sélectionné, à partir de la fenêtre de grille
    /// projetée à l'<paramref name="ancre"/> chargée (cohérente avec la grille côté client) et de la date
    /// <paramref name="aujourdhui"/> injectée (déterminisme — jamais <c>DateTime.Now</c>). La section
    /// « immédiat » compose la case du jour courant si elle appartient à la fenêtre chargée (sinon null =
    /// jour courant hors-fenêtre, section vide neutre côté IHM).
    /// </summary>
    public DigestImmediat Composer(
        DateOnly ancre, DateOnly aujourdhui, string enfantId, VuePlanning vue = VuePlanning.QuatreSemaines)
    {
        var jours = _grille.Projeter(ancre, vue).Jours;
        var immediat = jours.FirstOrDefault(j => j.Date == aujourdhui);

        // « À venir » = les jours STRICTEMENT APRÈS aujourd'hui de la fenêtre chargée qui PORTENT un transfert
        // (saisi OU auto-dérivé s31 — la grille arbitre déjà la source), en ordre chronologique CROISSANT. Un
        // jour à venir SANS transfert n'y figure pas (section « transferts à venir », Sc.2/Sc.3). Ne compose
        // que la fenêtre déjà projetée : aucun GET dédié, aucune mutation.
        var avenir = jours
            .Where(jour => jour.Date > aujourdhui && jour.Transfert is not null)
            .OrderBy(jour => jour.Date)
            .Select(jour => ComposerJour(jour, enfantId))
            .ToList();

        return new DigestImmediat(
            immediat is null ? null : ComposerJour(immediat, enfantId),
            avenir);
    }

    private static JourDigest ComposerJour(JourCase jour, string enfantId)
        => new(
            jour.Date,
            new ResponsableDuJour(jour.ResponsableId, jour.NomResponsable, jour.CouleurResponsable, jour.ResponsableId is not null),
            jour.Slots.Where(slot => slot.EnfantId == enfantId).ToList(),
            jour.Transfert is { } t
                ? new TransfertDuJour(t.NomDepart, t.CouleurDepart, t.NomArrivee, t.CouleurArrivee)
                : null);
}
