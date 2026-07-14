using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Écran de configuration du foyer (front <b>WASM</b>) : renomme un acteur déjà semé. L'écriture
/// passe par le <b>canal requête/réponse</b> (endpoint HTTP <c>/api/canal/editer-acteur</c>),
/// JAMAIS par un appel de handler en DI direct ni par le canal de diffusion (SignalR, lecture
/// seule) — règle 27. Sur succès, la vue confirme l'effet et <b>reste</b> sur l'écran (l'édition
/// est volatile, on peut en enchaîner d'autres) : la grille partagée suit sans rechargement via la
/// diffusion temps réel déclenchée par l'édition aboutie côté API. Sur refus, le motif métier
/// propagé est affiché. Aucune règle métier dans l'UI : l'identifiant stable est la clé (jamais
/// éditable), seuls le nom et la couleur mutent — deux surfaces indépendantes (un champ laissé
/// vide est envoyé <c>null</c> et n'est pas appliqué).
/// </summary>
public partial class ConfigurationFoyer
{
    // Onglet actif de la page de configuration (présentation façon settings GitHub : barre latérale
    // gauche + panneau). « acteurs » (défaut) / « roles » / « cycles ». Les trois panneaux restent
    // TOUJOURS rendus (masqués par CSS quand inactifs) : aucune règle métier, aucun gating porté ici —
    // le gating d'écriture reste sur l'identité effective, dans chaque panneau.
    private string _onglet = "acteurs";

    private void ChoisirOnglet(string onglet) => _onglet = onglet;

    // ── État de la MODAL acteur (refonte s32, patron crayon → modal) ──
    // Identifiant stable de l'acteur en cours d'édition dans la modal (null = pas d'édition ouverte).
    // C'est le crayon de la ligne qui le pose (jamais un select, jamais dérivé du libellé, règle 19).
    private string? _modalActeurId;
    // Modal ouverte en mode CRÉATION (bouton « Ajouter un acteur ») : champs vides, pas d'acteur porté.
    private bool _modalAjout;

    /// <summary>Ouvre la modal d'ÉDITION sur un acteur (clic crayon de sa ligne) : porte son id stable,
    /// pré-remplit le nom courant et repart d'une couleur « inchangée », efface les messages de l'édition
    /// précédente. L'écriture (nom/couleur/rôle/compte/admin/suppression) s'exerce ensuite dans la modal.</summary>
    private void OuvrirEdition(string acteurId)
    {
        _modalAjout = false;
        _modalActeurId = acteurId;
        _form.ActeurId = acteurId;
        // Sc.6 : la palette pré-sélectionne la couleur COURANTE de l'acteur (pastille active). Comme le champ
        // est pré-réglé, une édition nom/adresse-seule ré-émet la même couleur (idempotent), sans écrasement.
        _form.Couleur = _acteurs.FirstOrDefault(x => x.Id == acteurId)?.Couleur ?? "";
        // Toggles pré-réglés sur l'état COURANT (Sc.4) : admin si l'acteur est admin ; actif si son compte
        // (s'il en porte un) est déjà actif. À l'Enregistrer, seule une bascule OFF→ON est appliquée.
        _form.Admin = EstAdmin(acteurId);
        _form.Actif = CompteDe(acteurId) is { } compte && !EstInactif(compte);
        // Sc.5 : pré-remplit l'adresse avec la valeur courante de l'acteur (vide s'il n'en porte pas).
        _form.Adresse = _acteurs.FirstOrDefault(x => x.Id == acteurId)?.Adresse ?? "";
        PreRemplirNom();
    }

    /// <summary>Ouvre la MÊME modal en mode CRÉATION (bouton « Ajouter un acteur ») : tous les champs vides,
    /// aucun acteur pré-sélectionné, motif d'échec précédent effacé.</summary>
    private void OuvrirAjout()
    {
        _modalActeurId = null;
        _modalAjout = true;
        _ajout.Nom = "";
        _ajout.Couleur = "";
        _motifEchecAjout = null;
    }

    /// <summary>Ferme la modal (annuler, ou après un enregistrement abouti) sans émettre aucune commande :
    /// le tableau reste inchangé côté annulation. Efface les messages d'édition volatils.</summary>
    private void FermerModal()
    {
        _modalActeurId = null;
        _modalAjout = false;
        _confirmation = null;
        _motifEchec = null;
        _motifEchecAjout = null;
    }

    private sealed class Formulaire
    {
        public string ActeurId { get; set; } = "";
        public string Nom { get; set; } = "";
        public string Couleur { get; set; } = "";
        // Sc.5 (s33) : adresse de résidence éditable, pré-remplie sur la valeur courante à l'ouverture.
        // Champ optionnel (une adresse vide est acceptée, Sc.1) — envoyé tel quel à l'Enregistrer.
        public string Adresse { get; set; } = "";
        // Sc.4 (s33) : état désiré des toggles admin / actif de la modal, pré-réglés sur l'état courant à
        // l'ouverture. SENS UNIQUE : seul OFF→ON est appliqué à l'Enregistrer (commandes existantes) ; un
        // toggle déjà ON est rendu verrouillé (pas de commande inverse). Deux surfaces indépendantes.
        public bool Admin { get; set; }
        public bool Actif { get; set; }
    }

    private sealed class FormulaireAjout
    {
        public string Nom { get; set; } = "";
        public string Couleur { get; set; } = "";
    }

    private readonly Formulaire _form = new();
    private string? _confirmation;
    private string? _motifEchec;

    private readonly FormulaireAjout _ajout = new();
    private string? _motifEchecAjout;

    /// <summary>Accusé non bloquant de suppression (registre avertissement-à-part, aligné « Transfert
    /// défini » — D5) : affiché à côté de la liste sans interrompre la consultation, effacé à la
    /// suppression suivante.</summary>
    private string? _accuseSuppression;

    /// <summary>Motif d'échec de suppression (service injoignable, règle 28) — surface distincte de
    /// l'accusé : la liste/grille/légende restent inchangées, aucune mise en file (Sc.8).</summary>
    private string? _motifEchecSuppression;

    private sealed class FormulaireCycle
    {
        public int NombreSemaines { get; set; } = 2;
        public Dictionary<int, string> Affectations { get; } = new();
    }

    private readonly FormulaireCycle _cycle = new();
    private string? _confirmationCycle;
    private string? _motifEchecCycle;

    /// <summary>Affectations déclarées du cycle de fond lues depuis le store (GET /api/foyer/cycles, Sc.3) :
    /// alimentent le TABLEAU lecture seule de l'onglet Cycle (Sc.10), qui rend visibles toutes les semaines
    /// affectées — y compris celles auparavant invisibles (retour PO gate s32).</summary>
    private IReadOnlyList<CycleFoyer> _cyclesDeclares = Array.Empty<CycleFoyer>();

    // ── État de la MODAL cycle (refonte s33, Sc.10 — patron crayon → modal) ──
    // La modal héberge l'éditeur EXISTANT (N + selects par semaine) déplacé de l'inline (aucun autre changement).
    private bool _modalCycleOuverte;

    /// <summary>Ouvre la modal d'édition du cycle (clic « Éditer le cycle ») : l'éditeur est déjà pré-rempli
    /// sur le cycle courant (<see cref="_cycle"/> synchronisé depuis les affectations déclarées).</summary>
    private void OuvrirEditionCycle()
    {
        _modalCycleOuverte = true;
        _confirmationCycle = null;
        _motifEchecCycle = null;
    }

    /// <summary>Ferme la modal cycle (annuler ou après un enregistrement abouti), sans émettre de commande.
    /// Ne touche pas <see cref="_confirmationCycle"/> : sur succès, l'accusé posé par <see cref="DefinirCycle"/>
    /// reste affiché dans le panneau (hors modal) après fermeture.</summary>
    private void FermerModalCycle()
    {
        _modalCycleOuverte = false;
        _motifEchecCycle = null;
    }

    /// <summary>Résout le nom d'affichage du responsable d'une semaine du cycle (sur l'identifiant stable,
    /// jamais un libellé en dur), pour le tableau lecture seule (Sc.10) ; repli sur l'id brut si non résolu.</summary>
    private string NomResponsableCycle(string responsableId)
        => _acteurs.FirstOrDefault(a => a.Id == responsableId)?.Nom ?? responsableId;

    /// <summary>Libellé lisible d'une semaine du cycle (finition PO s33) : par parité pour le cas courant
    /// (cycle ISO 2 semaines) — index 0 = « Semaine paire », index 1 = « Semaine impaire ». Pour un cycle
    /// plus long (N &gt; 2), les index ≥ 2 conservent « Semaine d'index k » afin d'éviter des libellés paire/
    /// impaire dupliqués et ambigus.</summary>
    private static string LibelleSemaineCycle(int index) => index switch
    {
        0 => "Semaine paire",
        1 => "Semaine impaire",
        _ => $"Semaine d'index {index}",
    };

    /// <summary>Affecte (ou retire, si vide) un responsable à un index de semaine du cycle en cours de
    /// saisie. La valeur bindée est l'identifiant stable de l'acteur (jamais le libellé, règle 19).</summary>
    private void AffecterIndex(int index, string? responsableId)
    {
        if (string.IsNullOrWhiteSpace(responsableId))
            _cycle.Affectations.Remove(index);
        else
            _cycle.Affectations[index] = responsableId;
    }

    /// <summary>Acteurs du foyer énumérés <b>depuis le store durable</b> (canal de lecture HTTP), et non
    /// la liste statique front : c'est cette énumération qui fait apparaître un acteur ajouté (Sc.1).</summary>
    private IReadOnlyList<ActeurFoyer> _acteurs = Array.Empty<ActeurFoyer>();

    /// <summary>Formulaire de saisie du libellé d'un rôle à créer (référentiel du foyer, s21). Le front
    /// n'émet que le libellé ; l'identifiant stable neuf opaque est généré côté handler.</summary>
    private sealed class FormulaireRole
    {
        public string Libelle { get; set; } = "";
        // Sc.6 (s36) : état désiré de la case « rôle parent » de la modal, pré-réglé sur le flag COURANT du
        // rôle à l'ouverture. À l'« Enregistrer » en édition, une bascule vis-à-vis du flag courant émet
        // marquer-role-parent (source de vérité de l'éligibilité, jamais le libellé).
        public bool EstParent { get; set; }
    }

    private readonly FormulaireRole _role = new();
    private string? _motifEchecRole;

    // ── État de la MODAL rôle (refonte s33, Sc.8 — patron crayon → modal) ──
    // Identifiant stable du rôle en cours d'édition (null = pas d'édition ouverte) ; posé par le crayon.
    private string? _modalRoleId;
    // Modal ouverte en mode CRÉATION (bouton « Ajouter un rôle ») : champ vide, aucun rôle porté.
    private bool _modalRoleAjout;

    /// <summary>Ouvre la modal d'ÉDITION sur un rôle (clic crayon) : porte son id stable, pré-remplit son
    /// libellé courant, efface le motif d'échec précédent.</summary>
    private void OuvrirEditionRole(string roleId)
    {
        _modalRoleAjout = false;
        _modalRoleId = roleId;
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        _role.Libelle = role?.Libelle ?? "";
        _role.EstParent = role?.EstRoleParent ?? false; // case pré-réglée sur le flag COURANT (Sc.6)
        _motifEchecRole = null;
    }

    /// <summary>Ouvre la MÊME modal en mode CRÉATION (bouton « Ajouter un rôle ») : champ vide, aucun rôle porté.</summary>
    private void OuvrirAjoutRole()
    {
        _modalRoleId = null;
        _modalRoleAjout = true;
        _role.Libelle = "";
        _role.EstParent = false; // un rôle créé démarre non-parent (Sc.3) ; la case se coche après création
        _motifEchecRole = null;
    }

    /// <summary>Ferme la modal rôle (annuler ou après un enregistrement abouti), sans émettre de commande.</summary>
    private void FermerModalRole()
    {
        _modalRoleId = null;
        _modalRoleAjout = false;
        _motifEchecRole = null;
    }

    /// <summary>Rôles du référentiel du foyer énumérés <b>depuis le store durable</b> (GET /api/foyer/roles),
    /// jamais un rôle en dur : alimente la liste des rôles de l'onglet Acteurs (créés / renommés / supprimés
    /// suivent sans rechargement, Sc.7).</summary>
    private IReadOnlyList<RoleFoyer> _roles = Array.Empty<RoleFoyer>();

    /// <summary>Comptes utilisateurs du foyer énumérés <b>depuis le store durable</b> (GET /api/foyer/comptes),
    /// jamais en dur : alimente l'affichage du compte associé à chaque acteur et de son statut dans l'onglet
    /// Acteurs (créés / désassociés suivent sans rechargement, Sc.7).</summary>
    private IReadOnlyList<CompteFoyer> _comptes = Array.Empty<CompteFoyer>();

    /// <summary>Tampon de saisie de l'email de création de compte, par ligne d'acteur : clé = id stable de
    /// l'acteur, valeur = email saisi. La clé n'est jamais éditable (règle 19).</summary>
    private readonly Dictionary<string, string> _emailCompte = new();

    /// <summary>Motif d'échec de création de compte, par ligne d'acteur (clé = id stable de l'acteur) : sur
    /// refus métier (email vide / doublon, Sc.2) ou service injoignable, le formulaire de la ligne reste
    /// ouvert avec ce motif clair, sans compte créé (Sc.7).</summary>
    private readonly Dictionary<string, string> _motifEchecCompte = new();

    /// <summary>Ids stables des acteurs admins du foyer énumérés <b>depuis le store durable</b> (GET
    /// /api/foyer/admins), jamais en dur : marque l'acteur admin dans l'onglet Acteurs ; suit une désignation
    /// aboutie ailleurs sans rechargement (temps réel SignalR, Sc.9).</summary>
    private IReadOnlyList<string> _admins = Array.Empty<string>();

    /// <summary>Formulaire d'édition / création d'une activité (référentiel du foyer, s35, patron crayon →
    /// modal miroir Acteurs s32 / Enfants s34) : libellé + adresse (optionnelle). L'identifiant stable est
    /// posé côté handler en création (jamais dérivé du libellé) ; en édition il est porté par la modal.</summary>
    private sealed class FormulaireActivite
    {
        public string Libelle { get; set; } = "";
        public string Adresse { get; set; } = "";
    }

    private readonly FormulaireActivite _activite = new();
    private string? _motifEchecActivite;

    // ── État de la MODAL activité (refonte s35, Sc.4 — patron crayon → modal) ──
    // Identifiant stable de l'activité en cours d'édition (null = pas d'édition ouverte) ; posé par le crayon.
    private string? _modalActiviteId;
    // Modal ouverte en mode CRÉATION (bouton « Ajouter une activité ») : champs vides, aucune activité portée.
    private bool _modalActiviteAjout;

    /// <summary>Sélection des enfants à lier dans la modal activité (Sc.5) : initialisée aux enfants COURANTS de
    /// l'activité à l'ouverture (pré-cochés). À l'« Enregistrer », les diffs vis-à-vis des enfants courants
    /// émettent lier / délier. Lien N-M : AUCUNE borne de cardinalité (0..N des deux côtés, cadrage SM).</summary>
    private readonly HashSet<string> _selectionEnfants = new();

    /// <summary>Ouvre la modal d'ÉDITION sur une activité (clic crayon) : porte son id stable, pré-remplit son
    /// libellé + adresse courants et la sélection d'enfants sur les enfants liés COURANTS (pré-cochés, Sc.5),
    /// efface le motif d'échec précédent.</summary>
    private void OuvrirEditionActivite(string activiteId)
    {
        _modalActiviteAjout = false;
        _modalActiviteId = activiteId;
        var activite = _activites.FirstOrDefault(a => a.Id == activiteId);
        _activite.Libelle = activite?.Libelle ?? "";
        _activite.Adresse = activite?.Adresse ?? "";
        _selectionEnfants.Clear();
        foreach (var enfantId in activite?.EnfantsLies ?? Array.Empty<string>())
            _selectionEnfants.Add(enfantId);
        _motifEchecActivite = null;
    }

    /// <summary>Ouvre la MÊME modal en mode CRÉATION (bouton « Ajouter une activité ») : champs vides, aucune
    /// activité portée, aucune sélection d'enfant (les liens se posent après la création, en édition).</summary>
    private void OuvrirAjoutActivite()
    {
        _modalActiviteId = null;
        _modalActiviteAjout = true;
        _activite.Libelle = "";
        _activite.Adresse = "";
        _selectionEnfants.Clear();
        _motifEchecActivite = null;
    }

    /// <summary>Ferme la modal activité (annuler ou après un enregistrement abouti), sans émettre de commande.</summary>
    private void FermerModalActivite()
    {
        _modalActiviteId = null;
        _modalActiviteAjout = false;
        _selectionEnfants.Clear();
        _motifEchecActivite = null;
    }

    /// <summary>Bascule la sélection d'un enfant dans la modal activité (Sc.5). Lien N-M : aucune borne — on
    /// ajoute / retire librement l'id stable de l'enfant (jamais son prénom).</summary>
    private void BasculerEnfant(string enfantId, bool lie)
    {
        if (lie)
            _selectionEnfants.Add(enfantId);
        else
            _selectionEnfants.Remove(enfantId);
    }

    /// <summary>Libellé de lecture des enfants liés d'une activité (Sc.4) : les identifiants stables des enfants
    /// résolus en prénoms (jamais un libellé en dur), séparés par « , » ; « — » si aucun enfant lié.</summary>
    private string LibelleEnfantsLies(ActiviteFoyer activite)
    {
        var noms = activite.EnfantsLies
            .Select(id => _enfants.FirstOrDefault(e => e.Id == id)?.Prenom ?? id)
            .ToList();
        return noms.Count == 0 ? "—" : string.Join(", ", noms);
    }

    /// <summary>Formulaire de saisie du prénom d'un enfant à ajouter (référentiel du foyer, s30). Le front
    /// n'émet que le prénom ; l'identifiant stable opaque est généré côté handler.</summary>
    private sealed class FormulaireEnfant
    {
        public string Prenom { get; set; } = "";
    }

    private readonly FormulaireEnfant _enfant = new();
    private string? _motifEchecEnfant;

    // ── État de la MODAL enfant (refonte s34, Sc.4 — patron crayon → modal) ──
    // Identifiant stable de l'enfant en cours d'édition (null = pas d'édition ouverte) ; posé par le crayon.
    private string? _modalEnfantId;
    // Modal ouverte en mode CRÉATION (bouton « Ajouter un enfant ») : champ vide, aucun enfant porté.
    private bool _modalEnfantAjout;

    /// <summary>Sélection des parents-acteurs à lier dans la modal (Sc.5), chacun avec son <b>rôle-du-lien</b>
    /// choisi (père / mère / parent-libre, s37) : initialisée aux parents COURANTS de l'enfant à l'ouverture
    /// (pré-cochés, rôle pré-réglé sur le rôle courant). À l'« Enregistrer », les diffs vis-à-vis des parents
    /// courants (présence OU rôle) émettent lier (rôle inclus) / délier. Bornée à 2 (l'UI désactive une case
    /// non cochée quand 2 sont sélectionnés).</summary>
    private readonly Dictionary<string, RoleDuLien> _selectionParents = new();

    /// <summary>Ouvre la modal d'ÉDITION sur un enfant (clic crayon) : porte son id stable, pré-remplit son
    /// prénom courant et la sélection de parents sur les parents liés COURANTS (pré-cochés, rôle-du-lien
    /// pré-réglé, Sc.5/s37), efface le motif d'échec précédent.</summary>
    private void OuvrirEditionEnfant(string enfantId)
    {
        _modalEnfantAjout = false;
        _modalEnfantId = enfantId;
        var enfant = _enfants.FirstOrDefault(e => e.Id == enfantId);
        _enfant.Prenom = enfant?.Prenom ?? "";
        _selectionParents.Clear();
        foreach (var parent in enfant?.ParentsLies ?? Array.Empty<ParentLie>())
            _selectionParents[parent.ActeurId] = parent.Role;
        _motifEchecEnfant = null;
    }

    /// <summary>Ouvre la MÊME modal en mode CRÉATION (bouton « Ajouter un enfant ») : champ vide, aucun enfant
    /// porté, aucune sélection de parent (les liens se posent après la création, en édition).</summary>
    private void OuvrirAjoutEnfant()
    {
        _modalEnfantId = null;
        _modalEnfantAjout = true;
        _enfant.Prenom = "";
        _selectionParents.Clear();
        _motifEchecEnfant = null;
    }

    /// <summary>Ferme la modal enfant (annuler ou après un enregistrement abouti), sans émettre de commande.</summary>
    private void FermerModalEnfant()
    {
        _modalEnfantId = null;
        _modalEnfantAjout = false;
        _selectionParents.Clear();
        _motifEchecEnfant = null;
    }

    /// <summary>Acteurs candidats au lien parent (s34 Sc.5, éligibilité role-based s36 Sc.4/Sc.5) : exactement
    /// ceux dont le rôle affecté est marqué « est rôle parent » (le FLAG du rôle, résolu sur l'id, jamais le
    /// libellé ni le TypeActeur). L'IHM suit EXACTEMENT la règle back (<c>LierEnfantParentHandler</c>) — aucun
    /// critère divergent. Un acteur sans rôle, ou à rôle non marqué (Nounou/Grand-parent), n'apparaît pas.</summary>
    private IEnumerable<ActeurFoyer> ActeursParents()
        => _acteurs.Where(a => a.RoleId is { } roleId
            && _roles.FirstOrDefault(r => r.Id == roleId)?.EstRoleParent == true);

    /// <summary>Bascule la sélection d'un parent dans la modal (Sc.5). Borne « 2 parents max » reflétée à
    /// l'écran : on n'ajoute pas au-delà de 2 (les cases non cochées sont d'ailleurs désactivées). Un parent
    /// nouvellement coché démarre au rôle-du-lien neutre « parent-libre » (s37), ajustable ensuite.</summary>
    private void BasculerParent(string acteurId, bool lie)
    {
        if (lie)
        {
            if (_selectionParents.Count < 2)
                _selectionParents[acteurId] = RoleDuLien.ParentLibre;
        }
        else
        {
            _selectionParents.Remove(acteurId);
        }
    }

    /// <summary>Choisit le rôle-du-lien (père / mère / parent-libre, s37) d'un parent SÉLECTIONNÉ dans la modal :
    /// mémorisé dans la sélection, émis à l'« Enregistrer » via la commande « lier » (rôle inclus).</summary>
    private void ChoisirRoleParent(string acteurId, RoleDuLien role)
    {
        if (_selectionParents.ContainsKey(acteurId))
            _selectionParents[acteurId] = role;
    }

    /// <summary>Rôle-du-lien courant choisi pour un parent sélectionné (pré-réglé sur son rôle courant à
    /// l'ouverture, s37), défaut « parent-libre » si non sélectionné — pour pré-régler le sélecteur.</summary>
    private RoleDuLien RoleDeParent(string acteurId)
        => _selectionParents.TryGetValue(acteurId, out var role) ? role : RoleDuLien.ParentLibre;

    /// <summary>Libellé d'affichage d'un rôle-du-lien (s37) : « père » / « mère » / « parent » (parent-libre).</summary>
    private static string LibelleRoleDuLien(RoleDuLien role) => role switch
    {
        RoleDuLien.Pere => "père",
        RoleDuLien.Mere => "mère",
        _ => "parent",
    };

    /// <summary>Libellé de lecture des parents liés d'un enfant (Sc.4/Sc.5) : chaque parent résolu en NOM
    /// d'acteur (jamais un libellé en dur) suivi de son <b>rôle-du-lien</b> entre parenthèses
    /// (« Alice (père) », s37), séparés par « , » ; « — » si aucun parent lié.</summary>
    private string LibelleParentsLies(EnfantFoyer enfant)
    {
        var libelles = enfant.ParentsLies
            .Select(p => $"{_acteurs.FirstOrDefault(a => a.Id == p.ActeurId)?.Nom ?? p.ActeurId} ({LibelleRoleDuLien(p.Role)})")
            .ToList();
        return libelles.Count == 0 ? "—" : string.Join(", ", libelles);
    }

    /// <summary>Enfants du référentiel du foyer énumérés <b>depuis le store vivant</b> (GET /api/foyer/enfants),
    /// jamais un enfant en dur : alimente la liste de l'onglet Enfants (ajoutés / édités suivent sans
    /// rechargement, S9) — même source que le sélecteur d'enfant de la dialog de pose (S10).</summary>
    private IReadOnlyList<EnfantFoyer> _enfants = Array.Empty<EnfantFoyer>();

    /// <summary>Graphe foyer « enfant-racine » lu <b>depuis le store vivant</b> (GET /api/foyer/graphe, s38) :
    /// vue LECTURE SEULE affichée à l'arrivée sur la Config foyer — chaque enfant en racine, ses parents liés
    /// (nom résolu + rôle-du-lien) en branches, déjà filtrés des orphelins côté API (reflet fidèle, zéro
    /// fantôme). Ré-énuméré à la diffusion SignalR (un lien modifié dans la modal Enfants converge le graphe).</summary>
    private IReadOnlyList<GrapheEnfant> _graphe = Array.Empty<GrapheEnfant>();

    /// <summary>Libellé d'une branche parent du graphe (s38) : « nom (rôle-du-lien) » — le nom déjà résolu côté
    /// API + le rôle-du-lien en clair (père / mère / parent), miroir de la colonne « Parents liés » (Sc.5 s37).</summary>
    private static string LibelleBrancheParent(GrapheParent parent)
        => $"{parent.Nom} ({LibelleRoleDuLien(parent.Role)})";

    /// <summary>Libellé du badge de complétude du couple R3 (s40, Sc.4) — présentation seule : « couple complet »
    /// / « couple incomplet » / « aucun parent » (état neutre pour une racine isolée, distinct d'une anomalie).
    /// Le statut est déjà calculé côté API (GrapheFoyerQuery), l'UI ne fait que le libeller (aucune règle métier).</summary>
    private static string LibelleBadgeCouple(StatutCoupleR3 statut) => statut switch
    {
        StatutCoupleR3.Complet => "couple complet",
        StatutCoupleR3.Incomplet => "couple incomplet",
        _ => "aucun parent",
    };

    /// <summary>Suffixe de classe CSS du badge de complétude (s40) : « complet » / « incomplet » / « vide »
    /// — pilote uniquement l'apparence (couleur/ton), jamais une règle métier.</summary>
    private static string BadgeCoupleClasse(StatutCoupleR3 statut) => statut switch
    {
        StatutCoupleR3.Complet => "complet",
        StatutCoupleR3.Incomplet => "incomplet",
        _ => "vide",
    };

    /// <summary>Activités du référentiel du foyer énumérées <b>depuis le store vivant</b> (GET /api/foyer/activites,
    /// s35), jamais une activité en dur : alimente le tableau de l'onglet Activités (ajoutées / éditées / supprimées
    /// suivent sans rechargement, S6) — même source que les sélecteurs de lieu des dialogs.</summary>
    private IReadOnlyList<ActiviteFoyer> _activites = Array.Empty<ActiviteFoyer>();

    /// <summary>Fournisseur de services pour résoudre <see cref="OptionsConnexionHub"/> de façon
    /// <b>optionnelle</b> : présent, il redirige la connexion au hub vers le TestServer (acceptation runtime
    /// Sc.6) ; absent (écrans de config qui n'observent pas le temps réel), la connexion reste neutre et son
    /// éventuel échec est simplement avalé — l'écran demeure fonctionnel.</summary>
    [Inject] private IServiceProvider Services { get; set; } = default!;

    private HubConnection? _hub;

    /// <summary>Au montage de l'écran, charge acteurs et rôles depuis le store via l'API distante.</summary>
    protected override async Task OnInitializedAsync()
    {
        await RechargerActeurs();
        await RechargerRoles();
        await RechargerComptes();
        await RechargerAdmins();
        await RechargerActivites();
        await RechargerEnfants();
        await RechargerCycles();
        await RechargerGraphe();
    }

    /// <summary>Charge le graphe foyer « enfant-racine » À L'ARRIVÉE depuis le store vivant via la query
    /// AGRÉGÉE serveur (GET /api/foyer/graphe → <c>GrapheFoyerQuery</c>, s38 Sc.1/Sc.2) : source CANONIQUE de
    /// la vue lecture seule (orphelins filtrés côté serveur, contrat d'existence). C'est le chemin de lecture
    /// consommé à l'arrivée sur la Config foyer (goal : « quand on arrive… »).</summary>
    private async Task RechargerGraphe()
        => _graphe = await Canal.GetFromJsonAsync<List<GrapheEnfant>>("api/foyer/graphe")
            ?? new List<GrapheEnfant>();

    /// <summary>Reprojette le graphe EN TEMPS RÉEL (convergence Sc.5) à partir des données DÉJÀ rechargées sur la
    /// diffusion (<see cref="_enfants"/> avec liens + rôles-du-lien, <see cref="_acteurs"/> pour existence + nom) —
    /// SANS aller-retour HTTP supplémentaire par diffusion (le canal SignalR pousse à haute fréquence ; un GET de
    /// plus par push alourdit inutilement le trafic de lecture). Miroir EXACT des règles de <c>GrapheFoyerQuery</c>
    /// (Sc.1/Sc.2) : mêmes branches, même filtre d'orphelin (contrat d'existence = l'acteur figure dans
    /// <see cref="_acteurs"/>), même nom résolu, même rôle-du-lien — aucune sémantique divergente, la source
    /// canonique reste la query serveur consommée à l'arrivée. Lecture PURE, aucune écriture.</summary>
    private void ReprojeterGraphe()
        => _graphe = _enfants
            .Select(e => new GrapheEnfant(e.Id, e.Prenom)
            {
                Parents = e.ParentsLies
                    .Where(p => _acteurs.Any(a => a.Id == p.ActeurId)) // filtre orphelin (zéro fantôme), miroir Resolvable
                    .Select(p => new GrapheParent(p.ActeurId, _acteurs.First(a => a.Id == p.ActeurId).Nom, p.Role))
                    .ToList(),
            })
            .ToList();

    /// <summary>Ré-énumère les affectations déclarées du cycle de fond depuis le store (GET /api/foyer/cycles,
    /// Sc.3) : alimente le tableau lecture seule de l'onglet Cycle (Sc.10). Quand la modal cycle n'est pas
    /// ouverte, synchronise aussi l'éditeur <see cref="_cycle"/> sur le cycle courant (N dérivé de la plus
    /// grande semaine affectée + 1, cas nominal « un responsable par semaine »), pour qu'un clic « Éditer le
    /// cycle » l'ouvre pré-rempli — sans écraser une saisie en cours si la modal est ouverte.</summary>
    private async Task RechargerCycles()
    {
        _cyclesDeclares = await Canal.GetFromJsonAsync<List<CycleFoyer>>("api/foyer/cycles")
            ?? new List<CycleFoyer>();

        if (!_modalCycleOuverte)
        {
            _cycle.Affectations.Clear();
            foreach (var c in _cyclesDeclares)
                _cycle.Affectations[c.IndexSemaine] = c.ResponsableId;
            if (_cyclesDeclares.Count > 0)
                _cycle.NombreSemaines = _cyclesDeclares.Max(c => c.IndexSemaine) + 1;
        }
    }

    /// <summary>Ré-énumère les enfants du référentiel depuis le store vivant (GET /api/foyer/enfants) : c'est
    /// cette relecture qui fait suivre la liste des enfants après ajout / édition (S9), sans rechargement.</summary>
    private async Task RechargerEnfants()
        => _enfants = await Canal.GetFromJsonAsync<List<EnfantFoyer>>("api/foyer/enfants")
            ?? new List<EnfantFoyer>();

    /// <summary>Ré-énumère les activités du référentiel depuis le store vivant (GET /api/foyer/activites, s35) :
    /// c'est cette relecture qui fait suivre le tableau des activités après ajout / édition / suppression (S6),
    /// sans rechargement.</summary>
    private async Task RechargerActivites()
        => _activites = await Canal.GetFromJsonAsync<List<ActiviteFoyer>>("api/foyer/activites")
            ?? new List<ActiviteFoyer>();

    private async Task RechargerActeurs()
        => _acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs")
            ?? new List<ActeurFoyer>();

    /// <summary>Ré-énumère les rôles du référentiel depuis le store durable (GET /api/foyer/roles) : c'est
    /// cette relecture qui fait suivre la liste des rôles après création / renommage / suppression (Sc.7).</summary>
    private async Task RechargerRoles()
        => _roles = await Canal.GetFromJsonAsync<List<RoleFoyer>>("api/foyer/roles")
            ?? new List<RoleFoyer>();

    /// <summary>Ré-énumère les comptes du foyer depuis le store durable (GET /api/foyer/comptes) : c'est
    /// cette relecture qui fait suivre l'affichage du compte associé à un acteur après création / désassociation
    /// (Sc.7), sans rechargement.</summary>
    private async Task RechargerComptes()
        => _comptes = await Canal.GetFromJsonAsync<List<CompteFoyer>>("api/foyer/comptes")
            ?? new List<CompteFoyer>();

    /// <summary>Ré-énumère les admins du foyer depuis le store durable (GET /api/foyer/admins) : c'est cette
    /// relecture qui fait suivre le marqueur d'admin après une désignation aboutie, sans rechargement (Sc.9).</summary>
    private async Task RechargerAdmins()
        => _admins = await Canal.GetFromJsonAsync<List<string>>("api/foyer/admins")
            ?? new List<string>();

    /// <summary>
    /// S'abonne au <b>hub SignalR de lecture</b> de l'API distante (même hôte que le canal) pour préserver
    /// le <b>temps réel</b> sur l'écran de configuration (Sc.6) : une écriture aboutie ailleurs — typiquement
    /// un acteur ajouté ou renommé depuis un second écran (store partagé) — <b>ré-énumère</b> les acteurs
    /// depuis le store unifié, si bien que le sélecteur d'édition (onglet Acteurs) et la liste suivent
    /// <b>sans rechargement</b>, cohérents avec la grille, la légende et les sélecteurs des dialogs. Lecture
    /// seule : la diffusion ne déclenche jamais d'écriture. Le temps réel est un confort : si le hub est
    /// indisponible, l'écran reste fonctionnel (rechargement à la navigation).
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            var urlHub = new Uri(Canal.BaseAddress!, "hubs/planning");
            var configurer = Services.GetService<OptionsConnexionHub>()?.Configurer ?? (_ => { });
            _hub = new HubConnectionBuilder()
                .WithUrl(urlHub, configurer)
                .WithAutomaticReconnect()
                .Build();

            _hub.On(PlanningHubEvenement.MiseAJour, async () =>
            {
                // Ré-énumère acteurs ET rôles depuis le store partagé : une création / suppression de rôle
                // aboutie sur un autre écran (store partagé) fait suivre la liste des rôles et les sélecteurs
                // de rôle sans rechargement, et un acteur portant un rôle supprimé retombe « sans rôle »
                // (repli neutre) — cohérence temps réel du référentiel de rôles (Sc.10). Lecture seule.
                await RechargerActeurs();
                await RechargerRoles();
                await RechargerComptes();
                await RechargerAdmins();
                // Un ajout / une édition / une suppression d'activité abouti sur un autre écran (store partagé)
                // fait suivre le tableau des activités sans rechargement — cohérence temps réel du référentiel (S6).
                await RechargerActivites();
                // Un ajout / une édition d'enfant abouti sur un autre écran (store partagé) fait suivre la
                // liste des enfants sans rechargement — cohérence temps réel du référentiel d'enfants (S9/S10).
                await RechargerEnfants();
                // Une édition du cycle aboutie sur un autre écran fait converger le tableau des cycles déclarés
                // sans rechargement (Sc.11) — diffusion en LECTURE SEULE (ré-énumération du store).
                await RechargerCycles();
                // Un lien enfant↔parent ajouté / supprimé ou un rôle-du-lien modifié (modal Enfants) sur un autre
                // écran fait CONVERGER le graphe foyer sans rechargement (s38 Sc.5) — diffusion LECTURE SEULE.
                // Reprojection LOCALE à partir des enfants + acteurs déjà rechargés ci-dessus (aucun GET de plus
                // par diffusion) : miroir exact de GrapheFoyerQuery, la source canonique restant la query serveur
                // consommée à l'arrivée (RechargerGraphe). Évite d'alourdir le trafic de lecture à chaque push.
                ReprojeterGraphe();
                await InvokeAsync(StateHasChanged);
            });

            await _hub.StartAsync();
        }
        catch
        {
            // Hub indisponible : le temps réel est un confort, l'écran reste consultable et éditable.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }

    /// <summary>Revient à l'identité réelle depuis le bandeau d'incarnation de l'écran de configuration
    /// (sprint 14, cohérence inter-écrans, Sc.2) : l'incarnation est levée → les écritures config
    /// redeviennent visibles (gating sur l'identité effective). Aucune écriture domaine.</summary>
    private void RevenirIdentiteReelle() => Session.RevenirIdentiteReelle();

    /// <summary>Nom d'affichage courant de l'acteur sélectionné (aide de saisie, miroir du seed) —
    /// <c>null</c> tant qu'aucun acteur n'est choisi. Sert d'indicateur « ce que vous éditez ».</summary>
    private string? NomActuel
        => _acteurs.FirstOrDefault(a => a.Id == _form.ActeurId)?.Nom;

    /// <summary>À la sélection d'un acteur, pré-remplit le champ nom avec son nom courant et efface les
    /// messages de l'édition précédente — pour qu'on parte de la valeur en place plutôt que d'un champ
    /// vide. L'utilisateur ajuste ensuite le nom et/ou la couleur.</summary>
    private void PreRemplirNom()
    {
        _confirmation = null;
        _motifEchec = null;
        _form.Nom = NomActuel ?? "";
    }

    private async Task Soumettre()
    {
        _confirmation = null;
        _motifEchec = null;

        // Couleur : une pastille non choisie (chaîne vide) part null (non appliquée). Depuis s33 Sc.6 la
        // palette pré-sélectionne la couleur courante, donc une édition normale ré-émet cette couleur.
        var couleur = string.IsNullOrWhiteSpace(_form.Couleur) ? null : _form.Couleur;
        // Nom : le champ est PRÉ-REMPLI à l'ouverture (patron modal s32/s33). On émet donc sa valeur telle
        // quelle — un nom VIDÉ volontairement part vide et est refusé côté domaine (« le nom ne peut pas être
        // vide », Sc.7), motif surfacé dans la modal restée ouverte. (L'ancien repli « nom vide + couleur =
        // recoloriage-seul » est caduc : avec la palette pré-sélectionnée la couleur n'est jamais null, il
        // avalait le nom vide et court-circuitait le refus — régression corrigée s33 Sc.6.)
        var nom = _form.Nom;

        HttpResponseMessage reponse;
        try
        {
            // Émission de la commande d'édition via le canal HTTP de l'API distante (adaptateur de gauche).
            // L'adresse (Sc.5) part telle quelle : pré-remplie sur la valeur courante, elle n'écrase donc pas
            // par erreur lors d'une édition nom/couleur-seule, et une adresse vidée volontairement est acceptée (Sc.1).
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/editer-acteur",
                new EditerActeurRequete(_form.ActeurId, nom, couleur, _form.Adresse));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, saisie conservée,
            // aucune écriture ni mise en file. Cf. Sc.9.
            _motifEchec = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Le canal renvoie le motif métier en corps JSON (Results.BadRequest(string)) : on le
            // désérialise comme la chaîne qu'il est, pour surfacer un message propre (« le nom ne peut
            // pas être vide ») sans guillemets parasites (Sc.5). La modal RESTE OUVERTE, saisie conservée.
            _motifEchec = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // Sc.4 (s33) : la modal porte désormais les toggles admin/actif appliqués au MÊME « Enregistrer ».
        // SENS UNIQUE — n'émettre les commandes EXISTANTES que sur une bascule OFF→ON (un toggle déjà ON est
        // verrouillé à l'écran, aucune bascule OFF no-op). Sur refus/injoignable, la modal reste ouverte avec
        // le motif, sans écriture partielle relue (le tableau n'est relu qu'après le succès complet).
        if (_form.Admin && !EstAdmin(_form.ActeurId) && !await AppliquerToggle("api/canal/designer-admin", new DesignerAdminRequete(_form.ActeurId)))
            return;

        if (_form.Actif && CompteDe(_form.ActeurId) is { } compteAActiver && EstInactif(compteAActiver))
        {
            if (!await AppliquerToggle("api/canal/activer-compte", new ActiverCompteRequete(compteAActiver.Id)))
                return;
            _accuseActivation = "Compte activé.";
        }

        // Succès complet : le tableau est relu (acteurs + comptes + admins) et la modal se ferme — l'état
        // neuf (badge admin / actif) est reflété sans rechargement (la grille partagée suit via la diffusion).
        _confirmation = "Modification enregistrée.";
        await RechargerActeurs();
        await RechargerComptes();
        await RechargerAdmins();
        FermerModal();
    }

    /// <summary>Émet une commande de toggle (Sc.4) via le canal HTTP réel et renvoie <c>true</c> en succès.
    /// Sur service injoignable ou refus métier, pose le motif dans la modal (<c>_motifEchec</c>) et renvoie
    /// <c>false</c> — la modal reste ouverte, aucun tableau relu (pas d'écriture partielle affichée).</summary>
    private async Task<bool> AppliquerToggle<TRequete>(string route, TRequete requete)
    {
        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(route, requete);
        }
        catch (HttpRequestException)
        {
            _motifEchec = MessagesEcriture.ServiceInjoignable;
            return false;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchec = await reponse.Content.ReadFromJsonAsync<string>();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ajoute un acteur neuf au foyer via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/ajouter-acteur</c>, règle 27 — aucune vue n'écrit le domaine en direct),
    /// puis ré-énumère le store pour faire apparaître l'acteur ajouté <b>sans rechargement</b> (Sc.1).
    /// Sur refus métier (Sc.8, nom vide), le motif renvoyé par le canal est surfacé sans muter la liste.
    /// Sur <b>service injoignable</b> (Sc.9 s09, échec de transport <see cref="HttpRequestException"/> avant
    /// que le handler ne tourne), un message dédié s'affiche, la saisie est conservée et rien n'est enregistré.
    /// </summary>
    private async Task Ajouter()
    {
        _motifEchecAjout = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/ajouter-acteur",
                new AjouterActeurRequete(_ajout.Nom, _ajout.Couleur));
        }
        catch (HttpRequestException)
        {
            // Service de configuration injoignable (échec de transport, pas un refus métier Sc.8) : le
            // handler AjouterActeur ne s'exécute jamais. Message dédié, saisie « Carla / rose » conservée
            // à resoumettre, aucune écriture ni mise en file (règle 28). Cf. Sc.9 (s09).
            _motifEchecAjout = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Refus métier (nom vide / tout-espaces, Sc.8) : le canal renvoie le motif en corps JSON
            // (Results.BadRequest(string)). On le surface tel quel à l'écran, sans muter la liste ni
            // effacer la saisie — aucun identifiant n'est généré, la liste des acteurs reste inchangée.
            _motifEchecAjout = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // La liste reflète l'ajout sans recharger la page : on relit l'énumération du store durable, puis
        // la modal de création se ferme (refonte s32, Sc.4) — le nouvel acteur apparaît aussitôt.
        await RechargerActeurs();
        _ajout.Nom = "";
        _ajout.Couleur = "";
        FermerModal();
    }

    /// <summary>
    /// Supprime un acteur du foyer via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/supprimer-acteur</c>, règle 27 — aucune vue n'écrit le domaine en direct),
    /// puis ré-énumère le store pour que l'acteur supprimé <b>quitte la liste sans rechargement</b> (Sc.6).
    /// Sur succès, un accusé <b>« Acteur supprimé »</b> non bloquant s'affiche à part (D5) et le handler a
    /// muté le store ET déclenché la diffusion temps réel (grilles et légende dédoublonnée suivent — le
    /// filtre d'existence côté projection neutralise l'acteur orphelin). Sur <b>service injoignable</b>
    /// (échec de transport <see cref="HttpRequestException"/>, règle 28), un message dédié s'affiche, la
    /// liste/grille/légende restent inchangées, rien n'est mis en file (Sc.8). La clé est l'identifiant
    /// stable opaque (jamais le libellé, règle 19) ; aucune règle métier dans l'UI (idempotence côté handler).
    /// </summary>
    private async Task Supprimer(string acteurId)
    {
        _motifEchecSuppression = null;

        // Accusé posé — et rendu — AVANT l'appel réseau. Raison : la suppression aboutie côté API déclenche
        // une diffusion SignalR MiseAJour qui, sur CE même écran, ré-énumère le store et fait quitter l'acteur
        // de la liste de façon concurrente à notre propre flux. Poser l'accusé après la réponse OK le mettrait
        // en course avec ce re-render de diffusion (l'acteur peut disparaître de la liste AVANT que l'accusé ne
        // soit posé → accusé absent au moment de l'observation, régression Sc.6). En le posant en amont, l'accusé
        // est déjà présent quel que soit le chemin qui retire l'acteur en premier. C'est un accusé optimiste :
        // on le rétracte sur échec (transport injoignable ou refus métier), sans qu'aucune suppression ait eu lieu.
        _accuseSuppression = "Acteur supprimé.";
        StateHasChanged();

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/supprimer-acteur",
                new SupprimerActeurRequete(acteurId));
        }
        catch (HttpRequestException)
        {
            // Service de configuration injoignable (échec de transport, pas un refus métier) : le handler
            // SupprimerActeur ne s'exécute jamais. On rétracte l'accusé optimiste, on surface le message dédié ;
            // liste/grille/légende inchangées, aucune suppression ni mise en file (règle 28). Cf. Sc.8.
            _accuseSuppression = null;
            _motifEchecSuppression = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Refus métier éventuel : on rétracte l'accusé optimiste et on surface le motif renvoyé par le
            // canal, sans muter la liste.
            _accuseSuppression = null;
            _motifEchecSuppression = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // La liste reflète la suppression sans recharger la page : on relit l'énumération du store durable,
        // puis la modal d'édition se ferme (refonte s32) — l'acteur supprimé quitte la table.
        await RechargerActeurs();
        FermerModal();
    }

    /// <summary>Libellé d'affichage du rôle courant d'un acteur (Sc.8) : le libellé du rôle du référentiel
    /// porté (résolu sur son id stable, jamais un libellé en dur), ou « sans rôle » si aucun (attribut
    /// optionnel non renseigné = neutre assumé).</summary>
    private string LibelleRoleActeur(string? roleId)
        => roleId is not null && _roles.FirstOrDefault(r => r.Id == roleId) is { } r
            ? r.Libelle
            : "sans rôle";

    /// <summary>
    /// Affecte (ou retire, si l'option « sans rôle » est choisie = valeur vide) un rôle du référentiel à un
    /// acteur via le <b>canal d'écriture HTTP</b> de l'API distante (POST /api/canal/affecter-role ou
    /// /retirer-role, règle 27 — aucune vue n'écrit le domaine en direct). La valeur émise est l'<b>id de
    /// rôle du référentiel</b> (jamais un libellé en dur, Sc.8) ; sur succès, on relit les acteurs pour que
    /// le rôle courant suive sans rechargement. Sur refus métier (id hors référentiel, Sc.4), le motif est surfacé.
    /// </summary>
    private async Task AffecterRole(string acteurId, string? roleId)
    {
        _motifEchecRole = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = string.IsNullOrWhiteSpace(roleId)
                ? await Canal.PostAsJsonAsync("api/canal/retirer-role", new RetirerRoleRequete(acteurId))
                : await Canal.PostAsJsonAsync("api/canal/affecter-role", new AffecterRoleRequete(acteurId, roleId));
        }
        catch (HttpRequestException)
        {
            _motifEchecRole = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecRole = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        await RechargerActeurs();
    }

    /// <summary>
    /// Enregistre la modal rôle (Sc.8) via le <b>canal d'écriture HTTP</b> de l'API distante (règle 27) :
    /// en mode CRÉATION émet <c>POST /api/canal/creer-role</c> (id stable neuf généré côté handler), en mode
    /// ÉDITION émet <c>POST /api/canal/renommer-role</c> sur l'id stable (jamais le libellé). Réutilise les
    /// commandes EXISTANTES (aucun handler neuf). Sur succès, on relit le référentiel (le libellé suit sans
    /// rechargement, Sc.2) et la modal se ferme. Sur refus métier (libellé vide / doublon) ou service
    /// injoignable, le motif est surfacé DANS la modal, qui reste ouverte et la saisie conservée (Sc.9).
    /// </summary>
    private async Task SoumettreRole()
    {
        _motifEchecRole = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = _modalRoleAjout
                ? await Canal.PostAsJsonAsync("api/canal/creer-role", new CreerRoleRequete(_role.Libelle))
                : await Canal.PostAsJsonAsync("api/canal/renommer-role", new RenommerRoleRequete(_modalRoleId!, _role.Libelle));
        }
        catch (HttpRequestException)
        {
            _motifEchecRole = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecRole = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // Sc.6 (s36) — en ÉDITION, si la case « rôle parent » a basculé vis-à-vis du flag COURANT, émet
        // marquer-role-parent (surface DISTINCTE du libellé, source de vérité de l'éligibilité). Aucun POST
        // si le flag est inchangé (rien à réémettre). En création, le rôle démarre non-parent (Sc.3).
        if (!_modalRoleAjout)
        {
            var flagCourant = _roles.FirstOrDefault(r => r.Id == _modalRoleId)?.EstRoleParent ?? false;
            if (_role.EstParent != flagCourant)
            {
                HttpResponseMessage reponseFlag;
                try
                {
                    reponseFlag = await Canal.PostAsJsonAsync(
                        "api/canal/marquer-role-parent",
                        new MarquerRoleParentRequete(_modalRoleId!, _role.EstParent));
                }
                catch (HttpRequestException)
                {
                    _motifEchecRole = MessagesEcriture.ServiceInjoignable;
                    return;
                }

                if (!reponseFlag.IsSuccessStatusCode)
                {
                    _motifEchecRole = await reponseFlag.Content.ReadFromJsonAsync<string>();
                    return;
                }
            }
        }

        await RechargerRoles();
        FermerModalRole();
    }

    /// <summary>Supprime un rôle du référentiel via le <b>canal d'écriture HTTP</b>
    /// (<c>POST /api/canal/supprimer-role</c>) depuis la modal d'édition (Sc.8) : la clé est l'identifiant
    /// stable du rôle. Sur succès, on relit le référentiel (le rôle quitte la table sans rechargement) ET
    /// les acteurs (un porteur du rôle supprimé retombe « sans rôle »), puis la modal se ferme. Idempotence
    /// côté handler ; sur refus / service injoignable, le motif reste DANS la modal ouverte.</summary>
    private async Task SupprimerRole(string roleId)
    {
        _motifEchecRole = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/supprimer-role",
                new SupprimerRoleRequete(roleId));
        }
        catch (HttpRequestException)
        {
            _motifEchecRole = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecRole = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        await RechargerRoles();
        await RechargerActeurs(); // un porteur du rôle supprimé retombe « sans rôle » (repli neutre)
        FermerModalRole();
    }

    /// <summary>
    /// Définit / ré-édite le cycle de fond via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/definir-cycle</c>, règle 27). Sur succès, la grille partagée suit sans
    /// rechargement via la diffusion temps réel déclenchée côté API. Sur refus métier (N &lt; 1, Sc.7),
    /// le motif propagé est affiché.
    /// </summary>
    private async Task DefinirCycle()
    {
        _confirmationCycle = null;
        _motifEchecCycle = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/definir-cycle",
                new DefinirCycleRequete(_cycle.NombreSemaines, _cycle.Affectations));
        }
        catch (HttpRequestException)
        {
            // Service de configuration injoignable (échec de transport, pas un refus métier Sc.7) : le
            // handler DefinirCycle ne s'exécute jamais. Message dédié, saisie du cycle (N + mapping)
            // conservée à resoumettre, aucun cycle enregistré ni mis en file (règle 28). Cf. Sc.8 / s09 Sc.9.
            _motifEchecCycle = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Refus métier (N < 1, Sc.11) : le motif reste DANS la modal ouverte, la saisie (N + affectations)
            // est conservée, le tableau inchangé — aucune écriture partielle.
            _motifEchecCycle = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // Succès : le cycle est relu (le tableau reflète l'édition sans rechargement), la modal se ferme (l'accusé
        // « Cycle de fond enregistré » reste affiché dans le panneau), et la grille partagée suit via la diffusion
        // temps réel déclenchée côté API (Sc.10).
        await RechargerCycles();
        FermerModalCycle();
        _confirmationCycle = "Cycle de fond enregistré.";
    }

    /// <summary>
    /// Enregistre la modal activité (Sc.4) via le <b>canal d'écriture HTTP</b> de l'API distante (règle 27) :
    /// en mode CRÉATION émet <c>POST /api/canal/ajouter-activite</c> (id stable neuf posé côté handler), en mode
    /// ÉDITION émet <c>POST /api/canal/editer-activite</c> sur l'id stable (jamais éditable) — libellé + adresse.
    /// Réutilise les commandes EXISTANTES (aucun handler neuf). Sur succès, on relit le référentiel (le tableau
    /// suit sans rechargement) et la modal se ferme. Sur refus métier (libellé vide) ou service injoignable, le
    /// motif est surfacé DANS la modal, qui reste ouverte et la saisie conservée (Sc.6).
    /// </summary>
    private async Task SoumettreActivite()
    {
        _motifEchecActivite = null;

        if (_modalActiviteAjout)
        {
            // Création : ajouter-activite seul (les liens enfant se posent ensuite en édition, Sc.5).
            if (!await PosterActivite("api/canal/ajouter-activite", new AjouterActiviteRequete(_activite.Libelle)))
                return;
        }
        else
        {
            // Édition : libellé + adresse (editer-activite) PUIS les diffs d'enfants liés (lier/délier). Sur le
            // PREMIER refus (métier ou injoignable), on s'arrête, motif dans la modal restée ouverte (Sc.6) — le
            // tableau n'est relu qu'après le succès complet.
            if (!await PosterActivite("api/canal/editer-activite",
                new EditerActiviteRequete(_modalActiviteId!, _activite.Libelle, _activite.Adresse)))
                return;

            var courant = _activites.FirstOrDefault(a => a.Id == _modalActiviteId)?.EnfantsLies
                ?? (IReadOnlyCollection<string>)Array.Empty<string>();

            foreach (var enfantId in _selectionEnfants.Where(id => !courant.Contains(id)).ToList())
                if (!await PosterActivite("api/canal/lier-enfant-activite", new LierEnfantActiviteRequete(enfantId, _modalActiviteId!)))
                    return;

            foreach (var enfantId in courant.Where(id => !_selectionEnfants.Contains(id)).ToList())
                if (!await PosterActivite("api/canal/delier-enfant-activite", new DelierEnfantActiviteRequete(enfantId, _modalActiviteId!)))
                    return;
        }

        await RechargerActivites();
        FermerModalActivite();
    }

    /// <summary>Émet une écriture activité (ajouter / éditer / lier / délier) via le canal HTTP réel et renvoie
    /// <c>true</c> en succès. Sur service injoignable ou refus métier, pose le motif dans la modal
    /// (<c>_motifEchecActivite</c>) et renvoie <c>false</c> — la modal reste ouverte, aucun tableau relu.</summary>
    private async Task<bool> PosterActivite<TRequete>(string route, TRequete requete)
    {
        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(route, requete);
        }
        catch (HttpRequestException)
        {
            _motifEchecActivite = MessagesEcriture.ServiceInjoignable;
            return false;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecActivite = await reponse.Content.ReadFromJsonAsync<string>();
            return false;
        }

        return true;
    }

    /// <summary>Supprime une activité du référentiel via le <b>canal d'écriture HTTP</b>
    /// (<c>POST /api/canal/supprimer-activite</c>) depuis la modal d'édition (Sc.4) : la clé est l'identifiant
    /// stable. Sur succès, on relit le référentiel (l'activité quitte le tableau et n'est plus proposée à la
    /// saisie, sans rechargement — S6), puis la modal se ferme. Idempotence côté handler ; borne : les slots
    /// déjà posés sur cette activité conservent leur lieu. Sur refus / injoignable, le motif reste DANS la modal.</summary>
    private async Task SupprimerActivite(string activiteId)
    {
        if (!await PosterActivite("api/canal/supprimer-activite", new SupprimerActiviteRequete(activiteId)))
            return;

        await RechargerActivites();
        FermerModalActivite();
    }

    /// <summary>
    /// Enregistre la modal enfant (Sc.4) via le <b>canal d'écriture HTTP</b> de l'API distante (règle 27) :
    /// en mode CRÉATION émet <c>POST /api/canal/ajouter-enfant</c> (id stable opaque neuf posé côté handler),
    /// en mode ÉDITION émet <c>POST /api/canal/editer-enfant</c> sur l'id stable (jamais éditable, seul le
    /// prénom change). Réutilise les commandes EXISTANTES (aucun handler neuf). Sur succès, on relit le
    /// référentiel (la table suit sans rechargement) et la modal se ferme. Sur refus métier (prénom vide /
    /// doublon) ou service injoignable, le motif est surfacé DANS la modal, qui reste ouverte et la saisie
    /// conservée (Sc.6).
    /// </summary>
    private async Task SoumettreEnfant()
    {
        _motifEchecEnfant = null;

        if (_modalEnfantAjout)
        {
            // Création : ajouter-enfant seul (les liens parents se posent ensuite en édition, Sc.5).
            if (!await PosterEnfant("api/canal/ajouter-enfant", new AjouterEnfantRequete(_enfant.Prenom)))
                return;
        }
        else
        {
            // Édition : prénom (editer-enfant) PUIS les diffs de parents liés (lier/délier). Sur le PREMIER
            // refus (métier ou injoignable), on s'arrête, motif dans la modal restée ouverte (Sc.6) — le
            // tableau n'est relu qu'après le succès complet.
            if (!await PosterEnfant("api/canal/editer-enfant", new EditerEnfantRequete(_modalEnfantId!, _enfant.Prenom)))
                return;

            var courant = (_enfants.FirstOrDefault(e => e.Id == _modalEnfantId)?.ParentsLies
                ?? (IReadOnlyCollection<ParentLie>)Array.Empty<ParentLie>())
                .ToDictionary(p => p.ActeurId, p => p.Role);

            // Lier (rôle inclus) tout parent sélectionné NOUVEAU ou dont le rôle-du-lien a CHANGÉ (s37) : le
            // handler réécrit le rôle sans dupliquer le lien. Sur le PREMIER refus, on s'arrête (modal ouverte).
            foreach (var (acteurId, role) in _selectionParents
                         .Where(kv => !courant.TryGetValue(kv.Key, out var r) || r != kv.Value).ToList())
                if (!await PosterEnfant("api/canal/lier-enfant-parent", new LierEnfantParentRequete(_modalEnfantId!, acteurId, role)))
                    return;

            foreach (var acteurId in courant.Keys.Where(id => !_selectionParents.ContainsKey(id)).ToList())
                if (!await PosterEnfant("api/canal/delier-enfant-parent", new DelierEnfantParentRequete(_modalEnfantId!, acteurId)))
                    return;
        }

        await RechargerEnfants();
        FermerModalEnfant();
    }

    /// <summary>Émet une écriture enfant (ajouter / éditer / lier / délier) via le canal HTTP réel et renvoie
    /// <c>true</c> en succès. Sur service injoignable ou refus métier, pose le motif dans la modal
    /// (<c>_motifEchecEnfant</c>) et renvoie <c>false</c> — la modal reste ouverte, aucun tableau relu.</summary>
    private async Task<bool> PosterEnfant<TRequete>(string route, TRequete requete)
    {
        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(route, requete);
        }
        catch (HttpRequestException)
        {
            _motifEchecEnfant = MessagesEcriture.ServiceInjoignable;
            return false;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecEnfant = await reponse.Content.ReadFromJsonAsync<string>();
            return false;
        }

        return true;
    }

    /// <summary>Compte utilisateur associé à un acteur (résolu sur son id stable), ou <c>null</c> s'il n'en
    /// porte aucun. Un acteur porte au plus un compte (association 1-1, Sc.3).</summary>
    private CompteFoyer? CompteDe(string acteurId)
        => _comptes.FirstOrDefault(c => c.ActeurId == acteurId);

    /// <summary>Vrai si l'acteur (résolu sur son id stable) est admin du foyer (énuméré depuis le store),
    /// pour marquer sa ligne. Suit une désignation aboutie ailleurs sans rechargement (Sc.9).</summary>
    private bool EstAdmin(string acteurId) => _admins.Contains(acteurId);

    /// <summary>Email courant du champ de création de compte d'une ligne d'acteur (tampon saisi, sinon vide).</summary>
    private string EmailCompte(string acteurId)
        => _emailCompte.TryGetValue(acteurId, out var email) ? email : "";

    /// <summary>Mémorise l'email saisi pour la création de compte d'un acteur (clé = id stable, jamais éditable).</summary>
    private void SaisirEmailCompte(string acteurId, string? email)
        => _emailCompte[acteurId] = email ?? "";

    /// <summary>Motif d'échec de création de compte d'une ligne d'acteur, ou <c>null</c> (aucun échec en cours).</summary>
    private string? MotifEchecCompte(string acteurId)
        => _motifEchecCompte.TryGetValue(acteurId, out var m) ? m : null;

    /// <summary>
    /// Crée / associe un compte à un acteur via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/creer-compte</c>, règle 27 — aucune vue n'écrit le domaine en direct), puis
    /// ré-énumère les comptes pour que le compte associé apparaisse <b>sans rechargement</b>, avec son
    /// statut « inactif » (Sc.7). Le front n'émet que l'acteur et l'email ; l'id stable neuf et le statut
    /// sont posés côté handler. Sur refus métier (email vide / doublon, Sc.2) ou <b>service injoignable</b>
    /// (échec de transport, règle 28), le motif est surfacé DANS la ligne, le formulaire reste ouvert et
    /// la saisie conservée, aucun compte créé.
    /// </summary>
    private async Task CreerCompte(string acteurId)
    {
        _motifEchecCompte.Remove(acteurId);

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/creer-compte",
                new CreerCompteRequete(acteurId, EmailCompte(acteurId)));
        }
        catch (HttpRequestException)
        {
            _motifEchecCompte[acteurId] = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecCompte[acteurId] = await reponse.Content.ReadFromJsonAsync<string>() ?? "Échec de la création du compte.";
            return;
        }

        await RechargerComptes();
        _emailCompte.Remove(acteurId);
    }

    /// <summary>Accusé non bloquant d'activation de compte (registre avertissement-à-part, aligné « Acteur
    /// supprimé » — D5) : affiché sans interrompre la consultation, effacé à l'activation suivante.</summary>
    private string? _accuseActivation;

    /// <summary>Vrai si le compte est de statut « inactif » (le statut est renvoyé en minuscules par le canal
    /// de lecture) — pilote l'actionnabilité du toggle « actif » de la modal (Sc.4). Aucune règle métier dans
    /// l'UI : simple lecture du statut projeté ; l'activation est tranchée côté handler.</summary>
    private static bool EstInactif(CompteFoyer compte)
        => string.Equals(compte.Statut, "inactif", StringComparison.OrdinalIgnoreCase);
}
