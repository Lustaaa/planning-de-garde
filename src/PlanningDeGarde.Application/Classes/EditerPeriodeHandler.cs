using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande d'édition d'une période existante depuis la dialog (re-bornage). <paramref name="EtatObserve"/>
/// est le snapshot affiché par l'auteur : il porte l'<b>identifiant stable</b> (clé du store, jamais un
/// libellé) ET sert de jeton de version (concurrence optimiste réutilisée, Sc.10 s01). Les nouvelles bornes
/// remplacent l'intervalle ; le responsable observé est conservé.
/// </summary>
public sealed record EditerPeriodeCommand(PeriodeSnapshot EtatObserve, System.DateTime NouveauDebut, System.DateTime NouvelleFin);

/// <summary>Confirmation d'une édition aboutie : le snapshot re-borné (mêmes identifiant et responsable).</summary>
public sealed record EditerPeriodeResultat(PeriodeSnapshot Periode);

/// <summary>
/// Use case : éditer une période existante (re-borner). Réutilise le port d'écriture optimiste
/// <see cref="IPeriodeRepository.Modifier"/> — aucune règle de concurrence neuve (décision CP, un seul
/// modèle par agrégat). La case re-résout après ré-bornage (repli surcharge &gt; fond &gt; neutre, palier 6).
/// </summary>
public sealed class EditerPeriodeHandler
{
    private readonly IPeriodeRepository _periodes;

    public EditerPeriodeHandler(IPeriodeRepository periodes) => _periodes = periodes;

    public Result<EditerPeriodeResultat> Handle(EditerPeriodeCommand commande)
    {
        var modification = commande.EtatObserve with { Debut = commande.NouveauDebut, Fin = commande.NouvelleFin };
        _periodes.Modifier(commande.EtatObserve, modification);
        return Result<EditerPeriodeResultat>.Succes(new EditerPeriodeResultat(modification));
    }
}
