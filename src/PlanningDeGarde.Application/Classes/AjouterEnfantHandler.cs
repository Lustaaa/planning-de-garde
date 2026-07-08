using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'ajout d'un enfant au référentiel du foyer (config). Le handler génère un identifiant
/// stable neuf opaque (jamais dérivé du prénom, anti-pattern s06) et persiste le prénom via le port
/// d'écriture <see cref="IEditeurEnfants"/>, pour que l'enfant devienne disponible à la saisie
/// (validation de pose + sélecteur d'enfant).
/// </summary>
public sealed record AjouterEnfantCommand(string Prenom);

/// <summary>Confirmation d'un ajout abouti : l'identifiant stable neuf généré pour l'enfant.</summary>
public sealed record AjouterEnfantResultat(string EnfantId);

/// <summary>
/// Use case : ajouter un enfant au référentiel du foyer. Génère un identifiant stable neuf opaque,
/// persiste l'enfant via le port d'écriture du référentiel, puis diffuse la mise à jour temps réel.
/// </summary>
public sealed class AjouterEnfantHandler
{
    private readonly IEnumerationEnfants _enfants;
    private readonly IEditeurEnfants _referentiel;
    private readonly INotificateurPlanning _notificateur;

    public AjouterEnfantHandler(IEnumerationEnfants enfants, IEditeurEnfants referentiel, INotificateurPlanning notificateur)
    {
        _enfants = enfants;
        _referentiel = referentiel;
        _notificateur = notificateur;
    }

    public Result<AjouterEnfantResultat> Handle(AjouterEnfantCommand commande)
    {
        // Garde « prénom requis » (S2, miroir R5/R10) : un prénom vide ou tout-espaces est refusé AVANT
        // toute génération d'id, toute écriture et toute diffusion — aucun enfant vide persisté.
        if (string.IsNullOrWhiteSpace(commande.Prenom))
            return Result<AjouterEnfantResultat>.Echec("prénom requis");

        // Identifiant stable neuf OPAQUE, généré (jamais dérivé du prénom, anti-pattern s06) et unique
        // (GUID → jamais un id existant). Le prénom se résout ensuite sur cet id.
        var enfantId = $"enfant-{Guid.NewGuid():N}";
        _referentiel.Ajouter(enfantId, commande.Prenom);

        // Écriture du foyer aboutie : on diffuse la mise à jour temps réel (lecture seule) — les
        // sélecteurs d'enfant des dialogs suivent sans rechargement (miroir de la pose).
        _notificateur.NotifierMiseAJour();
        return Result<AjouterEnfantResultat>.Succes(new AjouterEnfantResultat(enfantId));
    }
}
