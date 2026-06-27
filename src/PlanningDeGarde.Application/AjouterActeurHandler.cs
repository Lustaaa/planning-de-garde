using System;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'ajout d'un acteur au foyer (config). À la différence d'<c>EditerActeur</c> (qui
/// mute un acteur déjà semé sur son id stable), l'ajout <b>crée</b> un acteur neuf : le handler
/// génère un identifiant stable neuf opaque (jamais dérivé du libellé) et persiste le nom
/// (+ couleur fournie, sinon repli neutre par contrat <see cref="IPaletteCouleurs"/>) via le port
/// d'écriture. La couleur est optionnelle (Sc.5) ; le nom non vide sera exigé au Sc.8.
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
        // Identifiant stable neuf OPAQUE, généré (jamais dérivé du libellé, anti-pattern s06) et
        // unique (GUID → jamais un id existant). La résolution nom/couleur se fait sur cet id.
        var acteurId = $"acteur-{Guid.NewGuid():N}";
        _configuration.Ajouter(acteurId, commande.Nom, commande.Couleur);
        return Result<AjouterActeurResultat>.Succes(new AjouterActeurResultat(acteurId));
    }
}
