using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

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
    private readonly IEnumerationComptes _comptes;
    private readonly IEditeurComptes _editeurComptes;

    public SupprimerActeurHandler(
        IEditeurConfigurationFoyer configuration,
        INotificateurPlanning notificateur,
        IEnumerationComptes comptes,
        IEditeurComptes editeurComptes)
    {
        _configuration = configuration;
        _notificateur = notificateur;
        _comptes = comptes;
        _editeurComptes = editeurComptes;
    }

    public Result<SupprimerActeurResultat> Handle(SupprimerActeurCommand commande)
    {
        // Repli propre ciblé : chaque compte référençant l'acteur supprimé retombe DÉSASSOCIÉ (le
        // compte survit, énuméré, sans acteur) — pas de compte fantôme pointant un acteur absent.
        // Miroir du repli acteur orphelin (s13/s19). Aucun porteur → aucune écriture (idempotence Sc.6).
        foreach (var compte in _comptes.EnumererComptes())
            if (compte.ActeurId == commande.ActeurId)
                _editeurComptes.Desassocier(compte.Id);

        _configuration.Supprimer(commande.ActeurId);
        _notificateur.NotifierMiseAJour(); // diffusion temps réel sur suppression aboutie
        return Result<SupprimerActeurResultat>.Succes(new SupprimerActeurResultat(commande.ActeurId));
    }
}
