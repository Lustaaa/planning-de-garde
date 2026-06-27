namespace PlanningDeGarde.Web;

/// <summary>
/// Corps des requêtes d'écriture émises par le front <b>WASM</b> vers le canal d'écriture de
/// l'<b>API distante</b> (endpoints <c>/api/canal/*</c>). Ce sont de simples DTO de transport
/// sérialisés en JSON : le front ne porte plus le mapping d'endpoints (qui vit côté hôte d'API
/// détaché, <c>PlanningDeGarde.Api</c>), il n'en émet que les corps.
/// </summary>
public static class CanalEcriture
{
    /// <summary>Corps de la requête de pose de slot émise via le canal requête/réponse.</summary>
    public sealed record PoserSlotRequete(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la requête d'affectation de période émise via le canal requête/réponse.</summary>
    public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la requête de définition d'un transfert de bascule émise via le canal.</summary>
    public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

    /// <summary>Corps de la requête d'édition d'un acteur émise via le canal d'écriture. Le nom et la
    /// couleur sont deux champs optionnels et indépendants : un champ absent (null) n'est pas appliqué
    /// (renommage seul au Sc.1, recoloriage seul au Sc.2). L'identifiant stable n'est jamais éditable.</summary>
    public sealed record EditerActeurRequete(string ActeurId, string? Nom = null, string? Couleur = null);

    /// <summary>Corps de la requête d'ajout d'un acteur neuf au foyer émise via le canal d'écriture.
    /// Le front ne fournit que le nom (+ couleur optionnelle) : l'identifiant stable neuf est généré
    /// côté handler, jamais dérivé du libellé.</summary>
    public sealed record AjouterActeurRequete(string Nom, string? Couleur = null);

    /// <summary>Corps de la requête de définition / ré-édition du cycle de fond (palier 6) émise via le
    /// canal d'écriture : le nombre de semaines + le mapping index→responsable (identifiant stable bindé
    /// par le sélecteur, jamais le libellé). Une nouvelle définition remplace le cycle courant.</summary>
    public sealed record DefinirCycleRequete(int NombreSemaines, IReadOnlyDictionary<int, string> Affectations);
}
