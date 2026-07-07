using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'ajout d'un lieu au référentiel du foyer (config). Le handler persiste le libellé
/// sur un identifiant stable via le port d'écriture <see cref="IEditeurLieux"/>, pour que le lieu
/// devienne disponible à la saisie (validation de pose + sélecteurs).
/// </summary>
public sealed record AjouterLieuCommand(string Libelle);

/// <summary>Confirmation d'un ajout abouti : l'identifiant stable du lieu.</summary>
public sealed record AjouterLieuResultat(string LieuId);

/// <summary>
/// Use case : ajouter un lieu au référentiel du foyer via le port d'écriture du référentiel.
/// </summary>
public sealed class AjouterLieuHandler
{
    private readonly IEditeurLieux _referentiel;

    public AjouterLieuHandler(IEditeurLieux referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<AjouterLieuResultat> Handle(AjouterLieuCommand commande)
    {
        // Le lieu historique porte son libellé comme identifiant stable (parité avec le seed, préserve
        // les slots déjà posés) ; le rejet du libellé vide / doublon est piloté par S3.
        var lieuId = commande.Libelle;
        _referentiel.Ajouter(lieuId, commande.Libelle);
        return Result<AjouterLieuResultat>.Succes(new AjouterLieuResultat(lieuId));
    }
}
