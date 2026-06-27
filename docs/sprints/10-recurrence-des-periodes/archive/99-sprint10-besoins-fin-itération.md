# Besoins priorisés — Récurrence des périodes (sprint 10)

> Source : `99-sprint10-retours.md` (sections `# Retours produit (PO)`, `# Idée pour la suite`,
> `# Consigne pour la suite`) · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur **un** sujet prioritaire (GOAL 1 ci-dessous). Les sections
> `# Méthode (agents)`, `## IA`, `## Notes de contexte`, `# Décisions autonomes (chef de projet)`
> du fichier unifié **ne sont pas** traitées ici (elles relèvent de `retro-sprint`).
>
> **Bypass Tech confirmé** : la sous-section `## Tech` ne porte que le placeholder → aucune
> contrainte technique injectée par le PO, aucun garde-fou Tech à porter dans ce backlog.

## En tête — défaut confirmé à corriger HORS make-gherkin

> **Un seul `bug` du sprint, confronté au code courant (HEAD) et localisé.** Il part en
> **`/3` ciblé léger**, **indépendant** du sujet de sprint : à corriger avant ou en parallèle
> du make-gherkin, **sans** passer par `/2`.

- **R2 — dropdown « Acteur du foyer » périmée au renommage.**
  - **Symptôme PO** : « la dropdown "Acteur du foyer" n'est pas mise à jour quand je change le nom d'un acteur ».
  - **Défaut localisé (HEAD)** : `src/PlanningDeGarde.Web/Components/Pages/ConfigurationFoyer.razor:16-22`
    et `NomActuel` (`ConfigurationFoyer.razor.cs:77-78`) itèrent sur la liste **statique** front
    `Foyer.ActeursEditables` (`src/PlanningDeGarde.Web/Foyer.cs:39-45`, libellés figés Alice/Bruno…)
    au lieu du **store vivant** `_acteurs` (chargé via `api/foyer/acteurs`), pourtant **déjà** utilisé
    par le sélecteur de cycle (`ConfigurationFoyer.razor:114`) et la liste d'acteurs (`:138`).
    La dropdown ne relit jamais le store → nom périmé après renommage.
  - **Correctif** : repointer ces 2 lectures (dropdown + `NomActuel`) sur `_acteurs`. Cohérence
    de l'écran de config avec la grille (règle 5 « la grille relit immédiatement la config éditée »).
  - **Nature** : `/3` ciblé léger, **PAS** un make-gherkin. Pas de règle de gestion neuve.

## Classification des retours

| # | Retour (libellé court) | Source | Type | Besoin sous-jacent | Confrontation HEAD |
|---|---|---|---|---|---|
| R1 | Vue par utilisateur — remplacer la dropdown « Rôle » par un sélecteur d'acteur à incarner (impersonation) | IHM - général | nouveau besoin | Choisir l'acteur incarné pour une vue/des droits par acteur ; **amorce bornée** d'É10 (impersonation par l'utilisateur principal tant que les acteurs ne sont pas des utilisateurs réels) | `PlanningPartage.razor:14-18` + `.razor.cs:30-34` : dropdown Parent/Invité (`RoleAuteur`), pas d'impersonation. Comportement **conforme au livré** → **pas un bug** |
| R2 | Dropdown « Acteur du foyer » pas mise à jour au renommage | IHM - /configuration | **bug** | Cohérence du même écran : dropdown + « Nom actuel » doivent lire le store vivant `_acteurs` | **Défaut localisé** : `ConfigurationFoyer.razor:16-22` + `.razor.cs:77-78` lisent `Foyer.ActeursEditables` (statique, `Foyer.cs:39-45`) au lieu de `_acteurs` |
| R3 | Il manque de quoi choisir le début du cycle | IHM - /configuration | évolution | Choisir explicitement la phase / l'ancre du cycle (quelle semaine = index 0) | **Pas un défaut.** Décision CP 2026-06-27 (ancrage ISO sans ancre) ; option 2 (date d'ancrage) écartée AVEC la note « évolution séquencée si l'usage la réclame » — c'est le cas |
| R4 | Configurer finement les cycles (frontière de jour vendredi→vendredi, date début **et** fin, sur-cycle vacances, 1 WE/2) | IHM - /configuration | nouveau besoin | Cycle réellement utilisable au quotidien. **Sujet composite** : (a) frontière de jour paramétrable, (b) plage de validité début/fin, (c) sur-cycle/exception saisonnier (vacances), (d) cycle WE-only | Le cycle livré = N semaines ISO lundi→dimanche + mapping index→responsable, **EN MÉMOIRE** ; aucune des 4 capacités demandées. Heurte le risque spec « coût de saisie du cycle » |
| C1 | CRUD complet sur les acteurs (Delete manquant) + l'utilisateur principal agit sur les acteurs tant qu'ils ne sont pas des utilisateurs | Consigne pour la suite | nouveau besoin (consigne) | Supprimer un acteur (cadrage cases orphelines) + amorce d'impersonation/droits par l'utilisateur principal | Create+Read+Update livrés (s08/s09) ; **Delete absent** (dette BACKLOG É2 ; règle 6 « cases orphelines à cadrer »). Le volet « utilisateur principal agit » rejoint R1 |
| C2-C4 | Supprimer les écrans Poser slot / Affecter période / Définir transfert → dialogs depuis le planning | Consigne pour la suite | nouveau besoin (consigne — **poids séquencement fort**) | Écriture en contexte (palier 8, É12) | Écrans dédiés actuels `PoserSlot` / `AffecterPeriode` / `DefinirTransfert`. La spec porte déjà ce besoin (palier 8, Mécaniques « écriture via dialogs depuis une case ») |

> **Un seul `bug`** (R2), confronté HEAD et cité. Les autres items sont des **évolutions** (R3)
> ou des **nouveaux besoins** (R1, R4, C1, C2-C4) → aucun n'est une réparation à l'aveugle.
> R1/R3/R4 portent sur l'incrément livré (palier 6) ; les consignes C1/C2-C4 sont **forward**
> (orientation de cap, pèsent sur le séquencement).

## Arbitrage

- **Objectif de l'itération suivante (GOAL 1)** — **Écriture en contexte : dialogs depuis le
  planning** (palier 8, É12). Supprimer les écrans dédiés Poser slot / Affecter période / Définir
  transfert et les rouvrir comme **dialogs ouvertes depuis le planning**.
- **Arbitre (départage)** — **l'usage tranche** (spec v10) **+ un défaut confirmé prime sur une
  évolution**. Conséquence appliquée : R2 (seul défaut confirmé) part **en tête**, en `/3` ciblé
  léger, **hors make-gherkin** et indépendant du sujet de sprint. Les **consignes PO** (cap dialogs,
  3 items convergents) ont pesé sur le séquencement et fixent le **prochain sujet en G2 (PO)**.
  Palier 7 (survol enrichi) **skippé** faute de demande PO.

## Séquence de livraison

| Rang | Besoin | Type | Sujet | Dépend de |
|---|---|---|---|---|
| 0 | Fix R2 — dropdown config repointée sur le store vivant `_acteurs` | **bug** | `/3` ciblé léger (**PAS** make-gherkin), **en tête, indépendant** | — |
| 1 | Écriture en contexte — dialogs depuis le planning (Poser slot + Affecter période ; transfert en secours) | nouveau besoin | `ecriture-en-contexte-dialogs` (reprise `/2-make-gherkin`) | — |
| 2 | CRUD acteurs complet — Suppression manquante + amorce impersonation **bornée** (convenance admin) | nouveau besoin | `crud-acteurs-complet` (make-gherkin ultérieur) — fusionne R1 + C1 | rang 1 |
| 3 | Cycle de fond riche — ancre/début (R3) + config fine (R4 : frontière de jour, plage début/fin, sur-cycle vacances, WE-only) | nouveau besoin | `cycle-de-fond-riche` (make-gherkin ultérieur) — **sujet plein à cadrer/découper** | rang 1 |

> **Aucun abandon.** Les 3 besoins forward sont séquencés derrière le prochain sujet, pas écartés.

## Prochain sujet → reprise `/2-make-gherkin`

- **Sujet** : `ecriture-en-contexte-dialogs` — Écriture en contexte : dialogs depuis le planning (palier 8, É12).
- **Scope ~2h IA (corollaire de découpe)** :
  - **Livrer 2 dialogs** : **Poser un slot** + **Affecter une période**, ouvertes depuis le planning
    (le bouton / la case), en remplacement des écrans dédiés.
  - **Transfert** (Définir un transfert) = **tranche de secours** : livré seulement si le scope tient,
    sinon séquencé immédiatement derrière — **ne pas couper par précaution**, ne pas reporter en bloc.
- **Réutilise l'existant (pas de handler neuf attendu)** :
  - **Commandes / handlers** d'écriture déjà livrés (s01/s02/s04) et le **canal HTTP** (s04/s05) —
    l'écriture passe par le canal requête/réponse, jamais en DI direct (règle 28).
  - **Réapparition immédiate** de la saisie dans la grille (palier 2, s06) et **diffusion SignalR**
    lecture seule (jamais reconstruite). L'observable est le **déplacement de la saisie en contexte**,
    pas une nouvelle règle de gestion.
- **Acceptation runtime obligatoire** (rempart anti vert-qui-ment, R4 spec) : dialog réellement
  ouverte depuis le planning câblé (front WASM + API distante + SignalR), saisie aboutie qui
  **réapparaît** dans la grille — pas un test bUnit à doublures.
- **Hors périmètre (reporté, rangs 2-3)** :
  - Suppression d'acteur + impersonation (rang 2) ; cycle de fond riche R3/R4 (rang 3).
  - Survol enrichi (palier 7) : **skippé** ce sprint, faute de demande PO.
  - Aucune persistance tirée en avant (slots/périodes/transferts **restent InMemory**, borne
    anti-cliquet, règle 30).

## Risques & questions encore ouvertes

- **Transfert en tranche de secours — débordement ~2h.** Si Poser slot + Affecter période + Transfert
  débordent ensemble, **couper** au plus petit incrément observable (2 dialogs livrées) et séquencer
  le transfert juste derrière — jamais reporter en bloc (corollaire de découpe / leçon config foyer).
- **Impersonation (rang 2) bornée.** R1 + C1 demandent une vue/des droits par acteur **maintenant**,
  mais l'auth réelle (OAuth) est au **palier 13**. Cadrer l'impersonation comme **amorce de convenance
  admin** (l'utilisateur principal incarne un acteur), **pas** l'authentification complète — ne pas
  tirer la fondation auth devant l'usage.
- **Cycle de fond riche (rang 3) — sujet plein, deux frontières à surveiller.** (1) R3/R4 **rouvrent**
  la décision CP « ancrage ISO sans ancre » (2026-06-27) : choisir explicitement un début/une phase est
  l'option 2 jadis écartée, à ré-arbitrer au cadrage. (2) Plage début/fin **+** sur-cycles vacances
  **chevauchent la durabilité du cycle** (palier 9) : n'enrichir que l'**observable** de cycle, ne PAS
  tirer Mongo pour le cycle par précaution (borne anti-cliquet). Risque spec « coût de saisie du cycle »
  exactement ici → **découpe impérative**, jamais en bloc.
- **R2 (bug) prime mais ne fait pas un sprint.** Le `/3` ciblé est léger (repointer 2 lectures) ; le
  marquer en tête du backlog comme bug à corriger, sans le confondre avec le sujet make-gherkin ni
  l'oublier derrière les besoins forward.
