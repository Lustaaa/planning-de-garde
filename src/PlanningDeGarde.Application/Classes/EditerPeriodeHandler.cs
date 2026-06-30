using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'édition d'une période existante depuis la dialog (re-bornage et/ou réaffectation).
/// <paramref name="EtatObserve"/> est le snapshot affiché par l'auteur : il porte l'<b>identifiant stable</b>
/// (clé du store, jamais un libellé) ET sert de jeton de version (concurrence optimiste réutilisée, Sc.10 s01).
/// <paramref name="NouveauResponsableId"/> et les nouvelles bornes décrivent l'état voulu.
/// </summary>
public sealed record EditerPeriodeCommand(
    PeriodeSnapshot EtatObserve, string NouveauResponsableId, System.DateTime NouveauDebut, System.DateTime NouvelleFin);

/// <summary>Confirmation d'une édition aboutie : le snapshot édité (même identifiant stable).</summary>
public sealed record EditerPeriodeResultat(PeriodeSnapshot Periode);

/// <summary>
/// Use case : éditer une période existante (re-borner et/ou réaffecter). Réutilise le port d'écriture
/// optimiste <see cref="IPeriodeRepository.Modifier"/> — aucune règle de concurrence neuve (décision CP,
/// un seul modèle par agrégat). La case re-résout après édition (priorité surcharge &gt; fond &gt; neutre,
/// palier 6) via le read model, sans logique de résolution neuve.
/// </summary>
public sealed class EditerPeriodeHandler
{
    private readonly IPeriodeRepository _periodes;

    public EditerPeriodeHandler(IPeriodeRepository periodes) => _periodes = periodes;

    public Result<EditerPeriodeResultat> Handle(EditerPeriodeCommand commande)
    {
        var modification = commande.EtatObserve with
        {
            ResponsableId = commande.NouveauResponsableId,
            Debut = commande.NouveauDebut,
            Fin = commande.NouvelleFin,
        };
        _periodes.Modifier(commande.EtatObserve, modification);
        return Result<EditerPeriodeResultat>.Succes(new EditerPeriodeResultat(modification));
    }
}
