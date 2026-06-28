using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de suppression d'un acteur du foyer (config). L'identifiant stable opaque
/// (<c>acteur-…</c>) est la clé — jamais le libellé. La suppression est <b>autorisée sans
/// condition de références</b> (pas de refus « si l'acteur est utilisé ») et <b>idempotente</b>
/// (id absent / déjà supprimé = no-op qui réussit, exercé au Sc.5).
/// </summary>
public sealed record SupprimerActeurCommand(string ActeurId);

/// <summary>Confirmation d'une suppression aboutie : l'identifiant stable de l'acteur retiré.</summary>
public sealed record SupprimerActeurResultat(string ActeurId);

/// <summary>
/// Use case : supprimer un acteur du foyer. Retire l'acteur (nom ET couleur) de la configuration
/// via le port d'écriture, puis déclenche la diffusion temps réel sur succès — jamais d'écriture
/// par le canal de diffusion (CQRS).
/// </summary>
public sealed class SupprimerActeurHandler
{
    private readonly IEditeurConfigurationFoyer _configuration;
    private readonly INotificateurPlanning _notificateur;

    public SupprimerActeurHandler(IEditeurConfigurationFoyer configuration, INotificateurPlanning notificateur)
    {
        _configuration = configuration;
        _notificateur = notificateur;
    }

    public Result<SupprimerActeurResultat> Handle(SupprimerActeurCommand commande)
    {
        _configuration.Supprimer(commande.ActeurId);
        _notificateur.NotifierMiseAJour(); // diffusion temps réel sur suppression aboutie
        return Result<SupprimerActeurResultat>.Succes(new SupprimerActeurResultat(commande.ActeurId));
    }
}
