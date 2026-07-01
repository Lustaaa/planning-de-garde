using System.Collections.Generic;
using PlanningDeGarde.Application;

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
    /// non le libellé qui retombait sur le gris neutre (cadrage (B), Sc.6). Les libellés portent les
    /// noms RÉELS du foyer (Alice/Bruno) — plus aucun « Parent A / Parent B » fictif exposé (sprint 19,
    /// Sc.4) ; l'unification sur le store vivant des acteurs déclarés est portée par l'IHM (Sc.5).
    /// </summary>
    public static readonly IReadOnlyList<Responsable> Responsables = new[]
    {
        new Responsable("parent-a", "Alice"),
        new Responsable("parent-b", "Bruno"),
    };

    // Convergence s20 (Sc.1) : l'ancienne liste statique `ActeursEditables` qui peuplait le sélecteur
    // d'édition de l'écran de configuration a été RETIRÉE. Le sélecteur énumère désormais les acteurs
    // DEPUIS LE STORE VIVANT UNIFIÉ (IEnumerationActeursFoyer, via GET /api/foyer/acteurs) — même et
    // unique chemin de lecture du référentiel acteurs que les dialogs et la grille. Plus aucune source
    // statique parallèle susceptible de diverger du store.
}

/// <summary>Un responsable du foyer : identifiant stable (bindé) et libellé (affiché).</summary>
public sealed record Responsable(string Id, string Libelle);

/// <summary>Un acteur du foyer <b>énuméré depuis le store durable</b> via le canal de lecture (et non
/// la liste statique <see cref="Foyer.ActeursEditables"/>) : identifiant stable + nom d'affichage
/// courant + couleur courante (neutre « gris » par contrat si l'acteur n'en a pas). C'est cette
/// énumération qui fait apparaître un acteur fraîchement ajouté (Sc.1), nom et pastille de couleur.
/// Le <see cref="Type"/> (Admin / Parent / Autre) est surfacé en lecture seule depuis le seed (D3,
/// sprint 14) ; défaut <see cref="TypeActeur.Parent"/> si absent (acteur ajouté en session).</summary>
public sealed record ActeurFoyer(string Id, string Nom, string Couleur = "gris", TypeActeur Type = TypeActeur.Parent);
