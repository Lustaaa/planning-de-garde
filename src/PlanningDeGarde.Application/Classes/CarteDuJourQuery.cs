using System;
using System.Linq;

namespace PlanningDeGarde.Application;

/// <summary>
/// Le responsable RÉSOLU d'un jour restitué par la carte « qui récupère ce soir » (s42) : identifiant
/// stable de l'acteur (ou <c>null</c> si personne n'est assigné), son <b>nom</b> et sa <b>couleur</b>
/// tels que la grille les résout, et le flag <see cref="EstAssigne"/> (faux = état neutre « personne
/// assignée », sans nom ni couleur fantôme, repli s13 R5/R6). Présentation seule.
/// </summary>
public sealed record ResponsableDuJour(string? ActeurId, string Nom, string Couleur, bool EstAssigne);

/// <summary>
/// Carte de lecture PURE « qui récupère ce soir » (s42) pour une DATE + l'enfant sélectionné : restitue le
/// <see cref="Responsable"/> résolu du jour. Miroir de <see cref="GrapheFoyerQuery"/> (s38) : elle COMPOSE
/// la résolution EXISTANTE (surcharge &gt; fond &gt; neutre du palier 6, portée par
/// <see cref="GrilleAgendaQuery"/>) sans la réimplémenter — aucune mutation, aucun store neuf, aucune
/// persistance neuve. Le résultat est IDENTIQUE sur les deux adaptateurs (InMemory / Mongo), la grille
/// composée étant elle-même adaptateur-agnostique.
/// </summary>
public sealed class CarteDuJourQuery
{
    private readonly GrilleAgendaQuery _grille;

    public CarteDuJourQuery(GrilleAgendaQuery grille) => _grille = grille;

    /// <summary>
    /// « Qui récupère ce jour-là » pour la <paramref name="date"/> + l'<paramref name="enfantId"/> sélectionné.
    /// Projette la SEMAINE de la date (la date y est toujours présente : aucun bord de fenêtre / jour non
    /// chargé) et compose la case résolue de ce jour. Un jour sans responsable résolu retombe sur l'état
    /// neutre (personne assignée), sans nom ni couleur fantôme.
    /// </summary>
    public CarteDuJour Lire(DateOnly date, string enfantId)
    {
        var jour = _grille.Projeter(date, VuePlanning.Semaine).Jours.Single(j => j.Date == date);
        var responsable = new ResponsableDuJour(
            jour.ResponsableId, jour.NomResponsable, jour.CouleurResponsable, jour.ResponsableId is not null);
        return new CarteDuJour(responsable);
    }
}

/// <summary>
/// Payload de la carte « Aujourd'hui : qui récupère ce soir » (s42), pour une date + l'enfant sélectionné.
/// Lecture seule, composée de la résolution existante.
/// </summary>
public sealed record CarteDuJour(ResponsableDuJour Responsable);
