using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Foyer.Handlers;

/// <summary>
/// Commande d'ajout d'un acteur au foyer (config). À la différence d'<c>EditerActeur</c> (qui
/// mute un acteur déjà semé sur son id stable), l'ajout <b>crée</b> un acteur neuf : le handler
/// génère un identifiant stable neuf opaque (jamais dérivé du libellé) et persiste le nom
/// (+ couleur fournie, sinon repli neutre par contrat <see cref="IPaletteCouleurs"/>) via le port
/// d'écriture. La couleur est optionnelle ; le nom non vide sera exigé au.
/// </summary>
public sealed record AjouterActeurCommand(string Nom, string? Couleur = null);

/// <summary>Confirmation d'un ajout abouti : l'identifiant stable neuf généré pour l'acteur ajouté.</summary>
public sealed record AjouterActeurResultat(string ActeurId);

/// <summary>
/// Use case : ajouter un acteur au foyer. Génère un identifiant stable neuf opaque, puis persiste
/// l'acteur via le port d'écriture de la configuration.
/// </summary>
public sealed class AjouterActeurHandler
{
    private readonly IEditeurConfigurationFoyer _configuration;

    public AjouterActeurHandler(IEditeurConfigurationFoyer configuration)
    {
        _configuration = configuration;
    }

    public Result<AjouterActeurResultat> Handle(AjouterActeurCommand commande)
    {
        // Garde « nom non vide » CONDITIONNELLE (Sc.8) : un nom fourni vide OU tout-espaces (nom non
        // utile) est refusé AVANT toute génération d'id et toute mutation du store (aucun acteur
        // fantôme, liste inchangée). Garde sur le nom UTILE (espaces ignorés, à la EditerActeurHandler).
        // Conditionnelle : le nominal d'ajout (Sc.1) reste vert — seul le nom vide / tout-espaces est visé.
        if (string.IsNullOrWhiteSpace(commande.Nom))
            return Result<AjouterActeurResultat>.Echec("le nom ne peut pas être vide");

        // Identifiant stable neuf OPAQUE, généré (jamais dérivé du libellé, anti-pattern s06) et
        // unique (GUID → jamais un id existant). La résolution nom/couleur se fait sur cet id.
        var acteurId = $"acteur-{Guid.NewGuid():N}";
        _configuration.Ajouter(acteurId, commande.Nom, commande.Couleur);
        return Result<AjouterActeurResultat>.Succes(new AjouterActeurResultat(acteurId));
    }
}
