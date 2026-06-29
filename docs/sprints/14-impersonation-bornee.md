# Plan Gherkin — Sprint 14 · `impersonation-bornee` (palier 8, tranche 2, épic É10)

> Sujet : **Incarner (lecture seule)**. L'utilisateur principal (Parent
> configurateur) **incarne un acteur déjà déclaré** du foyer — convenance
> d'administration. La vue reflète le **rôle de l'acteur incarné** ; le retour à
> l'identité réelle restaure l'état. **Pas d'écriture « au nom de »**, **zéro
> persistance neuve**, **borne dure** : ce n'est PAS l'authentification réelle du
> palier 16 (ni OAuth, ni comptes, ni sessions, ni landing, ni prise en main).

## Analyse technique

Incrément à dominante **front / session** (adaptateur de gauche `Web`), sans
règle de résolution neuve côté domaine.

- **Couches & dépendances.** Étendre l'état de session `SessionPlanning`
  (aujourd'hui `Role` + `EnfantId`, scoped par circuit Blazor) d'une distinction
  **identité réelle** (fixe) vs **identité effective** (incarnée, ou repli sur la
  réelle), avec `Incarner(acteurId)` / `RevenirIdentiteReelle()`. `EstParent`
  dérive désormais de l'**identité effective**. Le gating règle 9 sur la grille
  `PlanningPartage` **et** sur l'écran `ConfigurationFoyer` lit cette identité
  effective. Le sélecteur de rôle manuel actuel (dropdown démo Parent/Invité) est
  remplacé / complété par un **sélecteur d'incarnation** alimenté par le
  référentiel d'acteurs.
- **CQRS — read vs write.** **READ** uniquement côté domaine : la liste des
  acteurs incarnables et la résolution de l'identité effective lisent le
  **référentiel d'acteurs persistés** (config foyer Mongo, palier 5) sur
  l'**identifiant stable** (`acteur-…`), jamais le libellé (règles 5/19) — réutilise
  le contrat d'énumération existant, **aucun port neuf**. **WRITE inchangé** :
  **aucun handler ni commande neuf**, les commandes (`PoserSlot`,
  `AffecterPeriode`, `DefinirTransfert`, `SupprimerActeur`) restent émises sous
  l'**identité réelle** (auteur inchangé) ; l'impersonation **ne touche pas** le
  canal requête/réponse.
- **Mapping type acteur → vue** (règle 8, non rouvert) : **Admin / Parent** →
  menu d'écriture clic-case **visible** ; **Autre** → menu **masqué**
  (consultation seule).
- **Concurrence & temps réel.** La diffusion SignalR lecture seule de la
  **suppression d'acteur** (déjà livrée, règle 6) déclenche un **retour
  automatique à l'identité réelle** si l'acteur supprimé est l'identité effective.
  Cas validé en **acceptation runtime / G3** (app câblée), **pas** en filet de
  régression automatisé instable (cf. flakes SignalR P2).
- **Borne anti-cliquet (règle 30).** État **mémoire / session** uniquement,
  **zéro persistance neuve** ; rien ne subsiste après redémarrage.
- **Lot IHM final** (`ihm-builder`, app câblée — front WASM + API distante +
  SignalR + Mongo réel) : bandeau « Vous incarnez X », sélecteur d'incarnation,
  gating effectif grille + config, et concurrence temps réel (Sc.5). Un scénario
  `🖥️` n'est jamais prouvé par bUnit seul.

**Ordre & nature.** Cœur d'abord (drivers identité effective Sc.1→Sc.4),
concurrence en cas limite (Sc.5), **durcissement gating config en fin (Sc.6,
CUTTABLE ≤ ~2h)**. Drivers réels : Sc.1, Sc.2, Sc.3, Sc.5, Sc.6 ; caractérisation
(filet, early-green attendu) : Sc.4.

**Couverture.** Règle 8 (impersonation) : nominal Sc.1, limite Sc.2 + Sc.5,
erreur Sc.3. Règle 9 (gating par identité effective) : nominal Sc.1, limite Sc.4,
erreur Sc.6.

## Scénarios

**Feature: Incarner un acteur déjà déclaré (lecture seule)** — En tant que Parent
configurateur du foyer, je veux incarner un acteur déjà déclaré pour voir le
planning « comme » lui (convenance d'administration), afin de vérifier ce qu'il
perçoit, sans m'authentifier réellement, sans écrire en son nom et sans rien
persister. La vue reflète le rôle de l'acteur incarné ; je peux revenir à mon
identité réelle à tout moment.

### Scenario 1 — Incarner un acteur déjà déclaré : bandeau + vue selon le rôle de l'incarné

`@nominal` · driver

```gherkin
Scenario Outline: Incarner un acteur déclaré reflète son rôle dans la vue
  Étant donné que je suis le Parent configurateur sous mon identité réelle
  Et que le foyer déclare "Bruno" (Parent), "Nina la nounou" (Autre) et "Carla" (Admin)
  Quand j'incarne l'acteur "<acteur>"
  Alors un bandeau affiche "Vous incarnez <acteur>"
  Et le menu d'actions au clic sur une case du planning est "<menu>"

  Examples:
    | acteur          | menu    |
    | Bruno           | visible |
    | Nina la nounou  | masqué  |
    | Carla           | visible |
```

### Scenario 2 — Retour à l'identité réelle : bandeau retiré, état restauré

`@nominal` · driver

```gherkin
Scenario: Revenir à mon identité réelle restaure ma vue
  Étant donné que je suis le Parent configurateur sous mon identité réelle
  Et que le foyer déclare "Bruno" (Parent)
  Et que j'incarne "Bruno", affichant le bandeau "Vous incarnez Bruno"
  Quand je reviens à mon identité réelle
  Alors le bandeau "Vous incarnez Bruno" n'est plus affiché
  Et le menu d'actions au clic sur une case est de nouveau celui de mon identité réelle (visible)
```

### Scenario 3 — Incarner un identifiant d'acteur inconnu : refus, identité réelle conservée

`@erreur` · driver

```gherkin
Scenario: Incarner un acteur absent du référentiel ne change rien
  Étant donné que je suis le Parent configurateur sous mon identité réelle
  Et que le référentiel du foyer ne contient aucun acteur d'identifiant "acteur-inexistant"
  Quand je tente d'incarner l'acteur d'identifiant "acteur-inexistant"
  Alors aucun bandeau "Vous incarnez" n'est affiché
  Et je reste sous mon identité réelle
  Et le menu d'actions au clic sur une case est inchangé (visible)
```

### Scenario 4 — Pas d'écriture « au nom de » : l'écriture aboutit sous l'identité réelle

`@limite` · caractérisation (early-green attendu)

```gherkin
Scenario: Écrire en incarnant un Parent part sous mon identité réelle
  Étant donné que je suis le Parent configurateur sous mon identité réelle
  Et que le foyer déclare "Bruno" (Parent)
  Et que j'incarne "Bruno", ce qui rend le menu d'actions visible
  Quand je pose un slot le 16/06 depuis la dialog ouverte sur cette case
  Alors le slot est enregistré
  Et la commande de pose part sous mon identité réelle, et non sous "Bruno"
```

### Scenario 5 — Concurrence : l'acteur incarné est supprimé → retour automatique à l'identité réelle

`@limite` · driver · 🖥️ acceptation runtime / G3 (touche la diffusion temps réel)

```gherkin
Scenario: La suppression concurrente de l'acteur incarné me ramène à mon identité réelle
  Étant donné que je suis le Parent configurateur et que j'incarne "Nina la nounou"
  Et que le bandeau "Vous incarnez Nina la nounou" est affiché
  Quand un autre écran supprime l'acteur "Nina la nounou" du foyer
  Et que la suppression se propage en temps réel à mon écran
  Alors je reviens automatiquement à mon identité réelle
  Et le bandeau "Vous incarnez Nina la nounou" n'est plus affiché
  Et aucun nom fantôme de "Nina la nounou" ne subsiste dans la vue
```

### Scenario 6 — Durcissement gating config : un acteur « Autre » incarné masque toutes les écritures config

`@erreur` · driver · CUTTABLE (≤ ~2h, sinon coupé et re-séquencé sans toucher au cœur)

```gherkin
Scenario: Incarner un acteur "Autre" masque toutes les écritures de l'écran de configuration
  Étant donné que je suis le Parent configurateur sous mon identité réelle
  Et que le foyer déclare "Nina la nounou" (Autre)
  Et que j'incarne "Nina la nounou"
  Quand j'ouvre l'écran de configuration du foyer
  Alors l'ajout d'un acteur n'est pas proposé
  Et l'édition d'un acteur n'est pas proposée
  Et l'édition du cycle de fond n'est pas proposée
  Et le bouton de suppression d'un acteur n'est pas proposé
```
