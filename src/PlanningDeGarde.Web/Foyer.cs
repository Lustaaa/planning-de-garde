using System.Collections.Generic;

namespace PlanningDeGarde.Web;

/// <summary>
/// Référentiel d'affichage du foyer (lieux, responsables) pour peupler les sélecteurs de l'IHM
/// WASM. Côté front, ce n'est qu'une aide de saisie : la validation réelle (existence du lieu, du
/// responsable) reste celle des use cases derrière l'API distante — l'UI ne décide rien.
/// </summary>
public static class Foyer
{
    public static readonly IReadOnlyList<string> Lieux = new[]
    {
        "école", "domicile A", "domicile B", "nounou"
    };

    /// <summary>
    /// Responsables du foyer comme paires (identifiant stable, libellé). Le sélecteur affiche le
    /// <see cref="Responsable.Libelle"/> mais bind l'<see cref="Responsable.Id"/> : le canal reçoit
    /// l'identifiant stable (<c>parent-a</c>/<c>parent-b</c>), clé atteignable du set de couleurs — et
    /// non le libellé qui retombait sur le gris neutre (cadrage (B), Sc.6).
    /// </summary>
    public static readonly IReadOnlyList<Responsable> Responsables = new[]
    {
        new Responsable("parent-a", "Parent A"),
        new Responsable("parent-b", "Parent B"),
    };

    /// <summary>
    /// Les acteurs <b>éditables</b> du foyer pour l'écran de configuration (renommer / recolorier),
    /// comme paires (identifiant stable, nom d'affichage courant). Le sélecteur affiche le
    /// <see cref="Responsable.Libelle"/> (vrai nom du foyer) mais bind l'<see cref="Responsable.Id"/>
    /// stable (clé de résolution nom+couleur, règle 18) : <c>parent-a</c> (Alice), <c>parent-b</c>
    /// (Bruno), <c>parent-c</c> (Marie-Hélène Grand-Dubois), <c>grand-pere</c> (grand-père, hors set
    /// couleur). Aide de saisie côté front (miroir du seed) : la valeur résolue de référence reste le
    /// store derrière l'API distante. Distincte de <see cref="Responsables"/> (libellés génériques
    /// hérités du sélecteur d'affectation, dont des tests de caractérisation dépendent).
    /// </summary>
    public static readonly IReadOnlyList<Responsable> ActeursEditables = new[]
    {
        new Responsable("parent-a", "Alice"),
        new Responsable("parent-b", "Bruno"),
        new Responsable("parent-c", "Marie-Hélène Grand-Dubois"),
        new Responsable("grand-pere", "grand-père"),
    };
}

/// <summary>Un responsable du foyer : identifiant stable (bindé) et libellé (affiché).</summary>
public sealed record Responsable(string Id, string Libelle);

/// <summary>Un acteur du foyer <b>énuméré depuis le store durable</b> via le canal de lecture (et non
/// la liste statique <see cref="Foyer.ActeursEditables"/>) : identifiant stable + nom d'affichage
/// courant + couleur courante (neutre « gris » par contrat si l'acteur n'en a pas). C'est cette
/// énumération qui fait apparaître un acteur fraîchement ajouté (Sc.1), nom et pastille de couleur.</summary>
public sealed record ActeurFoyer(string Id, string Nom, string Couleur = "gris");
