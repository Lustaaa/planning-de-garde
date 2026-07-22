using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Notifications.Queries;

/// <summary>Une notification de la cloche : l'événement du journal + son état LU / non-lu pour l'utilisateur courant.</summary>
public sealed record NotificationVue(EvenementChangementSnapshot Evenement, bool Lu);

/// <summary>
/// Projection de lecture (CQRS) de la cloche (s47) : le FLUX de notifications d'un utilisateur, dérivé du
/// JOURNAL DE CHANGEMENTS (trace de lecture) et enrichi de l'état LU / non-lu PAR utilisateur. Retient les
/// événements où l'utilisateur figure comme cédant OU recevant (« le concernant »), triés par RÉCENCE de
/// l'écriture (le plus récent en tête). N'écrit jamais : lecture pure du journal + de l'état de lecture.
/// </summary>
public sealed class FluxNotificationsQuery
{
    private readonly IJournalChangements _journal;
    private readonly IEtatLectureNotifications? _etat;

    public FluxNotificationsQuery(IJournalChangements journal, IEtatLectureNotifications? etat = null)
    {
        _journal = journal;
        _etat = etat;
    }

    /// <summary>Flux des événements concernant <paramref name="utilisateurId"/>, du plus récent au plus ancien.</summary>
    public IReadOnlyList<EvenementChangementSnapshot> Flux(string utilisateurId)
        => EvenementsConcernant(utilisateurId).ToList();

    /// <summary>Flux enrichi de l'état LU / non-lu par utilisateur (chrono récence). Sans état → tout non-lu.</summary>
    public IReadOnlyList<NotificationVue> FluxAvecEtat(string utilisateurId)
    {
        var lus = _etat?.EvenementsLus(utilisateurId) ?? new List<string>();
        return EvenementsConcernant(utilisateurId)
            .Select(e => new NotificationVue(e, lus.Contains(e.Id)))
            .ToList();
    }

    /// <summary>Nombre de notifications NON encore marquées lues par <paramref name="utilisateurId"/> (badge compteur).</summary>
    public int NombreNonLus(string utilisateurId)
    {
        var lus = _etat?.EvenementsLus(utilisateurId) ?? new List<string>();
        return EvenementsConcernant(utilisateurId).Count(e => !lus.Contains(e.Id));
    }

    private IEnumerable<EvenementChangementSnapshot> EvenementsConcernant(string utilisateurId)
        => _journal.Tout()
            .Where(e => e.CedantId == utilisateurId || e.RecevantId == utilisateurId)
            .OrderByDescending(e => e.Horodatage);
}
