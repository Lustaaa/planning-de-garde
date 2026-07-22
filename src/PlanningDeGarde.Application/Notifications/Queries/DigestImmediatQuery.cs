using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application.Notifications.Queries;

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
        // Projette la fenêtre chargée POUR CET ENFANT (s53 : résolution isolée — le digest LIT le planning d'un
        // enfant) puis DÉLÈGUE à la composition PURE partagée avec la reprojection client (s50 @ihm) : « immédiat »
        // = case du jour courant si présente dans la fenêtre (sinon null = hors-fenêtre), « à venir » = jours >
        // aujourd'hui portant un transfert, chrono croissant. Aucune mutation, 0 store neuf.
        return DigestImmediat.Composer(_grille.Projeter(ancre, vue, enfantId), aujourdhui, enfantId);
    }
}
