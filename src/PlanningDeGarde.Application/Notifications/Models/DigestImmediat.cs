using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningDeGarde.Application.Notifications.Models;

/// <summary>
/// Le responsable RÉSOLU d'un jour restitué par le digest cloche : identifiant stable de l'acteur
/// (ou <c>null</c> si personne n'est assigné), son <b>nom</b> et sa <b>couleur</b> tels que la grille les
/// résout, et le flag <see cref="EstAssigne"/> (faux = état neutre « personne assignée », sans nom ni
/// couleur fantôme, repli). Présentation seule. Miroir des ex-read-models retirés.
/// </summary>
public sealed record ResponsableDuJour(string? ActeurId, string Nom, string Couleur, bool EstAssigne);

/// <summary>
/// Le transfert cédant → recevant d'un jour du digest : nom + couleur du <b>cédant</b> (déposant) et
/// du <b>recevant</b> (récupérant), résolus par la grille composée (saisi OU dérivé, un acteur orphelin
/// retombe sur le repli neutre sans nom fantôme). Absent (<c>null</c>) = jour sans transfert.
/// </summary>
public sealed record TransfertDuJour(string CedantNom, string CedantCouleur, string RecevantNom, string RecevantCouleur);

/// <summary>
/// Un jour du digest cloche : sa <see cref="Date"/>, le <see cref="Responsable"/> résolu, le(s)
/// <see cref="Slots"/> du jour (le « où » de l'enfant sélectionné) et le <see cref="Transfert"/> éventuel
/// (null = jour sans transfert). Lecture seule, composée de la grille.
/// </summary>
public sealed record JourDigest(
    DateOnly Date, ResponsableDuJour Responsable, IReadOnlyList<SlotCase> Slots, TransfertDuJour? Transfert);

/// <summary>
/// Payload du digest « immédiat » de la cloche : la section <see cref="Immediat"/> « qui récupère
/// aujourd'hui / ce soir » (null = jour courant hors de la fenêtre de grille chargée) et la liste
/// <see cref="AVenir"/> des jours à venir de la fenêtre chargée, en ordre chronologique croissant. Lecture
/// stricte, composée de <see cref="GrilleAgendaQuery"/> — aucune mutation, aucun store neuf.
/// </summary>
public sealed record DigestImmediat(JourDigest? Immediat, IReadOnlyList<JourDigest> AVenir)
{
    /// <summary>Digest vide neutre : jour courant hors-fenêtre (immédiat null) et aucun transfert à venir.</summary>
    public static readonly DigestImmediat Vide = new(null, Array.Empty<JourDigest>());

    /// <summary>
    /// Compose le digest PUREMENT à partir d'une <see cref="GrilleAgenda"/> déjà projetée (la fenêtre de grille
    /// chargée), pour l'<paramref name="enfantId"/> sélectionné et la date <paramref name="aujourdhui"/>.
    /// Fonction PARTAGÉE entre le serveur (query qui projette d'abord) et la REPROJECTION CLIENT (@ihm : le
    /// front la ré-applique sur la grille déjà chargée — aucun GET dédié). La grille porte déjà les valeurs
    /// résolues (id stable, nom, couleur, slots, transfert), donc la composition ne relit ni référentiel ni
    /// palette : elle SÉLECTIONNE. Section « immédiat » null si le jour courant n'est pas dans la fenêtre chargée
    /// (navigation hors-semaine — vide neutre) ; section « à venir » = jours &gt; aujourd'hui PORTANT un transfert,
    /// chrono croissant.
    /// </summary>
    public static DigestImmediat Composer(GrilleAgenda grille, DateOnly aujourdhui, string enfantId)
    {
        var immediat = grille.Jours.FirstOrDefault(j => j.Date == aujourdhui);
        var avenir = grille.Jours
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
