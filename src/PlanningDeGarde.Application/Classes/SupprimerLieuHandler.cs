using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de suppression d'un lieu du référentiel du foyer (config). Le handler retire le lieu via
/// le port d'écriture <see cref="IEditeurLieux"/> : il cesse d'être énuméré et n'est plus acceptable
/// à la saisie. Borne s27 : aucune réécriture rétroactive — un slot déjà posé sur ce lieu conserve
/// son lieu.
/// </summary>
public sealed record SupprimerLieuCommand(string LieuId);

/// <summary>Confirmation d'une suppression aboutie (ou d'un no-op idempotent) : l'id du lieu visé.</summary>
public sealed record SupprimerLieuResultat(string LieuId);

/// <summary>
/// Use case : supprimer un lieu du référentiel du foyer via le port d'écriture du référentiel.
/// </summary>
public sealed class SupprimerLieuHandler
{
    private readonly IEditeurLieux _referentiel;

    public SupprimerLieuHandler(IEditeurLieux referentiel)
    {
        _referentiel = referentiel;
    }

    public Result<SupprimerLieuResultat> Handle(SupprimerLieuCommand commande)
    {
        // Le lieu disparaît du référentiel : il cesse d'être énuméré et n'est plus acceptable à la
        // saisie. Tolérant à l'absence (un lieu déjà supprimé = no-op qui réussit). Aucune réécriture
        // rétroactive : les slots déjà posés sur ce lieu conservent leur lieu (borne s27).
        _referentiel.Supprimer(commande.LieuId);
        return Result<SupprimerLieuResultat>.Succes(new SupprimerLieuResultat(commande.LieuId));
    }
}
