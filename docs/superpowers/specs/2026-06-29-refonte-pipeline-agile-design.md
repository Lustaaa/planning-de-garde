# Design — Refonte de la couche agile (méta-pipeline)

> Date : 2026-06-29 · Objet : le **pipeline** lui-même (agents/skills/commands), pas le produit.

## Problème

Le pipeline actuel (6 commands, ~10 agents, ~12 skills) est lourd :

- **Lenteur** — un sprint ≥ 2h ; trop d'étapes, ne colle pas au modèle SCRUM réel (A).
- **Acteurs non modélisés** — pas de rôles SCRUM explicites (PO / Scrum Master / Dev team / Architecte) (B, E).
- **Spec qui enfle** — la spec a été réécrite entière chaque sprint et a été multipliée par ~10 (`01` → `15`) par accumulation (C).
- **Coût tokens** — un sprint consomme ~50 % du budget de session (D).

**Cause racine commune** : trop de dispatches (1 agent = 1 contexte rechargé), trop de paperasse (dossier de suivi + fichier par scénario), réécriture intégrale de spec, rétro impérative verbeuse sans effet.

**Nord** : chaque sprint **fait avancer l'app concrètement**, en **moitié moins de temps/tokens**.

Ce qui marche aujourd'hui et qu'on garde : l'agent **CP** comme tampon décisionnel entre les agents et le PO ; le **gate visuel** ; l'**acceptation runtime** (anti vert-qui-ment) ; les **portes PO minimales** (G2, G3, git) ; les **scripts** PowerShell à garde-fous.

## Principe directeur

Un **acteur SCRUM = un chapeau**, pas forcément un dispatch. Peu d'agents portant plusieurs chapeaux → moins de rechargements de contexte → plus rapide et moins cher. On modélise les rôles **sans** multiplier les agents.

## Les acteurs

| Rôle SCRUM | Incarnation | Rôle |
|---|---|---|
| **Product Owner** | **Toi (humain)** | Vision, priorités, valide G2/G3/git. |
| **Scrum Master** | agent `scrum-master` | Orchestre, tampon décisionnel (absorbe l'ex-**CP**), tranche tout sauf G2/G3/git. Porte les chapeaux **retours produit** et **rétro méthode**. |
| **Dev team** | agent `dev-team` | Implémente : TDD / DDD / BDD / CQRS / archi hexagonale. Absorbe `tdd-analyse` + `tdd-auto` + `ihm-builder`. Back **et** IHM. |
| **Architecte** | agent `architecte` | **Hors-sprint.** Bypass total de la méthodo : exécute exactement la consigne technique du PO. **Ne démarre jamais un sprint.** Après intervention, **resynchronise** docs/spec/CLAUDE.md pour que SM + dev-team ne soient pas perdus. **Exclusif** avec `dev-team` (jamais les deux le même sprint). |

3 agents (vs ~10). Le CP disparaît (fondu dans le Scrum Master).

## Les phases — 3 commands (vs 6)

Repli des 6 commands vers les événements SCRUM réels (Planning · exécution · Review · Rétro).

### `/planning` — Sprint Planning (Scrum Master)
1. Le SM lit le **backlog vivant** (retours produit prioritaires) + la spec.
2. Il propose **3-4 goals candidats**, chacun en **carte** : titre + **bullets de scope concret** (« ce sprint : x · y · z »), retours non traités en tête.
3. **Porte G2** : le PO tranche (peut injecter un 5ᵉ goal).
4. Le SM écrit le **fichier de sprint léger** : tableau d'avancement **en tête** + scénarios **Gherkin structurés** + section retours (vide) en bas. Pas de dossier, pas de fichier-par-scénario.

### `/sprint` — Exécution + Review (Dev team, puis gate PO)
1. `dev-team` implémente **tous** les scénarios (boucle BDD externe + cycles TDD internes), back **puis** IHM Blazor/SignalR réel.
2. Met à jour le **tableau d'avancement** en direct (⏳/🔴/✅), commite par scénario.
3. **Acceptation runtime obligatoire** (store réel, câblage réel) — pas de preuve par doublure.
4. **Gate visuel G3** : back + IHM up → le PO teste et remplit la section retours du fichier de sprint.

### `/cloture` — Rétro + clôture (Scrum Master)
1. **Retours produit** (chaque sprint) : le SM fusionne les retours du PO dans le **backlog vivant** — rien ne se perd, les items restent jusqu'à **fait** (corrige les retours répétés).
2. **Spec en diff** : le SM édite **le(s) seul(s) sujet(s) de spec concerné(s)**, jamais une réécriture intégrale (corrige le x10).
3. **Rétro méthode — conditionnelle** : seulement si friction réelle ce sprint. Output = **un edit concret** d'un fichier pipeline + **1 ligne** de journal. Sinon **skip**. Devise : « amélioration ou rien », pas « rétro ou rien ».
4. **Git** : push → PR → merge `main` (porte git PO). Retour `main`, reboucle `/planning`.

## Artefacts (paperasse minimale)

| Fichier | Rôle | Évolution |
|---|---|---|
| `docs/specs/` | **Spec vivante éclatée** : un fichier par **sujet fonctionnel** + `index.md` navigable (liens markdown). | Éditée **en diff** par sujet. Fin du monolithe et du x10. |
| `docs/BACKLOG.md` | **Backlog produit vivant** : retours persistants (fait / à faire), source des goals candidats. | Alimenté à chaque `/cloture`, lu à chaque `/planning`. |
| `docs/sprints/NN-sujet.md` | **1 seul fichier par sprint** : tableau d'avancement en tête + Gherkin + retours en bas. | Plus de dossier de suivi ni de fichier-par-scénario. |
| Journal méthode | 1 ligne par amélioration de rétro (pas de doc dédié `98-retrospective.md`). | Append-only, minuscule. |

## Scripts (garde-fous, réutilisables)

Skills et agents **adossés à des scripts PowerShell** (jamais de commande brute).

- **Réutilisés** : `git/*` (branch, commit, pr, push, status, sync), `run.ps1`, `test-count.ps1`.
- **À ajouter** : wrappers `dotnet` manquants — `restore.ps1`, `build.ps1`, `test.ps1` (suite complète, Docker actif, sans `--no-build` ni filtre).

## Étape 0 — Archivage

Avant reconstruction : déplacer l'existant vers `.claude/_archive/{agents,skills,commands}/` (inspiration des nouveaux, pas supprimé).

## Leviers de réduction tokens/temps (récap)

1. 3 agents au lieu de ~10 → moins de contextes rechargés.
2. 3 commands au lieu de 6 → moins d'étapes/handoffs.
3. 1 fichier de sprint au lieu d'un dossier + N fichiers.
4. Spec éditée en diff, pas réécrite.
5. Rétro conditionnelle, output = edit (pas de doc).
6. SM en relais **compact** (prompts de dispatch et récaps courts, substance gardée).

## Hors scope

- Le produit planning-de-garde lui-même (règles métier, features).
- Refactor technique du code applicatif (c'est le rôle de l'`architecte`, hors-sprint).

## Risques

- **Dev-team trop gros** (back+IHM+TDD en un agent) : contexte chargé. Atténuation : scénarios backend d'abord, IHM en fin, scripts pour build/test (sortent du contexte).
- **Perte du détail de suivi** (plus de fichier-par-scénario) : atténué par le tableau d'avancement en tête + tags @rouge/@vert.
- **Rétro conditionnelle ignorée** : risque qu'on n'améliore jamais. Atténuation : le SM doit justifier explicitement « pas de friction » à la clôture.
