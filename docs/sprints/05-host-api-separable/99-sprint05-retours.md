# Retours — Sprint 05 (host-api-separable)

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

<!-- une sous-section `## IHM - /<route>` par route du sprint -->
## IHM - /planning/poser-slot

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

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-26 | skill `git` (commit.ps1) | Le script de commit échoue sur le chemin du dépôt accentué « privée » (`git rev-parse` mal décodé) ; phase IHM committée à la main en respectant les garde-fous (branche ≠ main, staging sélectif, trailer). | Durcir `commit.ps1` sur les chemins non-ASCII (encodage UTF-8 de la sortie git / quoting), sinon les garde-fous maison sont contournés à chaque commit. |
| 2026-06-26 | agent `ihm-builder` / `validation-visuelle` | Types absents du registre de session → fallback `general-purpose` à chaque dispatch IHM (régime nominal documenté, déjà constaté sprints 03/04). | Statu quo accepté ; envisager de documenter la commande de chargement du registre ou d'assumer définitivement le fallback dans le pipeline. |

## Notes de contexte (décisions produit, hors méthode)

-
