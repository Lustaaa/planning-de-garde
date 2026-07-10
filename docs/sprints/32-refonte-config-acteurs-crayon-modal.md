# Sprint 32 — Refonte Config foyer : Acteurs en tableau lecture seule + crayon → modal

> **Goal G2 (tranché PO)** : 1er incrément vertical de l'épic **Refonte de la Configuration du
> foyer** (brief `docs/briefs/refonte-configuration-foyer.md`). Remplacer l'édition **INLINE**
> actuelle des acteurs (2 cartes « Modifier » / « Ajouter » + contrôles inline dans la table) par
> le patron **tableau lecture seule + crayon → modal**. **Réutilise les commandes CRUD acteurs
> déjà livrées** (`AjouterActeur`, `EditerActeur`, `AffecterRole`… + compte/admin existants) —
> **aucun handler ni query neuf**. Gating Parent + temps réel SignalR **préservés**, cohérent avec
> le patron dialogs s11-s12.
>
> **HORS scope (2ᵉ incrément)** : champs **neufs** (adresse de résidence, palette couleur picker),
> **état en toggle** actif/admin *dans* la modal, et harmonisation **Rôles / Cycle / Enfants**.
> Ici : les états actif/admin restent **affichés en pastille lecture** dans le tableau ; la modal
> édite uniquement les **champs existants** (nom, couleur, rôle ; email/compte via l'affordance
> compte existante).

## Avancement — 4/7

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | Tableau des acteurs en lecture seule + colonne crayon (inline retiré) | 🖥️ IHM | ✅ |
| 2 | Crayon → modal pré-remplie avec les champs courants de l'acteur | 🖥️ IHM | ✅ |
| 3 | Édition via la modal → enregistrer (CRUD existant) → table relue, modal fermée | 🖥️ IHM | ✅ |
| 4 | Bouton « Ajouter » → même modal VIDE → création (id stable neuf) | 🖥️ IHM | ✅ |
| 5 | Erreur — refus domaine → modal reste ouverte, motif dedans, saisie conservée | 🖥️ IHM | ⏳ |
| 6 | Gating — Invité (non-Parent) : ni crayon ni « Ajouter », table lecture seule | 🖥️ IHM | ⏳ |
| 7 | Temps réel SignalR — un 2ᵉ écran édite/ajoute → table converge sans reload | 🖥️ IHM | ⏳ |

> **⚠️ Lot atomique Sc.1→Sc.4 = UN seul commit « swap de surface » (décision SM).** Retirer la
> surface d'écriture **inline** (Sc.1) et brancher la surface d'écriture **par la modal** (Sc.2-4)
> sont les deux faces d'un **même refactor de surface** : ils partagent les mêmes commandes CRUD et
> les mêmes testids d'écriture. Les ~34 fichiers d'acceptation runtime qui pilotent l'écriture par
> l'inline sont **migrés vers le parcours modal dans ce même commit**. La suite complète ne peut
> **pas** être verte sur un découpage partiel (retirer l'inline avant que la modal ne porte
> l'écriture = fenêtre rouge multi-scénarios, proscrite). Les 4 lignes basculent donc **✅ ensemble**
> — chaque scénario reste **individuellement asserté** (bUnit + acceptation runtime) dans ce commit.
> **Sc.5 (erreur), Sc.6 (gating), Sc.7 (SignalR)** suivent en **incréments propres** (1 commit vert
> chacun). Interdit : coexistence durable inline+modal (code mort + étape de démolition qui
> contredirait le Gherkin de Sc.1).

> **Sprint pur @ihm (0 @back).** Le goal est une **refonte de surface** : il **réutilise** les
> commandes/handlers CRUD acteurs, le référentiel de rôles, la création de compte et la désignation
> d'admin **déjà livrés** (s05→s24), ainsi que les données déjà lues côté client (email/statut du
> compte, rôle, marqueur admin). **Aucune frontière Application neuve**, donc aucun scénario `@back`.
> Tous les scénarios sont menés **RED→GREEN runtime** (bUnit + acceptation runtime), pas de doublure.

---

## Scénarios

### Sc.1 — Tableau des acteurs en lecture seule + colonne crayon @ihm @vert
```gherkin
Étant donné un foyer avec plusieurs acteurs déclarés (nom, couleur, rôle affecté, compte, admin)
Et que je suis connecté en tant que Parent, sur l'onglet « Acteurs » de /configuration
Quand la page est rendue
Alors chaque acteur apparaît sur UNE ligne en LECTURE SEULE : pastille de couleur + nom,
  email + statut du compte, rôle, et l'état (actif/admin) matérialisé en PASTILLE / badge
Et les deux cartes d'édition INLINE « Modifier un acteur » et « Ajouter un acteur » ne sont PLUS rendues
Et les contrôles d'édition inline de la table (sélecteur de rôle, champs email, boutons inline) sont retirés de la lecture
Et une colonne « Actions » porte, par ligne, un CRAYON d'édition
Et un bouton « Ajouter un acteur » est présent au bas du tableau
```

### Sc.2 — Crayon → modal pré-remplie avec les champs courants @ihm @vert
```gherkin
Étant donné le tableau des acteurs en lecture seule (Parent)
Quand je clique sur le crayon d'un acteur
Alors une MODAL d'édition s'ouvre, pré-remplie avec les champs COURANTS de cet acteur :
  nom, couleur, rôle (borné au référentiel : exactement les rôles du foyer + « sans rôle »)
Et l'identifiant stable de l'acteur est porté par la modal SANS être éditable (jamais dérivé du libellé)
Et fermer la modal (annuler) n'émet AUCUNE commande et laisse le tableau inchangé
```

### Sc.3 — Édition via la modal → enregistrer → table relue, modal fermée @ihm @vert
```gherkin
Étant donné la modal d'édition ouverte sur un acteur (Parent)
Quand je modifie son nom et/ou sa couleur et/ou son rôle puis clique « Enregistrer »
Alors les commandes CRUD acteurs EXISTANTES sont émises via le canal HTTP (aucun handler neuf)
Et en succès la modal se ferme, le tableau est relu et reflète le changement
Et la grille de planning partagée suit la nouvelle couleur / le nouveau nom sans recharger la page
Et l'identifiant stable reste inchangé (renommage, pas recréation)
```

### Sc.4 — Bouton « Ajouter » → même modal VIDE → création @ihm @vert
```gherkin
Étant donné le tableau des acteurs (Parent)
Quand je clique sur « Ajouter un acteur »
Alors la MÊME modal s'ouvre avec tous les champs VIDES (mode création, pas d'acteur pré-sélectionné)
Quand je saisis un nom (et éventuellement une couleur) puis « Enregistrer »
Alors la commande d'ajout EXISTANTE crée un acteur avec un identifiant stable NEUF (jamais le libellé)
Et le nouvel acteur apparaît aussitôt dans le tableau (relecture du store) sans recharger la page
```

### Sc.5 — Erreur : refus domaine → modal reste ouverte, motif dedans @ihm @pending
```gherkin
Étant donné la modal d'édition (ou d'ajout) ouverte (Parent)
Quand je tente d'enregistrer une valeur refusée par le domaine (ex. nom vide, ou doublon de nom)
  ou que l'API est injoignable
Alors la modal RESTE OUVERTE
Et le motif d'échec est affiché DANS la modal
Et ma saisie est CONSERVÉE
Et le tableau et la grille restent INCHANGÉS (aucune écriture partielle)
```

### Sc.6 — Gating : Invité (non-Parent) → ni crayon ni « Ajouter » @ihm @pending
```gherkin
Étant donné une identité EFFECTIVE non-Parent (Invité, ou incarnation d'un acteur « Autre »)
Quand j'ouvre l'onglet « Acteurs » de /configuration
Alors le tableau des acteurs reste visible en LECTURE SEULE (consultation préservée)
Mais AUCUN crayon d'édition n'est rendu
Et AUCUN bouton « Ajouter un acteur » n'est rendu
Et aucune modal d'écriture n'est atteignable
Et le gating par onglet (durcissement config s14/s20) est tenu, non régressé sur les autres onglets
```

### Sc.7 — Temps réel SignalR : convergence sans rechargement @ihm @pending
```gherkin
Étant donné deux écrans /configuration ouverts sur l'onglet « Acteurs »
Quand un acteur est édité (nom/couleur/rôle) ou ajouté depuis le 1ᵉʳ écran via la modal
Alors le tableau du 2ᵉ écran CONVERGE (ligne mise à jour ou ajoutée) SANS rechargement
Et le temps réel de lecture s20 (hub SignalR) reste préservé, sans écriture par la diffusion
```

---

# Retours produit (PO)

<!-- Rempli au gate G3 / à la clôture. -->
