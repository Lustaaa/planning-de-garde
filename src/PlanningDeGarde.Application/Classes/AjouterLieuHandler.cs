using System.Linq;
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
    private readonly IEnumerationLieux _lieux;
    private readonly IEditeurLieux _referentiel;

    public AjouterLieuHandler(IEnumerationLieux lieux, IEditeurLieux referentiel)
    {
        _lieux = lieux;
        _referentiel = referentiel;
    }

    public Result<AjouterLieuResultat> Handle(AjouterLieuCommand commande)
    {
        // Garde « libellé requis » (S3, miroir R5/R10) : un libellé vide ou tout-espaces est refusé
        // AVANT toute écriture — aucun lieu vide persisté, référentiel inchangé.
        if (string.IsNullOrWhiteSpace(commande.Libelle))
            return Result<AjouterLieuResultat>.Echec("libellé requis");

        // Garde « libellé déjà défini » (S3, miroir R6/R10) : refus si un lieu porte déjà ce libellé —
        // aucun doublon persisté, référentiel inchangé (unicité lue sur le référentiel courant).
        if (_lieux.EnumererLieux().Any(lieu => lieu.Libelle == commande.Libelle))
            return Result<AjouterLieuResultat>.Echec("libellé déjà défini");

        // Le lieu historique porte son libellé comme identifiant stable (parité avec le seed, préserve
        // les slots déjà posés).
        var lieuId = commande.Libelle;
        _referentiel.Ajouter(lieuId, commande.Libelle);
        return Result<AjouterLieuResultat>.Succes(new AjouterLieuResultat(lieuId));
    }
}
