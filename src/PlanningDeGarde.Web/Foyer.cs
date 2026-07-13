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
/// sprint 14) ; défaut <see cref="TypeActeur.Parent"/> si absent (acteur ajouté en session). Le
/// <see cref="RoleId"/> (s21) est l'identifiant stable du rôle du référentiel porté par l'acteur, ou
/// <c>null</c> = « sans rôle » (attribut optionnel, neutre assumé) : pré-sélectionne le sélecteur de rôle
/// borné au référentiel et affiche le rôle courant (jamais un libellé en dur).</summary>
public sealed record ActeurFoyer(string Id, string Nom, string Couleur = "gris", TypeActeur Type = TypeActeur.Parent, string? RoleId = null, string? Adresse = null);

/// <summary>Un rôle du référentiel du foyer <b>énuméré depuis le store durable</b> via le canal de
/// lecture (s21) : identifiant stable opaque (clé, jamais le libellé) + libellé d'affichage éditable +
/// flag <see cref="EstRoleParent"/> « est un rôle parent » (s36, B1 — source de vérité de l'éligibilité
/// au lien enfant↔parent, jamais le libellé). Alimente la liste des rôles de l'onglet Acteurs, le sélecteur
/// de rôle borné au référentiel, la case « rôle parent » de la modal Rôles et le sélecteur des parents.</summary>
public sealed record RoleFoyer(string Id, string Libelle, bool EstRoleParent = false);

/// <summary>Une affectation déclarée du cycle de fond <b>lue depuis le store</b> via le canal de lecture
/// (s33, Sc.3, GET /api/foyer/cycles) : index de semaine (0..N-1) → identifiant stable du responsable de
/// fond. Alimente le TABLEAU en lecture seule de l'onglet Cycle (Sc.10), qui rend visibles toutes les
/// affectations déclarées (y compris celles auparavant invisibles, retour PO gate s32) ; le nom est
/// résolu sur l'identifiant via la liste des acteurs, jamais un libellé en dur.</summary>
public sealed record CycleFoyer(int IndexSemaine, string ResponsableId);

/// <summary>Une activité du référentiel du foyer <b>énumérée depuis le store vivant</b> via le canal de lecture
/// (s35, GET /api/foyer/activites — ex-« lieu » s27) : identifiant stable (clé, bindé par les sélecteurs) +
/// libellé d'affichage + <b>adresse</b> (Sc.2, optionnelle, vide par défaut) + <b>enfants liés</b> (Sc.3, ids
/// stables résolus en prénoms par la colonne « Enfants liés »). Remplace la liste en dur <see cref="Foyer.Lieux"/>
/// — alimente l'onglet Activités de la config ET les sélecteurs de lieu (axe LOCALISATION du slot, préservé)
/// des dialogs « Poser un slot » / « Définir un transfert » (une activité ajoutée / éditée / supprimée suit sans
/// rechargement, temps réel SignalR lecture).</summary>
public sealed record ActiviteFoyer(string Id, string Libelle, string Adresse = "")
{
    /// <summary>Identifiants stables des enfants (référentiel s30) liés à cette activité (lien N-M, s35 Sc.3) —
    /// résolus en prénoms par la colonne « Enfants liés » (Sc.4). Propriété <c>init</c> peuplée depuis le JSON
    /// quand présente, sinon liste vide (lien optionnel).</summary>
    public IReadOnlyCollection<string> EnfantsLies { get; init; } = System.Array.Empty<string>();
}

/// <summary>Un enfant du référentiel du foyer <b>énuméré depuis le store vivant</b> via le canal de lecture
/// (s30, GET /api/foyer/enfants) : identifiant stable opaque (clé, bindé par les sélecteurs, jamais le
/// prénom) + prénom d'affichage. Alimente l'onglet Enfants de la config ET le sélecteur d'enfant de la
/// dialog « Poser un slot » (un enfant ajouté / édité suit sans rechargement, temps réel SignalR lecture) —
/// remplace le fantôme <c>Session.EnfantId</c> transmis à l'aveugle (s29).</summary>
public sealed record EnfantFoyer(string Id, string Prenom)
{
    /// <summary>Parents-acteurs liés (0..2, s34), chacun avec son <b>rôle-du-lien</b> (père / mère /
    /// parent-libre, s37) — résolus en noms + rôle par la colonne « Parents liés ». Propriété <c>init</c>
    /// (constructeur positionnel à 2 args préservé pour la pose / les tests) : System.Text.Json la peuple
    /// depuis le JEU JSON quand présente, sinon liste vide. Record <see cref="ParentLie"/> réutilisé.</summary>
    public IReadOnlyCollection<ParentLie> ParentsLies { get; init; } = System.Array.Empty<ParentLie>();
}

/// <summary>Un compte utilisateur du foyer <b>énuméré depuis le store durable</b> via le canal de lecture
/// (s22) : identifiant stable opaque (clé, jamais l'email) + email + statut (« inactif » / « actif ») +
/// id stable de l'acteur associé (ou <c>null</c> = désassocié). Alimente l'affichage du compte associé à
/// un acteur et de son statut dans l'onglet Acteurs.</summary>
public sealed record CompteFoyer(string Id, string Email, string Statut, string? ActeurId);
