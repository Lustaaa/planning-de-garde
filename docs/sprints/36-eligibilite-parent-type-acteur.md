# Sprint 36 — Éligibilité « parent » du lien enfant↔parent : flag « rôle parent » (option B1+B2)

> **Goal G2 (re-tranché PO — option A REJETÉE au gate, remplacée par B1+B2)**. L'option A livrée
> initialement (éligibilité = `TypeActeur.Parent`) est **KO par construction** : le `TypeActeur`
> n'est **jamais saisi via l'IHM** (`AjouterActeurHandler` ne passe aucun type ; `TypeParDefaut =
> Parent`), donc **tout acteur créé par l'utilisateur est `Parent`** → Valérie (nounou) et Mamie
> (grand-parent) apparaissaient liables (retour PO gate). Le PO raisonne en **RÔLES** : Papa / Maman /
> Parent = rôles-parents (liables) ; Nounou / Grand-parent = non.
>
> **Mécanisme retenu (G2, PO)** :
> - **B1** — chaque `RoleFoyer` porte un **flag booléen « est un rôle parent »**, pilotable par
>   l'utilisateur (coche/décoche). Le **flag** est la **source de vérité**, jamais le libellé (on ne
>   retombe PAS sur le piège du libellé littéral « Parent » rejeté en s35).
> - **B2 (défaut d'amorçage)** — à la **création du foyer (seed)**, les rôles de libellé **Papa /
>   Maman / Parent** démarrent **pré-cochés parent** ; les autres (Nounou, Grand-parent…) non.
>   L'utilisateur peut tout re-basculer ensuite.
> - **Éligibilité au lien enfant↔parent** = l'acteur **porte un rôle marqué parent** (REMPLACE la
>   garde `TypeActeur.Parent` de l'option A).
>
> **Tranche SM sur la portée du pré-cochage** : le pré-cochage B2 s'applique **au SEED initial du
> foyer UNIQUEMENT**. Un rôle **créé ensuite** via `CreerRole` démarre **non-parent** (flag à false),
> même si son libellé est « Papa/Maman/Parent » — la source de vérité reste le **flag posé
> explicitement**, jamais une reconnaissance de libellé à la volée (anti-piège s35). L'utilisateur
> coche après création si voulu.
>
> **Tranche SM « acteur sans rôle »** : un acteur **sans aucun rôle affecté** (`RoleId = null`) n'a
> **aucun rôle marqué parent** → **non liable**. Conséquence : le seed démo doit désormais **affecter
> un rôle-parent** aux acteurs-parents (Alice→Papa, Bruno→Maman marqués parent) pour rester liables
> (Sc.3 / Sc.7). C'est cohérent avec le modèle role-based voulu par le PO.
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (modèle+ports+persistance InMemory ET
> Mongo → commande de bascule → amorçage seed → bascule de l'éligibilité du handler) → puis @ihm
> (case « rôle parent » dans la modal Rôles + sélecteur parents aligné + preuve runtime seed).
>
> **HORS scope** :
> - **Champ père/mère distinct** sur l'acteur ou le lien : non traité (distinction par le NOM ;
>   backlog si demandé).
> - **Familles recomposées R2/R3** (« exactement 2 parents », graphe enfant-racine) : autre incrément.
> - **Saisie / édition du `TypeActeur` lui-même** : hors scope (le type n'est plus le pivot de
>   l'éligibilité ; il reste au seul service du gating d'écriture R8/R9, inchangé).

## Avancement — 1/7

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | `RoleFoyer` enrichi d'un flag **« est rôle parent »** (modèle + port lecture `IEnumerationRoles` + persistance **InMemory ET Mongo durable**) | back | ✅ |
| 2 | Commande/handler **`MarquerRoleParent`** : bascule le flag d'un rôle (coche/décoche), idempotent, rôle inexistant refusé sans écriture | back | ⏳ |
| 3 | **Amorçage B2** : au seed du foyer, rôles Papa/Maman/Parent **pré-cochés parent** (autres non) ; un rôle créé ensuite démarre **non-parent** ; seed démo affecte un rôle-parent aux acteurs-parents | back | ⏳ |
| 4 | **`LierEnfantParentHandler`** — éligibilité = l'acteur **porte un rôle marqué parent** (REMPLACE `TypeActeur.Parent`) ; rôle non-parent (Nounou/Grand-parent) ou sans rôle = REFUSÉ, sans écriture partielle | back | ⏳ |
| 5 | Sélecteur parents modal Enfants **`ActeursParents()`** énumère les acteurs à **rôle marqué parent** (l'IHM suit exactement la règle back) | 🖥️ IHM | ⏳ |
| 6 | **Case « rôle parent »** dans la modal Rôles (patron crayon→modal s33) : coche/décoche, Échap=Annuler, **Parent-gated**, convergence **SignalR** temps réel | 🖥️ IHM | ⏳ |
| 7 | **Preuve runtime seed** : Valérie (Nounou) et Mamie (Grand-parent) **N'apparaissent PAS** ; Alice/Bruno (Papa/Maman marqués parent) apparaissent et sont liables ; décocher « parent » les retire en temps réel | 🖥️ IHM | ⏳ |

> **⚠️ GARDE — lot atomique : REMPLACEMENT de l'option A, pas coexistence (Sc.4).** Les commits s36
> déjà poussés portent l'option A (`LierEnfantParentHandler` L53 `TypeDe==Parent`, filet Sc.2 s36,
> sélecteur `ActeursParents()` filtré sur `TypeActeur.Parent`, tests `Scenario36_S1/S2`,
> `FrontWasmConfigEnfantsSelecteurParentsTypeActeurTests`, `...SeedDemoParentsLiablesTypeActeurTests`).
> La dev-team **REMPLACE** cette logique par la logique role-flag et **migre/adapte ces tests dans le
> MÊME lot** que le changement de handler/sélecteur — **aucune fenêtre rouge** (rempart
> suite-complète-verte, récit lot atomique JOURNAL-METHODE s32). **Interdit** : laisser coexister les
> deux critères (type ET flag) — c'est une double source de vérité. Le prédicat `TypeActeur.Parent`
> ne doit **plus qualifier l'éligibilité du lien** après ce sprint (il reste au gating R8/R9 seul,
> non touché). Chaque assertion reste **individuelle** dans le lot.

> **⚠️ Non-régression gating impersonation R8/R9.** Le droit d'écriture dérive du `TypeActeur` de
> l'identité effective (R8/R9, `SessionPlanning`) : il **ne change pas**. Aucun droit d'écriture ne
> doit être gagné/perdu du fait de la bascule d'éligibilité vers le flag rôle. La suite COMPLÈTE
> reste verte (Docker actif, sans filtre).

> **Définition « parent liable » (cadrage SM, option B1+B2).** Un **parent liable** = un acteur dont
> le **rôle affecté est marqué « est rôle parent »** (flag booléen sur `RoleFoyer`, source de vérité
> unique). Un acteur **sans rôle** ou à **rôle non marqué** n'est **pas** liable. Le **libellé** de
> rôle **ne qualifie PLUS** l'éligibilité (ni « Parent » littéral, ni « Papa/Maman »). Le
> `TypeActeur` **ne qualifie PLUS** l'éligibilité (option A retirée). Borne **0..2 parents** (s34)
> **inchangée**.

---

## Scénarios

### Sc.1 — `RoleFoyer` porte un flag « est rôle parent », persisté durablement @back @vert
```gherkin
Étant donné le référentiel de rôles du foyer (agrégat de config, s21 : id stable + libellé)
Quand le modèle du rôle est enrichi d'un attribut booléen « est un rôle parent »
Alors le port de lecture (IEnumerationRoles.EnumererRoles) surface ce flag pour chaque rôle
Et l'adaptateur InMemory le porte et le restitue à l'identique
Et l'adaptateur Mongo le PERSISTE et le relit DURABLEMENT (round-trip store réel, survit au rechargement)
Et un rôle existant sans flag stocké (donnée antérieure) se relit avec « est rôle parent » = false (défaut neutre, pas de crash)
```

### Sc.2 — Commande `MarquerRoleParent` : bascule le flag @back @pending
```gherkin
Étant donné un rôle existant du référentiel dont « est rôle parent » = false
Quand la commande « marquer un rôle comme rôle parent » (roleId, estParent=true) est émise
Alors le flag du rôle passe à true et est PERSISTÉ (store réel, durable)
Et ré-émettre la même commande (estParent=true) est NEUTRE (idempotent, aucun doublon d'écriture)
Quand la commande est ré-émise avec estParent=false
Alors le flag repasse à false (décoche pilotée par l'utilisateur, source de vérité = le flag)
Étant donné un roleId INEXISTANT dans le référentiel
Quand la commande de bascule le désigne
Alors le domaine REFUSE (rôle inexistant), le motif est restitué, AUCUNE écriture ne touche le store
```

### Sc.3 — Amorçage B2 : Papa/Maman/Parent pré-cochés au seed, rôle créé ensuite non-parent @back @pending
```gherkin
Étant donné la création (seed) d'un foyer avec les rôles Papa, Maman, Parent, Nounou, Grand-parent
Alors les rôles de libellé Papa, Maman et Parent démarrent « est rôle parent » = true (pré-cochés)
Et les rôles Nounou et Grand-parent démarrent « est rôle parent » = false
Et le seed démo affecte un rôle-parent aux acteurs-parents (Alice→Papa, Bruno→Maman) et Nounou/Grand-parent aux autres
Étant donné le foyer déjà créé
Quand un NOUVEAU rôle de libellé « Parent » (ou « Papa ») est créé via CreerRole APRÈS le seed
Alors ce rôle démarre « est rôle parent » = false (le pré-cochage ne vaut QUE pour le seed initial)
Et il ne devient parent QUE par une bascule explicite (Sc.2) — jamais par reconnaissance de son libellé (anti-piège s35)
```

### Sc.4 — Éligibilité du lien basculée sur « rôle marqué parent » (REMPLACE l'option A) @back @pending
```gherkin
Étant donné un enfant déclaré dans le foyer (agrégat de 1er rang, s30)
Et un acteur portant un rôle dont « est rôle parent » = true (ex. Alice→Papa)
Quand la commande « lier un enfant à un parent » est émise (enfantId, acteurId)
Alors le lien est ACCEPTÉ et PERSISTÉ (store réel, durable au rechargement)
Et l'éligibilité est résolue sur « l'acteur porte un rôle marqué parent » — le critère TypeActeur.Parent n'intervient PLUS
Étant donné un acteur portant un rôle dont « est rôle parent » = false (ex. Valérie→Nounou, Mamie→Grand-parent)
Quand la commande « lier » le désigne comme parent
Alors le domaine REFUSE (acteur sans rôle-parent), le motif est restitué, AUCUNE écriture partielle (liens existants intacts)
Étant donné un acteur SANS aucun rôle affecté (RoleId = null)
Quand la commande « lier » le désigne
Alors le domaine REFUSE (aucun rôle marqué parent porté)
Et la borne 0..2 parents (s34) reste tenue (un 3ᵉ parent refusé, invariant inchangé)
```

### Sc.5 — Sélecteur de la modal Enfants aligné sur « rôle marqué parent » @ihm @pending
```gherkin
Étant donné la modal d'édition d'un enfant ouverte (onglet Enfants, connecté en tant que Parent)
Et le sélecteur des parents alimenté par ActeursParents() (ConfigurationFoyer.razor.cs)
Quand la modal est rendue
Alors le sélecteur énumère les acteurs dont le rôle affecté est marqué « rôle parent » (au lieu du filtre TypeActeur.Parent)
Et un acteur à rôle non marqué (Nounou, Grand-parent) ou sans rôle N'APPARAÎT PAS
Quand je lie un parent éligible puis « Enregistrer »
Alors la commande « lier » (Sc.4) est émise via le canal HTTP et le tableau reflète le parent lié
Et l'IHM suit EXACTEMENT la règle back (aucun critère d'éligibilité divergent entre back et IHM)
```

### Sc.6 — Case « rôle parent » dans la modal Rôles (crayon→modal s33) @ihm @pending
```gherkin
Étant donné l'onglet Rôles de la config (tableau lecture seule + crayon→modal, s33), connecté en tant que Parent
Quand j'ouvre la modal d'édition d'un rôle
Alors une case à cocher « rôle parent » y reflète l'état courant du flag du rôle
Quand je (dé)coche la case puis « Enregistrer »
Alors la commande MarquerRoleParent (Sc.2) est émise via le canal HTTP et le flag est persisté
Quand j'ouvre la modal puis j'appuie sur Échap
Alors la modal se ferme SANS mutation (port IEcouteurEchapModal), le flag antérieur intact
Étant donné une identité effective NON-Parent (Autre incarné, R8/R9)
Alors ni le crayon ni la bascule « rôle parent » ne sont actionnables (Parent-gated, lecture seule)
Quand un autre écran Parent bascule le flag d'un rôle
Alors la modal/tableau de rôles converge en TEMPS RÉEL (SignalR lecture seule) sans rechargement
```

### Sc.7 — Preuve runtime seed : Nounou/Grand-parent exclus, Papa/Maman liables, décoche en direct @ihm @pending
```gherkin
Étant donné l'app lancée sur le seed démo (Alice→Papa & Bruno→Maman marqués parent ; Valérie→Nounou & Mamie→Grand-parent non marqués)
Et un enfant déclaré dans le foyer
Quand j'ouvre la modal Enfants et le sélecteur des parents
Alors Alice et Bruno sont proposés comme parents liables
Et Valérie (Nounou) et Mamie (Grand-parent) N'APPARAISSENT PAS (retour PO gate corrigé)
Quand je lie Alice puis Bruno à l'enfant
Alors la colonne « Parents liés » affiche « Alice » et « Bruno » (distinction Papa/Maman par le NOM, hors scope champ dédié)
Quand, depuis l'onglet Rôles, je DÉCOCHE « rôle parent » sur le rôle Papa
Alors Alice disparaît du sélecteur de parents en TEMPS RÉEL (le flag est la source de vérité vivante)
```

---

# Retours produit (PO)
