# Sprint 33 — Refonte Config foyer : Acteurs 2ᵉ incrément + harmonisation Rôles & Cycle

> **Goal G2 (tranché PO)** : 2ᵉ incrément vertical de l'épic **Refonte de la Configuration du
> foyer** (brief `docs/briefs/refonte-configuration-foyer.md`). **Deux volets en un sprint** (goals
> candidats 2 + 3 du `/planning` s33, validés PO ; goal 4 « supprimer un slot récurrent depuis
> l'IHM » **reporté s34**) :
> - **(A) Acteurs enrichis** : l'état **actif/admin** passe de pastille lecture seule à **TOGGLE
>   éditable DANS la modal** ; **champ neuf « adresse de résidence »** (modèle + persistance + modal) ;
>   **palette couleur en picker minimal** dans la modal (solde la dette « set couleurs par défaut »).
> - **(B) Harmonisation Rôles & Cycle** sur le patron **tableau lecture seule + crayon → modal**
>   déjà rodé s32 ; l'onglet **Cycle** rend **visibles les cycles déclarés** qui n'apparaissaient pas
>   (retour PO gate s32).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (adresse acteur + toute frontière
> Application manquante pour éditer un rôle / lire tous les cycles), puis @ihm Acteurs, puis @ihm
> Rôles, puis @ihm Cycle.
>
> **HORS scope (périmètre resserré, décision SM)** :
> - **Palette = picker minimal** réutilisant le set de couleurs — **PAS** de gestion de palette
>   custom (créer/renommer/supprimer des couleurs). Premier candidat à **couper/reporter** si le
>   sprint déborde.
> - **Cycle : édition avancée HORS scope** (ancre/début explicite, frontière de jour, plage
>   début/fin, sur-cycle vacances, WE-only — cf. « cycle de fond riche », backlog +5). Ici : a minima
>   **tableau des cycles settés/actifs** + **visibilité des cycles déclarés** + modal d'édition des
>   champs déjà éditables.
> - **Enfants / Activités / lien enfant↔parent** : autres incréments de l'épic, **non traités ici**.
> - **Occurrence-unique vs série** (goal 4) : reporté s34.

## Avancement — 4/11

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | Adresse de résidence de l'acteur — modèle + persistance + édition (frontière Application) | back | ✅ |
| 2 | Éditer / ajouter un rôle à la frontière Application (harmonisation Rôles) | back | ✅ déjà couvert s21 (filet non-régression) |
| 3 | Lire TOUS les cycles déclarés/actifs du foyer (corrige le trou de lecture) | back | ✅ |
| 4 | Toggle actif/admin DANS la modal acteur (remplace la pastille lecture) | 🖥️ IHM | ✅ |
| 5 | Champ adresse éditable dans la modal + rendu dans le tableau lecture | 🖥️ IHM | ⏳ |
| 6 | Palette couleur en picker minimal dans la modal | 🖥️ IHM | ⏳ |
| 7 | Invariants Acteurs — refus→modal ouverte (adresse/toggle) + Parent-gated + SignalR | 🖥️ IHM | ⏳ |
| 8 | Onglet Rôles au patron tableau lecture + crayon → modal + « Ajouter » | 🖥️ IHM | ⏳ |
| 9 | Invariants Rôles — refus→modal ouverte + Parent-gated + SignalR | 🖥️ IHM | ⏳ |
| 10 | Onglet Cycle — tableau des cycles + cycles déclarés RENDUS VISIBLES + modal | 🖥️ IHM | ⏳ |
| 11 | Invariants Cycle — Parent-gated + SignalR (+ refus→modal si édition) | 🖥️ IHM | ⏳ |

> **⚠️ GARDE lot atomique de surface (Rôles & Cycle) — rappel s32.** Si un onglet (Rôles ou Cycle)
> porte AUJOURD'HUI une surface d'écriture **inline**, alors **retirer l'inline** et **brancher la
> modal** sont les deux faces d'un **même refactor** : ils partagent les commandes et les testids
> d'écriture, et les fichiers d'acceptation migrent **dans le même commit**. Sc.8 (Rôles) et Sc.10
> (Cycle) sont dans ce cas **un lot atomique « swap de surface » = un seul commit** (chaque assertion
> restant individuelle), pour ne pas ouvrir de **fenêtre rouge multi-scénarios**. Si l'onglet n'a
> **pas** d'inline préexistant (surface neuve), c'est un **incrément propre** classique. **Interdit** :
> coexistence durable inline+modal (code mort + démolition contredisant le Gherkin). *(récit :
> JOURNAL-METHODE s32.)*

> **@back réels attendus (Sc.1-3).** Contrairement à s32 (pur @ihm), ce sprint introduit une
> **frontière Application neuve** : le champ **adresse** est un **champ de modèle neuf** (Domaine +
> port de persistance + handler d'édition étendu). Sc.2 (éditer/ajouter un rôle) et Sc.3 (lire tous
> les cycles déclarés) ne sont @back **que si** la frontière ne les porte pas déjà : en cas
> d'**early-green** (capacité déjà livrée s21/cycle de fond), la dev-team **le signale** → le SM
> tranche (doublon supprimé / filet de non-régression conservé), **sans** réinventer un handler.

---

## Scénarios

### Sc.1 — Adresse de résidence de l'acteur (frontière Application) @back @vert
```gherkin
Étant donné un acteur déclaré dans le foyer (id stable, nom, couleur, rôle)
Quand la commande d'édition d'acteur est émise avec une « adresse de résidence » renseignée
Alors l'adresse est portée par le modèle d'acteur et PERSISTÉE (store réel, durable au rechargement)
Et l'adresse est relue telle quelle par la query qui alimente la configuration
Et l'identifiant stable reste inchangé (édition, pas recréation)
Et une adresse VIDE est acceptée (champ optionnel) sans écriture partielle des autres champs
```

### Sc.2 — Éditer / ajouter un rôle à la frontière Application @back @vert
> **Early-green (s21) — option A retenue par le SM.** La capacité complète existe déjà à la frontière
> Application : ajout (`CreerRoleHandler` — id stable neuf opaque, refus vide/doublon, persiste) et
> édition (`RenommerRoleHandler` — id stable inchangé donc renommage ≠ recréation, refus vide/doublon
> en s'excluant lui-même). Relecture par `IEnumerationRoles.EnumererRoles()`. Aucun handler neuf.
> Filet de non-régression (preuve, conservé) :
> `tests/PlanningDeGarde.Tests/Scenario1_CreerRole.cs`, `Scenario2_RenommerRole.cs`,
> `Scenario3_RejetLibelleVideOuDoublon.cs`, `Scenario7_DeuxLibellesIdentiquesIdsDistincts.cs` ;
> durabilité store Mongo réel : `tests/PlanningDeGarde.Api.Tests/ReferentielRolesMongoDurabiliteTests.cs`,
> `RenommerRoleMongoDurabiliteTests.cs`. Seul l'invariant « affectation acteur→rôle cohérente après
> renommage » n'était asserté nulle part de façon combinée → **un** test @back ciblé ajouté :
> `tests/PlanningDeGarde.Tests/Scenario33_S2_AffectationSurvitRenommageRole.cs`.
```gherkin
Étant donné le référentiel de rôles du foyer (rôles déclarés, s21)
Quand la commande d'édition d'un rôle (libellé) OU d'ajout d'un rôle est émise
Alors le rôle est créé / renommé et PERSISTÉ, relu par la query du référentiel de rôles
Et un libellé vide ou en doublon est REFUSÉ par le domaine, sans écriture
Et l'affectation existante des acteurs au rôle reste cohérente (renommage ≠ recréation)
Et si cette capacité existe déjà (early-green), la dev-team le signale au SM (pas de handler neuf réinventé)
```

### Sc.3 — Lire TOUS les cycles déclarés / actifs du foyer @back @vert
```gherkin
Étant donné un foyer où plusieurs cycles sont DÉCLARÉS (settés / actifs)
Quand la query de configuration du foyer est interrogée sur les cycles
Alors elle renvoie l'INTÉGRALITÉ des cycles déclarés / actifs (corrige le trou : des cycles
  déclarés n'apparaissaient pas dans la config — retour PO gate s32)
Et chaque cycle est identifié de façon stable, avec ses attributs déjà persistés
Et un foyer sans cycle déclaré renvoie une liste vide (pas d'erreur)
```

### Sc.4 — Toggle actif/admin DANS la modal acteur @ihm @vert
> **Cadrage SM (décision s33).** Sens UNIQUE ce sprint. Le domaine n'offre que `designer-admin`
> et `activer-compte` (montantes) ; **aucune commande inverse** (dé-désignation / désactivation).
> Un OFF « no-op silencieux » serait un **vert-qui-ment** (toggle bascule à l'écran, Enregistrer
> « réussit », rien ne change) → **PROSCRIT**. On promet donc **seulement le sens ON** : un toggle
> déjà ON est **verrouillé** (pas de bascule OFF actionnable). Les commandes inverses partent au
> **backlog**. **Lot atomique de surface (s32)** : ce Sc.4 **remplace** les boutons d'action
> immédiats (`bouton-designer-admin`, `bouton-activer-compte`) par des toggles-sur-Enregistrer →
> les ~4 tests runtime *TempsReel* qui cliquent ces boutons **migrent dans le MÊME commit** (pas de
> coexistence boutons+toggles, pas de fenêtre rouge multi-scénarios). Le toggle « actif » cible un
> `CompteId` : **actionnable seulement si l'acteur porte un compte**.
```gherkin
Étant donné la modal d'édition d'un acteur ouverte (Parent), issue du crayon (patron s32)
Quand la modal est rendue
Alors l'état « actif » et l'état « admin » sont matérialisés en TOGGLES DANS la modal, pré-réglés
  sur l'état COURANT (et non plus en pastille lecture seule, ni en boutons d'action immédiats)
Et le toggle « actif » n'est actionnable que si l'acteur porte un COMPTE (sinon désactivé, motif dedans)
Et un toggle DÉJÀ ON est rendu VERROUILLÉ (pas de sens inverse ce sprint — dé-désignation /
  désactivation absentes du domaine, reportées backlog ; AUCUNE bascule OFF no-op silencieuse)
Quand je bascule un toggle de OFF vers ON puis clique « Enregistrer »
Alors la commande EXISTANTE de désignation d'admin / d'activation de compte est émise via le canal HTTP
Et en succès la modal se ferme et le tableau relu reflète le nouvel état (pastille lecture à jour)
Et l'identifiant stable reste inchangé
```

### Sc.5 — Champ adresse éditable dans la modal + rendu tableau @ihm @pending
```gherkin
Étant donné la modal d'édition (ou d'ajout) d'un acteur ouverte (Parent)
Quand la modal est rendue
Alors un champ « adresse de résidence » pré-rempli avec la valeur courante est éditable
Quand je saisis / modifie l'adresse puis clique « Enregistrer »
Alors la commande d'édition porte l'adresse (Sc.1) et en succès la modal se ferme
Et le tableau en lecture seule affiche l'adresse de l'acteur (colonne ou ligne dédiée)
Et une adresse laissée vide est acceptée sans bloquer l'enregistrement des autres champs
```

### Sc.6 — Palette couleur en picker minimal @ihm @pending
```gherkin
Étant donné la modal d'édition (ou d'ajout) d'un acteur ouverte (Parent)
Quand j'ouvre le sélecteur de couleur
Alors une PALETTE de couleurs (picker minimal, choix dans le set de couleurs) est proposée,
  la couleur courante de l'acteur pré-sélectionnée
Quand je choisis une couleur puis « Enregistrer »
Alors la couleur choisie est persistée via la commande EXISTANTE et la grille de planning partagée
  suit la nouvelle couleur sans recharger la page
Et HORS scope : aucune gestion de palette custom (créer / renommer / supprimer des couleurs)
```

### Sc.7 — Invariants Acteurs : refus→modal ouverte + gating + SignalR @ihm @pending
```gherkin
Étant donné la modal acteur enrichie (toggle actif/admin, adresse, palette)
Quand une valeur est refusée par le domaine (ex. nom vide / doublon, ou API injoignable) à l'enregistrement
Alors la modal RESTE OUVERTE, le motif est affiché DEDANS, ma saisie (dont adresse/toggle/couleur) est CONSERVÉE
Et le tableau et la grille restent INCHANGÉS (aucune écriture partielle)
Étant donné une identité EFFECTIVE non-Parent (Invité)
Alors le tableau reste en lecture seule, sans crayon ni « Ajouter », toggles admin/actif inatteignables
Étant donné deux écrans /configuration ouverts sur l'onglet « Acteurs »
Quand un acteur est édité (toggle / adresse / couleur) depuis le 1ᵉʳ écran
Alors le tableau du 2ᵉ écran CONVERGE sur les champs neufs sans rechargement, sans écriture par la diffusion
```

### Sc.8 — Onglet Rôles au patron tableau + crayon → modal @ihm @pending
```gherkin
Étant donné l'onglet « Rôles » de /configuration, connecté en tant que Parent
Quand la page est rendue
Alors chaque rôle apparaît sur UNE ligne en LECTURE SEULE, avec une colonne « Actions » portant un CRAYON
Et un bouton « Ajouter un rôle » est présent
Et toute surface d'édition INLINE préexistante des rôles n'est PLUS rendue (lot atomique de surface)
Quand je clique le crayon d'un rôle
Alors une MODAL pré-remplie s'ouvre ; « Enregistrer » émet la commande d'édition rôle EXISTANTE (Sc.2),
  la modal se ferme, le tableau est relu
Quand je clique « Ajouter un rôle »
Alors la MÊME modal s'ouvre VIDE (mode création) → « Enregistrer » crée un rôle avec un id stable neuf
```

### Sc.9 — Invariants Rôles : refus→modal ouverte + gating + SignalR @ihm @pending
```gherkin
Étant donné la modal d'édition (ou d'ajout) d'un rôle ouverte (Parent)
Quand j'enregistre un libellé refusé par le domaine (vide / doublon) ou que l'API est injoignable
Alors la modal RESTE OUVERTE, le motif est affiché DEDANS, ma saisie est CONSERVÉE, le tableau INCHANGÉ
Étant donné une identité EFFECTIVE non-Parent (Invité)
Alors l'onglet Rôles reste en lecture seule, sans crayon ni « Ajouter », aucune modal atteignable
Étant donné deux écrans ouverts sur l'onglet « Rôles »
Quand un rôle est édité / ajouté depuis le 1ᵉʳ écran
Alors le tableau du 2ᵉ écran CONVERGE sans rechargement, sans écriture par la diffusion
```

### Sc.10 — Onglet Cycle : tableau + cycles déclarés visibles + modal @ihm @pending
```gherkin
Étant donné l'onglet « Cycle » de /configuration, connecté en tant que Parent
Et un foyer où plusieurs cycles sont déclarés (dont des cycles qui n'apparaissaient PAS avant)
Quand la page est rendue
Alors un TABLEAU en lecture seule liste TOUS les cycles settés / actifs du foyer (Sc.3),
  y compris ceux qui étaient auparavant invisibles dans la config (retour PO gate s32)
Et une colonne « Actions » porte un CRAYON par cycle ; toute surface inline préexistante n'est PLUS rendue
Quand je clique le crayon d'un cycle
Alors une MODAL pré-remplie s'ouvre, éditant les champs DÉJÀ éditables du cycle
Et « Enregistrer » persiste via la commande existante, la modal se ferme, le tableau est relu
Et HORS scope : édition avancée du cycle (ancre / frontière de jour / plage / sur-cycle vacances / WE-only)
```

### Sc.11 — Invariants Cycle : gating + SignalR (+ refus→modal) @ihm @pending
```gherkin
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'ouvre l'onglet « Cycle »
Alors le tableau des cycles reste visible en LECTURE SEULE, sans crayon, aucune modal atteignable
Étant donné la modal d'édition d'un cycle ouverte (Parent)
Quand j'enregistre une valeur refusée par le domaine ou que l'API est injoignable
Alors la modal RESTE OUVERTE, le motif est affiché DEDANS, la saisie CONSERVÉE, le tableau INCHANGÉ
Étant donné deux écrans ouverts sur l'onglet « Cycle »
Quand un cycle est édité depuis le 1ᵉʳ écran
Alors le tableau du 2ᵉ écran CONVERGE sans rechargement, sans écriture par la diffusion
```

---

# Retours produit (PO)

_(À remplir après le gate G3.)_
