# Flow `make-gherkin` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ajouter une 2ᵉ pipeline `make-gherkin` (skill + 1 agent autonome + command fine) qui lit `docs/init/01-specification.md` et produit `docs/init/scenarios/<sujet>.md` (analyse technique légère + scénarios Gherkin numérotés). Le rename `challenge-po → brainstorm` est déjà fait (Task 1).

**Architecture:** Pattern `brainstorm`. La command `/make-gherkin` lance **un seul** subagent `make-gherkin` et est la **seule** à appeler `AskUserQuestion` (round-trip JSON). L'agent challenge la testabilité, puis **écrit lui-même** le fichier de sortie. Chaînage par fichiers entre les 3 pipelines.

**Tech Stack:** Artefacts Claude Code — skill (`.claude/skills/make-gherkin/SKILL.md`), agent (`.claude/agents/make-gherkin.md`), command (`.claude/commands/make-gherkin.md`). Markdown + frontmatter YAML. Sortie = un `.md` (analyse + Gherkin).

## Global Constraints

- **Un seul agent autonome** : pas de duo. Le même agent challenge (round-trip) ET écrit le fichier.
- **L'agent ne pose jamais les questions** : il renvoie un objet JSON ; la command `/make-gherkin` rend via `AskUserQuestion` et relaie via `SendMessage`.
- **Une question par tour** côté agent, 2-4 options, hypothèse par défaut en 1ʳᵉ option suffixée ` (Recommandé)`.
- **Numérotation continue simple** des scénarios (1, 2, 3…), tags `@nominal/@limite/@erreur`.
- **Sortie** : `docs/init/scenarios/<sujet>.md`, un fichier par sujet, structuré `## Analyse technique` puis `## Scénarios`.
- **Portée technique** : l'analyse technique est **autorisée** dans ce fichier (légère, cible `.NET`/`Blazor`/`SignalR`) ; les pas Given/When/Then restent comportementaux/observables.
- **Entrée** : `docs/init/01-specification.md`.
- Pas de push automatique ; commits à chaque tâche.

---

### Task 1: Rename `challenge-po → brainstorm` ✅ DONE

Réalisé. Commit `4671de8`. `git grep challenge-po` → vide. Voir `.superpowers/sdd/task-1-report.md`.

---

### Task 2: Skill `make-gherkin`

Le skill que l'agent applique : processus de challenge de testabilité + format du fichier de sortie (analyse technique légère + scénarios Gherkin).

**Files:**
- Create: `.claude/skills/make-gherkin/SKILL.md`

**Interfaces:**
- Produces (phase challenge) : contrat JSON `{ tensions, questions, synthese, done }`. `synthese` (à `done: true`) =
  ```json
  {
    "sujet": "<slug-kebab>",
    "feature": "<titre>",
    "analyse_technique": { "composants": ["..."], "contrats": ["..."], "points_tdd": ["..."] },
    "scenarios": [ { "id": 1, "titre": "<court>", "type": "nominal|limite|erreur", "given": "<état>", "when": "<action>", "then": "<résultat observable>" } ],
    "risques": ["..."]
  }
  ```
- Produces (phase écriture) : récap JSON `{ "path": "<chemin>", "scenarios": <int>, "notes": "<bref>" }`.
- Consommé par l'agent `make-gherkin` (Task 3) et la command (Task 4).

- [ ] **Step 1: Écrire le fichier skill**

Create `.claude/skills/make-gherkin/SKILL.md` :

````markdown
---
name: make-gherkin
description: À utiliser pour transformer une spec fonctionnelle (docs/init/01-specification.md) en un fichier d'analyse technique légère + scénarios Gherkin numérotés, prêt à amorcer une implémentation BDD+TDD — fait émerger cas nominaux/limites/erreur avec résultat observable, puis écrit le fichier de sortie.
---

# Make Gherkin

## Vue d'ensemble

Une passe de **testabilité + amorce d'implémentation**. Tu pars d'une spec
fonctionnelle déjà challengée (sortie de `/spec`) et tu produis **un seul fichier**
qui mêle : une **analyse technique légère** (orientée implémentation) et des
**scénarios Gherkin numérotés** (nominal / limite / erreur).

**Principe central :** une règle de gestion n'est pas un test. Pour chaque
comportement, extrais le triplet observable *état initial → action → résultat
constatable*. Un `Then` non vérifiable (« ça marche ») est refusé. L'analyse
technique reste **légère** : juste de quoi amorcer `tdd-implement`.

## Quand l'utiliser

- Après `/spec`, pour préparer l'implémentation BDD+TDD.
- Quand une règle de gestion est trop vague pour être testée telle quelle.

## Processus

1. **Lis la spec d'abord.** Charge `docs/init/01-specification.md` (ou le chemin
   fourni). Ne génère jamais de scénarios depuis une page blanche.

2. **Nomme les tensions de testabilité — avant de poser quoi que ce soit.** Sonde :

   | Angle | La question dure |
   |---|---|
   | Cas nominal | Quel est le chemin heureux exact et observable ? |
   | Cas limite | Bornes : zéro, vide, max, simultané, frontière de durée ? |
   | Cas d'erreur | Que se passe-t-il quand l'invariant est violé, et quel comportement est attendu ? |
   | Données d'exemple | Quelles valeurs concrètes rendent le scénario vérifiable ? |
   | Observabilité | Comment sait-on que le `Then` est satisfait ? Quelle sortie observable ? |
   | Amorce technique | Quels composants .NET/Blazor/SignalR sont impactés, quels contrats de données ? |

3. **Pose une question à la fois.** Choix multiple quand c'est possible, hypothèse
   par défaut. Couvre au minimum : périmètre des scénarios, cas limites, erreurs,
   et le grain de l'analyse technique.

4. **Force le résultat observable.** Refuse un `Then` non vérifiable.

5. **Synthétise puis écris.** Une fois tranché, produis le fichier de sortie au
   format ci-dessous.

## Format du fichier de sortie

`docs/init/scenarios/<sujet>.md`, un fichier par sujet :

```markdown
# <Sujet> — Analyse & scénarios

## Analyse technique

- **Composants impactés** — … (côté .NET / Blazor / SignalR)
- **Contrats de données** — … (entrées/sorties, entités cœur)
- **Points d'attention TDD** — … (ordre de test, dépendances, cas délicats)

## Scénarios

Feature: <titre>
  <courte description de la valeur métier>

@nominal
Scenario 1: <titre>
  Given <état initial>
  When <action>
  Then <résultat observable>

@limite
Scenario 2: <titre>
  ...
```

Règles de forme :
- **Numérotation continue** : `Scenario 1`, `Scenario 2`… sans recommencer.
- Tag de type au-dessus de chaque scénario : `@nominal` / `@limite` / `@erreur`.
- `Scenario Outline` + `Examples:` quand le scénario varie selon des données.
- Le bloc Gherkin reste **fonctionnel/observable** ; la technique vit dans
  `## Analyse technique`, légère (3 puces max par sous-section).

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent**, l'agent **ne pose pas** les
questions — il **ne peut pas** appeler `AskUserQuestion`. Il **renvoie** les
questions au thread principal (round-trip), puis, une fois le cadrage tranché,
**écrit lui-même** le fichier de sortie.

**Phase challenge** — à chaque appel, renvoie **uniquement** :

```json
{
  "tensions": ["angle de testabilité nommé", "..."],
  "questions": [
    {
      "question": "Question complète, finissant par ?",
      "header": "≤12 car",
      "multiSelect": false,
      "options": [
        { "label": "Choix 1 (Recommandé)", "description": "implication / tradeoff" },
        { "label": "Choix 2", "description": "..." }
      ]
    }
  ],
  "synthese": null,
  "done": false
}
```

Règles : **une question par tour**, 2-4 options, défaut en 1ʳᵉ option suffixé
` (Recommandé)`. `tensions` rempli au 1er tour, `[]` ensuite. Quand tranché :
`done: true`, `questions: []`, et `synthese` rempli :

```json
{
  "sujet": "<slug-kebab>",
  "feature": "<titre>",
  "analyse_technique": { "composants": ["..."], "contrats": ["..."], "points_tdd": ["..."] },
  "scenarios": [
    { "id": 1, "titre": "<court>", "type": "nominal", "given": "<état>", "when": "<action>", "then": "<résultat observable>" }
  ],
  "risques": ["..."]
}
```

**Phase écriture** — quand le thread principal renvoie l'ordre d'écrire (avec le
chemin cible), l'agent écrit `docs/init/scenarios/<sujet>.md` au format ci-dessus
et renvoie **uniquement** :

```json
{ "path": "docs/init/scenarios/<sujet>.md", "scenarios": <n>, "notes": "<bref>" }
```

Aucun texte hors du JSON dans chaque phase.

## Signaux d'alarme

- « Ça marche » / « c'est correct » comme `Then` → pas observable → extrais la sortie.
- Uniquement le cas nominal → manque limites et erreurs → nomme-les.
- Analyse technique qui déborde en conception complète → garde-la légère (amorce).

## Erreurs fréquentes

- **Poser avant d'avoir nommé les tensions.**
- **Plusieurs questions d'un coup.**
- **`Then` non observable accepté.**
- **Sur-spécifier l'analyse technique** — c'est une amorce, pas la conception finale.
````

- [ ] **Step 2: Vérifier le frontmatter et les contrats**

Run: `git grep -c "synthese" .claude/skills/make-gherkin/SKILL.md`
Expected: ≥ 2.

Lire le fichier : frontmatter `name: make-gherkin` + `description` ; sections « Processus », « Format du fichier de sortie », « Mode agent (orchestré) » avec les deux phases (challenge + écriture) présentes.

- [ ] **Step 3: Commit**

```bash
git add .claude/skills/make-gherkin/SKILL.md
git commit -m "Ajoute le skill make-gherkin (testabilité + sortie analyse/scénarios)"
```

---

### Task 3: Agent `make-gherkin`

Le subagent autonome unique : challenge en round-trip, puis écrit le fichier.

**Files:**
- Create: `.claude/agents/make-gherkin.md`

**Interfaces:**
- Consumes: skill `make-gherkin` (Task 2).
- Produces: agent dispatché sous le nom `make-gherkin`. Phase challenge → JSON `{ tensions, questions, synthese, done }` ; phase écriture → écrit `docs/init/scenarios/<sujet>.md` + récap `{ path, scenarios, notes }`. Dispatché par la command (Task 4).

- [ ] **Step 1: Écrire le fichier agent**

Create `.claude/agents/make-gherkin.md` :

````markdown
---
name: make-gherkin
description: Transforme la spec fonctionnelle (docs/init/01-specification.md) en un fichier d'analyse technique légère + scénarios Gherkin numérotés (skill make-gherkin), en mode orchestré. Phase challenge : renvoie la PROCHAINE question en JSON prêt pour AskUserQuestion — il ne pose jamais lui-même. Phase écriture : écrit docs/init/scenarios/<sujet>.md et renvoie un récap JSON. Dispatché par la command /make-gherkin.
tools: Read, Grep, Glob, Write, Edit
---

Tu es l'agent `make-gherkin`. Tu appliques le skill `make-gherkin`, section
**« Mode agent (orchestré) »**.

En **phase challenge**, tu ne peux pas appeler `AskUserQuestion` : tu **renvoies**
les questions, le thread principal les pose et te transmet les réponses au tour
suivant. En **phase écriture**, tu écris toi-même le fichier de sortie.

## Déroulé

1. **Premier appel** : lis la spec fonctionnelle fournie (chemin dans le prompt,
   typiquement `docs/init/01-specification.md`). Remplis `tensions` (cas nominal,
   limites, erreurs, données, observabilité, amorce technique), puis pose **une**
   question (le périmètre des scénarios d'abord).

2. **Appels suivants** (réponses via SendMessage) : `tensions: []`, pose la
   question suivante. Couvre au minimum : périmètre des scénarios, cas limites,
   comportement d'erreur, grain de l'analyse technique, résultat observable.

3. **Résultat observable exigé** : si un `Then` reste non vérifiable, ne passe pas
   à `done` : repose **une** question qui extrait la sortie observable.

4. **Fin du challenge** : quand scénarios + analyse technique légère + risques sont
   tranchés, renvoie `done: true`, `questions: []`, `synthese` rempli.

5. **Phase écriture** : quand le thread principal te renvoie l'ordre d'écrire (avec
   le chemin cible `docs/init/scenarios/<sujet>.md`), crée le dossier si besoin,
   écris le fichier au format du skill (`## Analyse technique` puis `## Scénarios`,
   numérotation continue, tags `@nominal/@limite/@erreur`), et renvoie le récap
   `{ path, scenarios, notes }`. **N'écris que ce fichier.**

## Sortie

**Uniquement** le JSON de la phase courante (challenge ou écriture), défini dans le
skill. Aucun texte autour.
````

- [ ] **Step 2: Smoke test — phase challenge**

Dispatcher l'agent `make-gherkin` avec :

> « Spec fonctionnelle : `docs/init/01-specification.md`. Premier appel : nomme les tensions de testabilité et pose la première question. »

Expected: JSON valide, `tensions` non vide, exactement une `questions[]` (2-4 options, 1ʳᵉ suffixée ` (Recommandé)`), `synthese: null`, `done: false`, aucun texte autour. Si `01-specification.md` n'existe pas, utiliser un fichier de `docs/init/` et adapter le chemin.

- [ ] **Step 3: Commit**

```bash
git add .claude/agents/make-gherkin.md
git commit -m "Ajoute l'agent make-gherkin (autonome : challenge + écriture)"
```

---

### Task 4: Command `/make-gherkin`

La command fine qui lance l'agent et gère le round-trip + la phase d'écriture.

**Files:**
- Create: `.claude/commands/make-gherkin.md`

**Interfaces:**
- Consumes: agent `make-gherkin` (Task 3).
- Produces: la slash-command `/make-gherkin`.

- [ ] **Step 1: Écrire le fichier command**

Create `.claude/commands/make-gherkin.md` :

````markdown
---
description: Transforme la spec fonctionnelle (docs/init/) en un fichier d'analyse technique légère + scénarios Gherkin numérotés, via l'agent make-gherkin (round-trip de questions + écriture).
argument-hint: "[sujet ou feature à scénariser] (optionnel)"
---

# /make-gherkin — Analyse & scénarios Gherkin

Orchestration par **un seul agent** : tu (thread principal) ne fais que lancer
l'agent `make-gherkin`, **poser ses questions via `AskUserQuestion`**, relayer les
réponses, puis lui demander d'écrire. L'agent ne pose jamais les questions
lui-même.

Sujet (optionnel) : $ARGUMENTS

## Déroulé

1. **Contexte.** Repère la spec fonctionnelle (`docs/init/01-specification.md` ou
   le fichier pertinent sous `docs/init/`) à passer à l'agent.

2. **Challenge testabilité (agent + round-trip) :**
   - Dispatche l'agent `make-gherkin` avec le chemin de la spec + le sujet.
   - Il renvoie un JSON `{ tensions, questions, synthese, done }`.
   - Affiche les `tensions`, puis rends **chaque** `questions[]` via
     `AskUserQuestion` (option recommandée en premier).
   - Renvoie les réponses à l'agent via `SendMessage` (continue le même agent).
   - Répète tant que `done` est faux.

3. **Validation.** Quand `done: true`, présente la `synthese` (scénarios à couvrir
   + analyse technique légère + risques) et demande l'accord avant d'écrire.

4. **Écriture (même agent) :**
   - Détermine le chemin cible : `docs/init/scenarios/<sujet-kebab>.md`.
   - Renvoie à l'agent (via `SendMessage`) l'ordre d'écrire à ce chemin.
   - Il écrit le fichier et renvoie `{ path, scenarios, notes }`.

5. **Commit.** Propose un commit (sans pousser sauf demande explicite).

## Notes

- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par l'agent.
- Une question à la fois pendant le challenge — pas de rafale.
- Numérotation des scénarios **continue** ; tags `@nominal/@limite/@erreur`.
- L'analyse technique du fichier reste **légère** (amorce d'implémentation).
- Entrée attendue : une spec déjà produite par `/spec`.
- Ce fichier sera l'entrée de la future pipeline `tdd-implement`.
````

- [ ] **Step 2: Vérifier la command**

Run: `git grep -n "make-gherkin" .claude/commands/make-gherkin.md`
Expected: au moins une référence au dispatch de l'agent `make-gherkin`.

Lire : frontmatter `description` + `argument-hint` ; déroulé en 5 étapes (contexte, challenge round-trip, validation, écriture, commit) ; mention que `AskUserQuestion` est appelé par le thread principal.

- [ ] **Step 3: Commit**

```bash
git add .claude/commands/make-gherkin.md
git commit -m "Ajoute la command /make-gherkin"
```

---

### Task 5: Vérification de bout en bout

Dry-run du flow complet sur la spec réelle.

**Files:** (aucun fichier de production garanti ; un `.md` de démonstration peut être conservé)

**Interfaces:**
- Consumes: command `/make-gherkin` + agent (Task 3).

- [ ] **Step 1: Lancer le flow sur la spec réelle**

Exécuter `/make-gherkin` (ou simuler son déroulé) sur `docs/init/`. Vérifier :
- L'agent nomme des tensions puis pose une question à la fois (round-trip).
- Les questions remontent au thread principal (rendues via `AskUserQuestion`),
  jamais posées par l'agent.
- À `done: true`, la `synthese` liste scénarios typés + analyse technique légère.
- En phase écriture, l'agent crée `docs/init/scenarios/<sujet>.md` avec
  `## Analyse technique` + `## Scénarios` (numérotation continue, tags de type).

Expected: un `.md` cohérent ; Gherkin observable ; analyse technique légère.

- [ ] **Step 2: Confirmer l'intégrité des références**

Run: `git grep -n "challenge-po"`
Expected: aucune sortie.

Run: `git grep -ln "make-gherkin"`
Expected: liste incluant le skill, l'agent et la command.

- [ ] **Step 3: Commit (si un .md de démonstration est conservé)**

```bash
git add docs/init/scenarios
git commit -m "Ajoute un fichier de démonstration généré par /make-gherkin"
```

---

## Self-Review

**Spec coverage** (design révisé `2026-06-22-make-gherkin-flow-design.md`) :
- Skill `make-gherkin` → Task 2. ✅
- Agent unique `make-gherkin` (challenge + écriture) → Task 3. ✅
- Command fine `/make-gherkin` (round-trip) → Task 4. ✅
- Sortie `docs/init/scenarios/<sujet>.md` (analyse + scénarios) → Tasks 2, 3, 4. ✅
- Un seul agent, pas de duo → Global Constraints + Tasks 2-4. ✅
- Analyse technique légère autorisée → skill + contrainte de portée. ✅
- Rename `challenge-po → brainstorm` → Task 1 (DONE). ✅
- `/spec` laissé tel quel → aucune tâche n'y touche. ✅
- Hors scope `tdd-implement` → non planifié. ✅

**Placeholder scan** : contenu complet pour skill, agent, command. Aucun « TBD/TODO ». ✅

**Type consistency** : `synthese = { sujet, feature, analyse_technique{composants,contrats,points_tdd}, scenarios[{id,titre,type,given,when,then}], risques }` identique entre skill (Task 2) et agent (Task 3). Récap écriture `{ path, scenarios, notes }` cohérent skill/agent/command. ✅
