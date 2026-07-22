using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Periodes.Handlers;

/// <summary>
/// Commande d'édition d'une période existante depuis la dialog (re-bornage et/ou réaffectation).
/// <paramref name="EtatObserve"/> est le snapshot affiché par l'auteur : il porte l'<b>identifiant stable</b>
/// (clé du store, jamais un libellé) ET sert de jeton de version (concurrence optimiste réutilisée).
/// <paramref name="NouveauResponsableId"/> et les nouvelles bornes décrivent l'état voulu.
/// </summary>
public sealed record EditerPeriodeCommand(
    PeriodeSnapshot EtatObserve, string NouveauResponsableId, System.DateTime NouveauDebut, System.DateTime NouvelleFin);

/// <summary>Confirmation d'une édition aboutie : le snapshot édité (même identifiant stable).</summary>
public sealed record EditerPeriodeResultat(PeriodeSnapshot Periode);

/// <summary>
/// Use case : éditer une période existante (re-borner et/ou réaffecter). Réutilise le port d'écriture
/// optimiste <see cref="IPeriodeRepository.Modifier"/> — aucune règle de concurrence neuve (un seul
/// modèle par agrégat). La case re-résout après édition (priorité surcharge &gt; fond &gt; neutre)
/// via le read model, sans logique de résolution neuve.
/// </summary>
public sealed class EditerPeriodeHandler
{
    private readonly IPeriodeRepository _periodes;

    public EditerPeriodeHandler(IPeriodeRepository periodes) => _periodes = periodes;

    public Result<EditerPeriodeResultat> Handle(EditerPeriodeCommand commande)
    {
        // Tell-Don't-Ask : l'agrégat porte l'invariant des bornes (et du responsable). On le consulte
        // AVANT toute écriture — un état voulu invalide est refusé sans jamais toucher le port (rien appliqué).
        var validation = PeriodeDeGarde.Affecter(commande.NouveauResponsableId, commande.NouveauDebut, commande.NouvelleFin);
        if (!validation.EstSucces)
            return Result<EditerPeriodeResultat>.Echec(validation.Motif!);

        var modification = commande.EtatObserve with
        {
            ResponsableId = commande.NouveauResponsableId,
            Debut = commande.NouveauDebut,
            Fin = commande.NouvelleFin,
        };
        // Concurrence optimiste (décision CP : rejet sur état périmé, pas last-write-wins) : le port
        // n'écrit que si l'état observé est encore l'état courant. Devancé → false → rejet, rien appliqué.
        var enregistree = _periodes.Modifier(commande.EtatObserve, modification);
        if (!enregistree)
            return Result<EditerPeriodeResultat>.Echec(
                "Édition rejetée : l'état affiché est périmé, veuillez recharger la période à jour.");

        return Result<EditerPeriodeResultat>.Succes(new EditerPeriodeResultat(modification));
    }
}
