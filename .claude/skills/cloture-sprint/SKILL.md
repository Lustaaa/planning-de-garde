---
name: cloture-sprint
description: À utiliser pour clore un sprint terminé (validation visuelle faite, retours traités par /4-retours, nouvelle spec produite par /5-consolidation) — pousse la branche, prépare la Pull Request vers main (gh-optionnel : commande gh si présent, sinon titre + corps prêt à coller + URL de comparaison), puis, une fois la PR mergée, revient sur main et amorce l'itération suivante. Adossé au script cloture-sprint.ps1. Rituel mécanique, pas de raisonnement métier.
---

# Clôture de sprint

## Vue d'ensemble

Le **rituel mécanique** de fin de sprint, en bout de boucle. Une fois le sprint livré
(gate visuel passé), ses retours traités (`/4-retours` → besoins + archivage) et la spec
consolidée (`/5-consolidation` → nouvelle version), on **clôt** : push → Pull Request →
merge dans `main` → retour sur `main` → amorce de l'itération suivante.

C'est volontairement **sans intelligence métier** — un enchaînement git/PR déterministe.
La seule part assemblée est le **corps de PR** (depuis les commits `main..HEAD` + le
résumé du sprint), templaté par le script.

## Script

`pwsh -NoProfile -File .claude/skills/cloture-sprint/scripts/cloture-sprint.ps1 [-Base main] [-Sprint <nom>] [-NoPush]`

- **Pousse** la branche courante sur `origin` (sauf `-NoPush`).
- Assemble **titre + corps de PR** (commits en avance sur la base, sprint clos, version
  de spec courante) ; écrit le corps dans un fichier (`bodyPath`) et le renvoie.
- Renvoie l'**URL de comparaison** GitHub (`compareUrl`) et `ghPresent` (booléen).
- **Ne merge jamais** tout seul et ne crée pas la PR sans confirmation.

## gh-optionnel

- **`gh` présent** : la command propose `gh pr create --base main --body-file <bodyPath>`
  puis `gh pr merge` (après validation explicite du PO).
- **`gh` absent** : la command présente le **titre**, le **corps** (contenu de `bodyPath`)
  et l'**URL de comparaison** ; l'utilisateur crée et merge la PR via l'UI GitHub, puis
  signale que c'est mergé pour reprendre (retour `main` + itération suivante).

## Après le merge

- Revenir sur `main`, `git pull`.
- Amorcer l'itération suivante : `/2-make-gherkin` sur la **nouvelle version de spec**
  (`NN-specification.md`), ciblant le `prochain_sujet` du backlog du sprint clos.

## Quand l'utiliser

- En toute fin de boucle, après `/5-consolidation`.
- Jamais depuis `main` (le script refuse) ni avec un working tree non commité.
