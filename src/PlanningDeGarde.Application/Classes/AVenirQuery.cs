using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application;

/// <summary>
/// Liste de lecture PURE « À venir » (s43) pour l'enfant sélectionné : les JOURS À VENIR de la fenêtre de
/// grille déjà chargée (strictement après aujourd'hui), chacun portant le <b>responsable RÉSOLU</b>, le(s)
/// <b>slot(s)</b> du jour (le « où » de l'enfant) et le <b>transfert éventuel</b>. Miroir strict de
/// <see cref="CarteDuJourQuery"/> (s42), itéré sur les jours à venir : elle COMPOSE la résolution EXISTANTE
/// (surcharge &gt; fond &gt; neutre du palier 6, transferts saisis/dérivés s31, slots s29), portée par
/// <see cref="GrilleAgendaQuery"/>, sans la réimplémenter — aucune mutation, aucun store neuf, aucune
/// persistance neuve. Résultat IDENTIQUE sur les deux adaptateurs (InMemory / Mongo), la grille composée
/// étant elle-même adaptateur-agnostique.
/// </summary>
public sealed class AVenirQuery
{
    private readonly GrilleAgendaQuery _grille;

    public AVenirQuery(GrilleAgendaQuery grille) => _grille = grille;

    /// <summary>
    /// Les jours à venir (strictement après <paramref name="aujourdhui"/>) de la fenêtre projetée pour la
    /// <paramref name="vue"/> choisie (cohérente avec la grille déjà chargée), pour l'<paramref name="enfantId"/>
    /// sélectionné. Liste ORDONNÉE par date croissante ; chaque entrée compose la case résolue du jour
    /// (responsable + slots de l'enfant + transfert). Fenêtre sans à-venir ⇒ liste vide (aucune racine fantôme).
    /// </summary>
    public IReadOnlyList<JourAVenir> Lire(DateOnly aujourdhui, string enfantId, VuePlanning vue = VuePlanning.Semaine)
        => _grille.Projeter(aujourdhui, vue).Jours
            .Where(jour => jour.Date > aujourdhui)
            .OrderBy(jour => jour.Date)
            .Select(jour => new JourAVenir(
                jour.Date,
                new ResponsableDuJour(jour.ResponsableId, jour.NomResponsable, jour.CouleurResponsable, jour.ResponsableId is not null),
                jour.Slots.Where(slot => slot.EnfantId == enfantId).ToList(),
                jour.Transfert is { } t
                    ? new TransfertDuJour(t.NomDepart, t.CouleurDepart, t.NomArrivee, t.CouleurArrivee)
                    : null))
            .ToList();
}

/// <summary>
/// Un jour de la liste « À venir » (s43) : sa <see cref="Date"/>, le <see cref="Responsable"/> résolu, le(s)
/// <see cref="Slots"/> du jour (le « où » de l'enfant sélectionné) et le <see cref="Transfert"/> éventuel
/// (null = jour unicolore). Lecture seule, composée de la grille — mêmes types que la carte du jour (s42).
/// </summary>
public sealed record JourAVenir(
    DateOnly Date, ResponsableDuJour Responsable, IReadOnlyList<SlotCase> Slots, TransfertDuJour? Transfert);
