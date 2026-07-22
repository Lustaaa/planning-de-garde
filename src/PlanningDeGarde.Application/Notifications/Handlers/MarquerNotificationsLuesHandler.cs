using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Notifications.Handlers;

/// <summary>
/// Commande « marquer lu » de la cloche : <paramref name="EvenementId"/> renseigné = marquer UNE
/// notification lue ; <paramref name="EvenementId"/> null = marquer TOUTES les notifications de l'utilisateur
/// comme lues.
/// </summary>
public sealed record MarquerNotificationsLuesCommand(string UtilisateurId, string? EvenementId = null);

/// <summary>
/// Use case : marque une notification (ou toutes) comme lue(s) PAR utilisateur, via le port d'état de lecture
/// (idempotent). « Toutes » itère le flux de l'utilisateur (les événements le concernant). N'écrit rien au
/// planning : seul l'état LU / non-lu par utilisateur est muté.
/// </summary>
public sealed class MarquerNotificationsLuesHandler
{
    private readonly FluxNotificationsQuery _flux;
    private readonly IEtatLectureNotifications _etat;

    public MarquerNotificationsLuesHandler(FluxNotificationsQuery flux, IEtatLectureNotifications etat)
    {
        _flux = flux;
        _etat = etat;
    }

    public Result<int> Handle(MarquerNotificationsLuesCommand commande)
    {
        if (commande.EvenementId is not null)
        {
            _etat.MarquerLu(commande.UtilisateurId, commande.EvenementId);
        }
        else
        {
            foreach (var evenement in _flux.Flux(commande.UtilisateurId))
                _etat.MarquerLu(commande.UtilisateurId, evenement.Id);
        }

        return Result<int>.Succes(_flux.NombreNonLus(commande.UtilisateurId));
    }
}
