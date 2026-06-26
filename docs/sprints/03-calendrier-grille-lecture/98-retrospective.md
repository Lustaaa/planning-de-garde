# Rétrospective — Sprint 03 (calendrier, grille de lecture)

> Rétro de la **méthode** (pipeline d'agents/skills/commands) · produite par `retro-sprint`
> (étape 1 de `/6-cloture-sprint`). Distincte de `99-sprint03-besoins-fin-itération.md`
> (rétro produit). 1re rétro du projet : avant elle, aucun `98-retrospective.md` n'existait.

## Ce qui a bien marché

- **Garde-fou early-green de `tdd-auto`** : a tenu. 2 scénarios (Sc.6, Sc.8) et 1 test
  (Sc.3 #2) ont déclenché une **suspension de boucle** au lieu d'un faux vert silencieux —
  le PO a pu trancher (suppression des doublons).
- **Journal méthode unifié** (`# Méthode (agents)` + `## IA` dans `99-sprint03-retours.md`) :
  a effectivement capté les frictions au fil du sprint — la matière de rétro était prête.
- **Routage backend vs IHM de `/3`** : a fonctionné (scénarios étiquetés, render mode global).

## Ce qui a coincé

- **Bypass de la rétrospective elle-même (CRITIQUE)** — aucun `98-retrospective.md`
  n'existait ; `retro-sprint` n'était câblé dans aucune command (`/4` → `/5` → `/2` sans
  rétro). On a enchaîné un nouveau cycle sans amélioration continue.
- **Types d'agents absents du registre** — `ihm-builder`, `validation-visuelle` (et
  `retro-sprint`) tombaient en fallback `general-purpose` à chaque dispatch.
- **`commit.ps1 -Files` séparé par espaces** échoue (« positional parameter »).
- **Propagation `/5` oubliée** — pointeur « Spec courante » du README périmé de 2 versions.
- **Course spec `/2`** — `/2` a démarré sur v02 pendant que `/5` écrivait v03 en tâche de fond.
- **`tdd-analyse` n'anticipe pas l'early-green des caractérisations** — Sc.6 (acquis par
  Sc.1+Sc.3) et Sc.8 (contrat du port `IPaletteCouleurs`) prédits drivers à tort (2/8 retirés).
- **Bruit récurrent du tag `@vert` auto-référentiel** dans `tdd-auto` (boucle `--amend`).
- **Demandes PO** : schéma README verbeux/dev (illisible non-tech) ; pas de product backlog
  cumulé ; vocabulaire SCRUM non nommé ; `/clear` à faire après le plan Gherkin.

## Actions sur le pipeline

| # | Cible (fichier) | Édition | Statut |
|---|---|---|---|
| 1 | `.claude/skills/retro-sprint/scripts/find-retro.ps1` (nouveau) + `.claude/commands/6-cloture-sprint.md` | Gate `find-retro.ps1` (détecte sprint clos sans `98-retrospective.md`) + `retro-sprint` en **étape 1 impérative** de `/6` avant push | ✅ appliquée |
| 2 | `.claude/commands/2-make-gherkin.md` + `4-retours.md` + `5-consolidation.md` | Gate dur d'entrée `/2` (refuse si rétro manquante) + gardes de handoff dans `/4` et `/5` (double barrière) | ✅ appliquée |
| 3 | `.claude/agents/retro-sprint.md` (nouveau) | Agent dédié `retro-sprint` (plus de fallback `general-purpose`) | ✅ appliquée |
| 4 | `.claude/commands/3-tdd-implement.md` + `README-claude.md` | Note « agents requis dans le registre » (`ihm-builder`/`validation-visuelle`) + liste dans la table README | ✅ appliquée |
| 5 | `.claude/skills/git/scripts/commit.ps1` + `SKILL.md` | Forme `-Files a,b,c` (**virgules**) documentée comme canonique ; la forme espaces est explicitement signalée comme échouant (`ValueFromRemainingArguments` ne se lie pas de façon fiable sur un param nommé). La friction réelle était l'**absence de doc** — corrigée. | ✅ appliquée |
| 6 | `.claude/skills/spec-consolidation/scripts/find-spec.ps1` + `5-consolidation.md` | Mode `-PropagateReadme` (réécrit mécaniquement le pointeur spec README) + étape 6 de `/5` mécanique | ✅ appliquée |
| 7 | `.claude/commands/2-make-gherkin.md` | `/2` résout la spec courante via `find-spec.ps1` (anti-course avec `/5`) | ✅ appliquée |
| 8 | `.claude/agents/tdd-analyse.md` | Anticipe early-green : vérifier le **contrat des ports** + les **invariants cross-scénario** avant de prédire une contradiction (exemples Sc.6/Sc.8) | ✅ appliquée |
| 9 | `.claude/agents/tdd-auto.md` | Tag `@vert` **sans hash** auto-référentiel (tue la boucle `--amend`) ; lien optionnel en 2 temps | ✅ appliquée |
| 10 | `README-claude.md` | Schéma **concis non-technique** (langage métier) + maillon `retro-sprint`, détail technique en annexe | ✅ appliquée |
| 11 | `docs/BACKLOG.md` (nouveau) + `4-retours.md` + `6-cloture-sprint.md` | **Product backlog permanent** (fait/en cours/à faire + sprint), alimenté par `/4` (ajout) et `/6` (passage à « fait ») | ✅ appliquée |
| 12 | `README-claude.md` + commands | Mapping vocabulaire **SCRUM** ↔ pipeline | ✅ appliquée |
| 13 | `.claude/commands/2-make-gherkin.md` + `3-tdd-implement.md` | `/clear` **après** l'écriture du plan Gherkin, jamais avant | ✅ appliquée |

## Questions ouvertes (méthode)

- **Charger réellement** `ihm-builder`, `validation-visuelle`, `retro-sprint` dans le
  registre de session (aujourd'hui documenté + fallback ; reste à câbler le chargement).
- `AskUserQuestion` plafonne à 4 options : la priorisation rétro à >4 actions doit être
  groupée par le thread principal (contrainte tooling, pas du pipeline).
- Corps de PR narratif (`/6`) encore templaté — enrichissement futur possible.

> **Note d'exécution** : application menée au thread principal (sur confirmation directe du
> PO), le subagent `retro-sprint` ayant été bloqué par le classifieur (autorité d'« apply »
> relayée par le coordinateur, non par l'utilisateur). Toutes les actions 1–13 validées « Toutes ».
