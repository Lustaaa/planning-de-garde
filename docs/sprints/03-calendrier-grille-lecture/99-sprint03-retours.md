# Retours — Sprint 03 (calendrier, grille de lecture)

> **Fichier unifié.** Il porte deux choses, consommées par deux étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
>
> Créé à l'analyse `/3` (par `tdd-analyse`). La partie produit est préparée vide ici et
> remplie par le PO après le gate visuel ; la partie méthode est appendée au fil de l'eau
> par le thread principal. Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.

## IHM - général

-

## IHM - /planning

-

## Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-24 | `/2-make-gherkin` (déroulé pipeline) | Faire le `/clear` **après** la rédaction du plan Gherkin du sprint, pas avant. | Adopté dès le sprint 03 : ne plus clearer en fin de `/2` ; le clear intervient une fois `docs/sprints/NN-<sujet>.md` écrit. À porter dans le déroulé `/2`/`/3` par retro-sprint. |

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-24 | `/2-make-gherkin` (entrée spec) | L'agent a démarré sur `docs/02-specification.md` alors qu'une `/5-consolidation` produisait `docs/03-specification.md` en tâche de fond (course) ; redirigé à chaud vers v03, challenge repris à zéro. | `/2` devrait résoudre la spec courante = plus grand `NN-specification.md` au dispatch, pas `01-`/`02-` en dur, pour éviter la course avec `/5`. |

## Notes de contexte (décisions produit, hors méthode)

-
