using System.Linq;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Activites.Handlers;

/// <summary>
/// Commande d'ajout d'un lieu au référentiel du foyer (config). Le handler persiste le libellé
/// sur un identifiant stable via le port d'écriture <see cref="IEditeurActivites"/>, pour que le lieu
/// devienne disponible à la saisie (validation de pose + sélecteurs). L'<b>adresse</b> est optionnelle
/// (<c>null</c> = non renseignée) : quand elle est fournie à la création, elle est persistée dans la
/// foulée (write-through durable) — corrige le trou où l'adresse saisie au formulaire de création était
/// silencieusement perdue (retour PO s54 : « les adresses des lieux ne sont pas persistées »).
/// </summary>
public sealed record AjouterActiviteCommand(string Libelle, string? Adresse = null);

/// <summary>Confirmation d'un ajout abouti : l'identifiant stable du lieu.</summary>
public sealed record AjouterActiviteResultat(string LieuId);

/// <summary>
/// Use case : ajouter un lieu au référentiel du foyer via le port d'écriture du référentiel.
/// </summary>
public sealed class AjouterActiviteHandler
{
    private readonly IEnumerationActivites _lieux;
    private readonly IEditeurActivites _referentiel;

    public AjouterActiviteHandler(IEnumerationActivites lieux, IEditeurActivites referentiel)
    {
        _lieux = lieux;
        _referentiel = referentiel;
    }

    public Result<AjouterActiviteResultat> Handle(AjouterActiviteCommand commande)
    {
        // Garde « libellé requis » (S3, miroir R5/R10) : un libellé vide ou tout-espaces est refusé
        // AVANT toute écriture — aucun lieu vide persisté, référentiel inchangé.
        if (string.IsNullOrWhiteSpace(commande.Libelle))
            return Result<AjouterActiviteResultat>.Echec("libellé requis");

        // Garde « libellé déjà défini » (S3, miroir R6/R10) : refus si un lieu porte déjà ce libellé —
        // aucun doublon persisté, référentiel inchangé (unicité lue sur le référentiel courant).
        if (_lieux.EnumererActivites().Any(lieu => lieu.Libelle == commande.Libelle))
            return Result<AjouterActiviteResultat>.Echec("libellé déjà défini");

        // Le lieu historique porte son libellé comme identifiant stable (parité avec le seed, préserve
        // les slots déjà posés).
        var lieuId = commande.Libelle;
        _referentiel.Ajouter(lieuId, commande.Libelle);
        // Adresse fournie à la création : write-through immédiat sur la MÊME clé stable (surface distincte
        // du libellé, miroir de l'édition). Non fournie (null) = rien à écrire (adresse « vide » à la lecture).
        // Une adresse vide/tout-espaces fournie n'est pas persistée (pas de bruit) mais reste licite en édition.
        if (!string.IsNullOrWhiteSpace(commande.Adresse))
            _referentiel.ChangerAdresse(lieuId, commande.Adresse);
        return Result<AjouterActiviteResultat>.Succes(new AjouterActiviteResultat(lieuId));
    }
}
