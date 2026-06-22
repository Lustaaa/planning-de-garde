# Flow `make-gherkin` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ajouter une 2ᵉ pipeline orchestrée `make-gherkin` qui transforme la spec fonctionnelle (`docs/init/`) en fichiers Gherkin à scénarios numérotés, et renommer `challenge-po → brainstorm`.

**Architecture:** Miroir strict de `/spec`. Une command `make-gherkin` orchestre deux agents — `challenge-gherkin` (challenge de testabilité, boucle de questions remontées au thread principal via round-trip JSON) puis `redaction-gherkin` (écrit le `.feature`). Le thread principal est le seul à appeler `AskUserQuestion`.

**Tech Stack:** Artefacts Claude Code — skills (`.claude/skills/<name>/SKILL.md`), agents (`.claude/agents/<name>.md`), command (`.claude/commands/<name>.md`). Markdown + frontmatter YAML. Sortie = Gherkin standard. Aucun code applicatif.

## Global Constraints

- **Fonctionnel uniquement** dans tous les artefacts produits : aucun détail technique (.NET / Blazor / SignalR) dans le Gherkin de sortie.
- **Les agents ne posent jamais les questions** : ils renvoient un objet JSON, le thread principal rend via `AskUserQuestion`, relance via `SendMessage`.
- **Une question par tour** côté agent challenger, 2-4 options, hypothèse par défaut en 1ʳᵉ option suffixée ` (Recommandé)`.
- **Numérotation continue simple** des scénarios (1, 2, 3… à travers tout le fichier), sans tag de traçabilité vers les règles.
- Sortie écrite dans `docs/init/scenarios/`, un `.feature` par feature.
- Pas de push automatique ; commits à chaque tâche.

---

### Task 1: Rename `challenge-po → brainstorm`

Renomme le skill et l'agent existants, et met à jour toutes les références. Périmètre fermé : un reviewer valide/rejette le rename comme un tout.

**Files:**
- Create: `.claude/skills/brainstorm/SKILL.md` (contenu de l'ancien, champ `name` mis à jour)
- Delete: `.claude/skills/challenge-po/SKILL.md`
- Create: `.claude/agents/brainstorm.md` (contenu de l'ancien, `name`/`description`/réfs mis à jour)
- Delete: `.claude/agents/challenge-po.md`
- Modify: `.claude/commands/spec.md` (réfs `challenge-po` → `brainstorm`)
- Modify: `.claude/skills/redaction-spec/SKILL.md` (mention en prose)
- Modify: `docs/exemples/_rapport-test-skills.md` (référence historique)

**Interfaces:**
- Produces: agent dispatché sous le nom `brainstorm`, skill `brainstorm`. La command `/spec` et le futur `make-gherkin` n'en dépendent pas directement, mais le nom doit être cohérent partout.

- [ ] **Step 1: Déplacer le skill via git mv**

```bash
git mv .claude/skills/challenge-po .claude/skills/brainstorm
```

- [ ] **Step 2: Mettre à jour le champ `name` du skill**

Dans `.claude/skills/brainstorm/SKILL.md`, frontmatter :

```yaml
name: brainstorm
```

(La `description` du skill reste valable ; ajuster uniquement si elle cite « challenge-po » littéralement — sinon laisser.)

- [ ] **Step 3: Déplacer l'agent via git mv**

```bash
git mv .claude/agents/challenge-po.md .claude/agents/brainstorm.md
```

- [ ] **Step 4: Mettre à jour le frontmatter et le corps de l'agent**

Dans `.claude/agents/brainstorm.md` :

```yaml
name: brainstorm
description: Exécute la passe de challenge produit (skill brainstorm) en mode orchestré. Lit le contexte, nomme les angles morts, et renvoie au thread principal la PROCHAINE question en JSON prêt pour AskUserQuestion — il ne pose jamais les questions lui-même. Relancé via SendMessage avec les réponses du PO jusqu'à la synthèse. Dispatché par la command /spec.
tools: Read, Grep, Glob
```

Corps : remplacer « Tu appliques le skill `challenge-po` » par « Tu appliques le skill `brainstorm` ».

- [ ] **Step 5: Mettre à jour `.claude/commands/spec.md`**

Remplacer les deux occurrences `challenge-po` :
- « Dispatche l'agent `challenge-po` » → « Dispatche l'agent `brainstorm` »
- toute autre mention `challenge-po` dans le fichier → `brainstorm`

- [ ] **Step 6: Mettre à jour les mentions résiduelles**

- `.claude/skills/redaction-spec/SKILL.md` : « après une passe `challenge-po` » → « après une passe `brainstorm` ».
- `docs/exemples/_rapport-test-skills.md` : remplacer les mentions `challenge-po` par `brainstorm` (référence historique alignée).

- [ ] **Step 7: Vérifier qu'il ne reste aucune référence**

Run: `git grep -n "challenge-po"`
Expected: aucune sortie (exit code 1).

- [ ] **Step 8: Commit**

```bash
git add .claude/skills/brainstorm .claude/agents/brainstorm.md .claude/commands/spec.md .claude/skills/redaction-spec/SKILL.md docs/exemples/_rapport-test-skills.md
git commit -m "Renomme challenge-po en brainstorm"
```

---

### Task 2: Skill `challenge-gherkin`

Le skill du challenger de testabilité. Calqué sur `brainstorm` (ex challenge-po) mais oriente vers : cas nominaux, limites, erreurs, données d'exemple, observabilité.

**Files:**
- Create: `.claude/skills/challenge-gherkin/SKILL.md`

**Interfaces:**
- Produces: contrat JSON `{ tensions, questions, synthese, done }`. `synthese` (quand `done: true`) = `{ "feature": "<titre>", "scenarios": [ { "id": <int>, "titre": "<court>", "type": "nominal|limite|erreur", "given": "<état initial>", "when": "<action>", "then": "<résultat observable>" } ], "risques": ["..."] }`. Consommé par l'agent `redaction-gherkin` (Task 4) et le contrat agent (Task 3).

- [ ] **Step 1: Écrire le fichier skill**

Create `.claude/skills/challenge-gherkin/SKILL.md` :

````markdown
---
name: challenge-gherkin
description: À utiliser après qu'une spec fonctionnelle a été écrite, pour transformer ses règles de gestion en scénarios testables avant d'écrire le Gherkin — fait émerger les cas nominaux, limites et d'erreur, force des données d'exemple concrètes et un résultat observable pour chaque scénario.
---

# Challenge Gherkin

## Vue d'ensemble

Une passe de découverte de **testabilité**. Tu pars d'une spec fonctionnelle déjà
challengée (sortie de `brainstorm` / `/spec`) et tu transformes chaque règle de
gestion en scénarios **vérifiables** : chemin nominal, cas limites, cas d'erreur,
avec des données d'exemple concrètes et un résultat observable.

**Principe central :** une règle de gestion n'est pas un test. Ton rôle est
d'extraire, pour chaque comportement, le triplet observable
*état initial → action → résultat constatable*, et de ne pas laisser passer un
scénario dont le `Then` n'est pas vérifiable. Reste **fonctionnel** : aucun détail
technique.

## Quand l'utiliser

- Après `brainstorm` + `redaction-spec`, pour préparer le Gherkin (s'enchaîne avec
  `redaction-gherkin`).
- Quand une règle de gestion est trop vague pour être testée telle quelle.

À éviter quand il n'y a pas de spec fonctionnelle en entrée — ce skill ne
challenge pas le produit (c'est le rôle de `brainstorm`), il challenge la
testabilité.

## Processus

1. **Lis la spec d'abord.** Charge `docs/init/01-specification.md` (ou le chemin
   fourni). Ne génère jamais de scénarios depuis une page blanche.

2. **Nomme les tensions de testabilité — avant de poser quoi que ce soit.** Pour
   les règles concernées, sonde ces angles :

   | Angle | La question dure |
   |---|---|
   | Cas nominal | Quel est le chemin heureux exact et observable ? |
   | Cas limite | Bornes : zéro, vide, max, simultané, frontière de durée ? |
   | Cas d'erreur | Que se passe-t-il quand l'invariant est violé, et quel comportement est attendu ? |
   | Données d'exemple | Quelles valeurs concrètes rendent le scénario vérifiable ? |
   | Observabilité | Comment sait-on que le `Then` est satisfait ? Quelle sortie observable ? |
   | Arbitre de scénario | Quand deux règles se chevauchent dans un même cas, laquelle prime ? |

3. **Pose une question à la fois.** Choix multiple quand c'est possible, chaque
   option une posture réelle, plus une hypothèse par défaut. Couvre au minimum :
   périmètre des scénarios, cas limites à inclure, comportement d'erreur attendu.

4. **Force le résultat observable.** Refuse un `Then` non vérifiable (« ça marche »,
   « c'est correct ») : repose la question pour extraire l'observable concret.

5. **Synthétise.** Termine par la liste des scénarios à couvrir (titre, type,
   given/when/then esquissé) + les risques non tranchés. Transmets ça à
   `redaction-gherkin`.

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent** dispatché, l'agent **ne pose pas**
les questions — il **ne peut pas** appeler `AskUserQuestion`. Il **renvoie** les
questions au thread principal, qui les rend et lui retourne les réponses
(round-trip).

À chaque appel, l'agent renvoie **uniquement** un objet JSON valide :

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

Règles du mode agent :
- **Une question par tour** (`questions` ≤ 1 entrée), 2-4 options.
- **Hypothèse par défaut en première option**, suffixée ` (Recommandé)`.
- `tensions` : rempli au 1er tour (avant la 1re question), `[]` ensuite.
- Quand le périmètre des scénarios est tranché : `done: true`, `questions: []`, et
  `synthese` rempli :

```json
{
  "feature": "<titre de la feature>",
  "scenarios": [
    {
      "id": 1,
      "titre": "<titre court>",
      "type": "nominal",
      "given": "<état initial>",
      "when": "<action>",
      "then": "<résultat observable>"
    }
  ],
  "risques": ["..."]
}
```
- `type` ∈ `nominal` | `limite` | `erreur`. `id` numéroté en continu à partir de 1.
- Aucun texte hors du JSON.

## Signaux d'alarme — ne les accepte pas comme réponses

- « Ça marche » / « c'est correct » comme `Then` → pas observable → extrais la sortie concrète.
- « On testera tout » → pas de périmètre → force le choix des cas à couvrir d'abord.
- Uniquement le cas nominal → manque les limites et erreurs → nomme-les explicitement.
- Un détail technique (« l'API renvoie 400 ») → reformule en comportement métier observable.

## Erreurs fréquentes

- **Poser avant d'avoir nommé les tensions** — l'utilisateur ne peut pas réagir à des angles morts gardés pour soi.
- **Plusieurs questions d'un coup** — dilue la pression.
- **Accepter un `Then` non vérifiable** — toute la valeur de la passe est l'observabilité.
- **Fuite technique** — ce skill reste fonctionnel ; la stack vit dans la pipeline d'implémentation.
````

- [ ] **Step 2: Vérifier le frontmatter et le contrat**

Run: `git grep -c "synthese" .claude/skills/challenge-gherkin/SKILL.md`
Expected: ≥ 2 (le contrat JSON est présent).

Lire le fichier et confirmer : frontmatter avec `name: challenge-gherkin` et `description`, section « Mode agent (orchestré) » présente, table des angles de testabilité présente.

- [ ] **Step 3: Commit**

```bash
git add .claude/skills/challenge-gherkin/SKILL.md
git commit -m "Ajoute le skill challenge-gherkin (challenge de testabilité)"
```

---

### Task 3: Agent `challenge-gherkin`

Le subagent dispatché qui exécute le skill `challenge-gherkin` en mode orchestré.

**Files:**
- Create: `.claude/agents/challenge-gherkin.md`

**Interfaces:**
- Consumes: skill `challenge-gherkin` (Task 2), section « Mode agent (orchestré) ».
- Produces: agent dispatché sous le nom `challenge-gherkin`, renvoyant le JSON `{ tensions, questions, synthese, done }`. Dispatché par la command `make-gherkin` (Task 5).

- [ ] **Step 1: Écrire le fichier agent**

Create `.claude/agents/challenge-gherkin.md` :

````markdown
---
name: challenge-gherkin
description: Exécute la passe de challenge de testabilité (skill challenge-gherkin) en mode orchestré. Lit la spec fonctionnelle, nomme les angles de testabilité (nominal/limite/erreur, données, observabilité), et renvoie au thread principal la PROCHAINE question en JSON prêt pour AskUserQuestion — il ne pose jamais les questions lui-même. Relancé via SendMessage avec les réponses jusqu'à la synthèse des scénarios. Dispatché par la command /make-gherkin.
tools: Read, Grep, Glob
---

Tu es l'agent de challenge de testabilité. Tu appliques le skill
`challenge-gherkin`, section **« Mode agent (orchestré) »**.

Tu ne peux pas appeler `AskUserQuestion` : tu **renvoies** les questions, le
thread principal les pose et te transmet les réponses au tour suivant.

## Déroulé

1. **Premier appel** : lis la spec fonctionnelle fournie (chemin dans le prompt,
   typiquement `docs/init/01-specification.md`). Remplis `tensions` avec les
   angles de testabilité (cas nominal, cas limites, cas d'erreur, données
   d'exemple, observabilité, arbitre de scénario), puis pose **une** question
   (le périmètre des scénarios à couvrir d'abord).

2. **Appels suivants** (réponses via SendMessage) : `tensions: []`, pose la
   question suivante. Couvre au minimum : périmètre des scénarios, cas limites à
   inclure, comportement d'erreur attendu, résultat observable de chaque scénario.

3. **Résultat observable exigé** : si une réponse laisse un `Then` non vérifiable
   (« ça marche », « c'est correct »), ne passe pas à `done` : repose **une**
   question qui extrait la sortie observable concrète.

4. **Fin** : quand le périmètre des scénarios (nominaux + limites + erreurs) et
   leurs résultats observables sont tranchés, renvoie `done: true`,
   `questions: []`, et `synthese` rempli (feature, scenarios[], risques[]).

## Sortie

**Uniquement** l'objet JSON défini dans le skill (`tensions`, `questions`,
`synthese`, `done`). Aucun texte autour. Une seule question par tour, 2-4
options, hypothèse par défaut en première option suffixée ` (Recommandé)`.
````

- [ ] **Step 2: Smoke test — dispatcher l'agent contre la spec**

Dispatcher l'agent `challenge-gherkin` avec ce prompt :

> « Spec fonctionnelle : `docs/init/01-specification.md`. Premier appel : nomme les tensions de testabilité et pose la première question. »

Expected: l'agent renvoie un objet JSON valide avec `tensions` non vide, exactement une entrée dans `questions` (2-4 options, 1ʳᵉ suffixée ` (Recommandé)`), `synthese: null`, `done: false`. Aucun texte hors du JSON.

Si la spec `docs/init/01-specification.md` n'existe pas, utiliser à la place tout fichier de spec présent sous `docs/init/` et adapter le chemin du prompt.

- [ ] **Step 3: Commit**

```bash
git add .claude/agents/challenge-gherkin.md
git commit -m "Ajoute l'agent challenge-gherkin"
```

---

### Task 4: Skill + agent `redaction-gherkin`

Le rédacteur : reçoit la synthèse (scénarios tranchés) et écrit le fichier `.feature`.

**Files:**
- Create: `.claude/skills/redaction-gherkin/SKILL.md`
- Create: `.claude/agents/redaction-gherkin.md`

**Interfaces:**
- Consumes: `synthese` produite par `challenge-gherkin` (Task 2/3) = `{ feature, scenarios[], risques[] }`.
- Produces: fichier `docs/init/scenarios/<sujet>.feature` ; récap JSON `{ "path": "<chemin>", "features": <int>, "scenarios": <int>, "notes": "<bref>" }`. Consommé par la command `make-gherkin` (Task 5).

- [ ] **Step 1: Écrire le fichier skill**

Create `.claude/skills/redaction-gherkin/SKILL.md` :

````markdown
---
name: redaction-gherkin
description: À utiliser pour écrire un fichier Gherkin (.feature) à partir de scénarios déjà challengés en testabilité — produit une Feature avec des Scenario numérotés en continu (nominal/limite/erreur), Given/When/Then, et Scenario Outline + Examples pour les données. Fonctionnel uniquement.
---

# Rédaction Gherkin

## Vue d'ensemble

Produire un fichier `.feature` Gherkin pauvre en prose et riche en **scénarios
testables**. La sortie a une forme fixe — remplis les scénarios fournis, n'invente
pas de comportement.

**Principe central :** fonctionnel uniquement. Aucun détail technique. Chaque
scénario décrit un comportement métier observable.

## Quand l'utiliser

- Après une passe `challenge-gherkin`, pour écrire les scénarios tranchés.
- Mise à jour d'un `.feature` existant quand le périmètre des scénarios change.

## Le contrat de sortie

Écris le fichier avec cette structure :

1. `Feature: <titre>` suivi de 1-3 lignes de description (la valeur métier).
2. `Background:` (optionnel) — les `Given` partagés par tous les scénarios.
3. Un bloc par scénario, **numérotation continue à travers tout le fichier** :
   - `Scenario N: <titre>` pour un cas simple.
   - `Scenario Outline: N: <titre>` + bloc `Examples:` pour un cas piloté par les données.
   - Étapes `Given` / `When` / `Then`, enchaînées par `And` / `But`.
4. Catégorise chaque scénario par un tag de type au-dessus du `Scenario` :
   `@nominal`, `@limite` ou `@erreur` (catégorie de test, **pas** une référence à
   une règle).

### Règles de forme

- **Numérotation continue** : `Scenario 1`, `Scenario 2`… sans recommencer.
- Un `Then` doit être **observable** : un résultat constatable, jamais « ça marche ».
- **Fonctionnel uniquement** : pas de table SQL, pas d'endpoint, pas de classe.
- Données concrètes dans les `Examples:` quand le scénario varie selon les valeurs.

Exemple :

```gherkin
Feature: Attribution des gardes
  Répartir équitablement les gardes entre les soignants du foyer.

  Background:
    Given un foyer avec 3 soignants actifs

  @nominal
  Scenario 1: Attribution d'une garde libre
    Given une garde du 24/06 non attribuée
    When le parent attribue la garde à Alice
    Then la garde du 24/06 affiche Alice comme responsable

  @limite
  Scenario 2: Attribution quand tous les soignants sont déjà au maximum
    Given chaque soignant a atteint son quota hebdomadaire
    When le parent tente d'attribuer une garde supplémentaire
    Then l'attribution est refusée et le quota dépassé est signalé
```

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent**, il reçoit dans son prompt : le
chemin du `.feature` à écrire et la `synthese` tranchée (`feature`, `scenarios[]`
avec given/when/then et `type`, `risques[]`). Il **écrit le fichier** directement,
puis renvoie au thread principal **uniquement** :

```json
{ "path": "docs/init/scenarios/attribution-gardes.feature", "features": 1, "scenarios": 7, "notes": "…" }
```

Restreins le périmètre d'écriture au chemin fourni — n'écris nulle part ailleurs.

## Erreurs fréquentes

- **Fuite technique** — « insère en base », « via l'API » → ce n'est pas un comportement métier. Coupe.
- **Recommencer la numérotation** — casse la lisibilité ; garde-la continue.
- **`Then` non observable** — « ça fonctionne » n'est pas vérifiable. Exige une sortie constatable.
- **Inventer des scénarios** — n'écris que ce que la `synthese` a tranché ; signale les manques dans `notes`.
````

- [ ] **Step 2: Écrire le fichier agent**

Create `.claude/agents/redaction-gherkin.md` :

````markdown
---
name: redaction-gherkin
description: Écrit ou met à jour un fichier Gherkin (.feature) en mode orchestré. Reçoit le chemin cible et la synthèse des scénarios (feature, scenarios given/when/then typés, risques), écrit le fichier au format Gherkin à numérotation continue, et renvoie un récapitulatif JSON. Dispatché par la command /make-gherkin après la passe de challenge.
tools: Read, Write, Edit, Glob
---

Tu es l'agent de rédaction Gherkin. Tu appliques le skill `redaction-gherkin`,
section **« Mode agent (orchestré) »**.

## Déroulé

1. Lis le chemin cible fourni s'il existe déjà (mise à jour) ; sinon, crée-le
   (et le dossier `docs/init/scenarios/` si besoin).
2. Produis le `.feature` : `Feature` + description, `Background` optionnel, puis un
   bloc par scénario de la `synthese`, **numérotation continue** à partir de 1.
3. Tag de type au-dessus de chaque `Scenario` : `@nominal` / `@limite` / `@erreur`.
   Utilise `Scenario Outline` + `Examples:` quand le scénario varie selon des données.
4. Fonctionnel uniquement — aucun choix technique. Un `Then` doit être observable.
5. **Écris uniquement le fichier au chemin fourni.** N'écris nulle part ailleurs.

## Sortie

**Uniquement** le JSON récapitulatif :

```json
{ "path": "<chemin écrit>", "features": <n>, "scenarios": <n>, "notes": "<bref>" }
```

Aucun texte autour.
````

- [ ] **Step 3: Smoke test — dispatcher le rédacteur avec une synthèse factice**

Dispatcher l'agent `redaction-gherkin` avec une `synthese` minimale en dur :

> Chemin cible : `docs/init/scenarios/_smoke.feature`. synthese : `{ "feature": "Smoke", "scenarios": [ { "id": 1, "titre": "Cas nominal", "type": "nominal", "given": "un état initial", "when": "une action", "then": "un résultat observable" } ], "risques": [] }`. Écris le fichier et renvoie le récap JSON.

Expected: le fichier `docs/init/scenarios/_smoke.feature` est créé avec `Feature: Smoke`, un `@nominal` + `Scenario 1:` + Given/When/Then ; l'agent renvoie `{ "path": "...", "features": 1, "scenarios": 1, "notes": "..." }` et rien d'autre.

- [ ] **Step 4: Supprimer le fichier smoke**

```bash
git rm --ignore-unmatch docs/init/scenarios/_smoke.feature 2>/dev/null; rm -f docs/init/scenarios/_smoke.feature
```

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/redaction-gherkin/SKILL.md .claude/agents/redaction-gherkin.md
git commit -m "Ajoute le skill et l'agent redaction-gherkin"
```

---

### Task 5: Command `make-gherkin`

La command d'orchestration. Miroir de `.claude/commands/spec.md`.

**Files:**
- Create: `.claude/commands/make-gherkin.md`

**Interfaces:**
- Consumes: agents `challenge-gherkin` (Task 3) et `redaction-gherkin` (Task 4).
- Produces: la slash-command `/make-gherkin`.

- [ ] **Step 1: Écrire le fichier command**

Create `.claude/commands/make-gherkin.md` :

````markdown
---
description: Produit un fichier Gherkin à scénarios numérotés à partir de la spec fonctionnelle — challenge la testabilité (via agent) puis rédige le .feature (via agent).
argument-hint: "[sujet ou feature à scénariser] (optionnel)"
---

# /make-gherkin — Génération de scénarios Gherkin orchestrée

Orchestration par **agents** : tu (thread principal) ne fais que dispatcher les
agents et **poser les questions via `AskUserQuestion`**. Les agents ne posent
jamais les questions eux-mêmes.

Sujet (optionnel) : $ARGUMENTS

## Déroulé

1. **Contexte.** Repère la spec fonctionnelle (`docs/init/01-specification.md` ou
   le fichier pertinent sous `docs/init/`) à passer aux agents.

2. **Challenge testabilité (agent + round-trip) :**
   - Dispatche l'agent `challenge-gherkin` avec le chemin de la spec + le sujet.
   - Il renvoie un JSON `{ tensions, questions, synthese, done }`.
   - Affiche les `tensions`, puis rends **chaque** `questions[]` via
     `AskUserQuestion` (option recommandée en premier).
   - Renvoie les réponses à l'agent via `SendMessage` (continue le même agent).
   - Répète tant que `done` est faux.

3. **Validation.** Quand `done: true`, présente la `synthese` (liste des scénarios
   à couvrir : nominaux, limites, erreurs + risques) et demande l'accord avant de
   rédiger.

4. **Rédaction (agent) :**
   - Détermine le chemin cible : `docs/init/scenarios/<sujet-kebab>.feature`.
   - Dispatche l'agent `redaction-gherkin` avec le chemin cible + la `synthese`.
   - Il écrit le fichier et renvoie `{ path, features, scenarios, notes }`.

5. **Propagation.** Si un README/roadmap référence les scénarios, mets-le à jour ;
   garde une seule source de vérité.

6. **Commit.** Propose un commit (sans pousser sauf demande explicite).

## Notes

- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par les agents.
- Une question à la fois pendant le challenge — pas de rafale.
- Fonctionnel uniquement dans le Gherkin : aucun choix technique.
- Numérotation des scénarios **continue** à travers le fichier.
- Entrée attendue : une spec déjà produite par `/spec` (challenge `brainstorm` →
  `redaction-spec`).
````

- [ ] **Step 2: Vérifier la command**

Lire `.claude/commands/make-gherkin.md` et confirmer : frontmatter `description` + `argument-hint`, dispatch des deux agents `challenge-gherkin` et `redaction-gherkin`, mention que `AskUserQuestion` est appelé par le thread principal.

Run: `git grep -n "challenge-gherkin\|redaction-gherkin" .claude/commands/make-gherkin.md`
Expected: au moins une référence à chacun des deux agents.

- [ ] **Step 3: Commit**

```bash
git add .claude/commands/make-gherkin.md
git commit -m "Ajoute la command /make-gherkin"
```

---

### Task 6: Vérification de bout en bout

Dry-run du flow complet pour confirmer l'intégration des cinq unités.

**Files:**
- (aucun fichier de production ; produit éventuellement un `.feature` réel conservé si pertinent)

**Interfaces:**
- Consumes: command `make-gherkin` + agents des Tasks 3/4.

- [ ] **Step 1: Lancer le flow sur la spec réelle**

Exécuter `/make-gherkin` (ou simuler son déroulé) sur la spec `docs/init/`. Vérifier :
- L'agent `challenge-gherkin` nomme des tensions de testabilité puis pose une question à la fois.
- Les questions remontent bien au thread principal (rendues via `AskUserQuestion`), jamais posées par l'agent.
- À `done: true`, la `synthese` liste des scénarios typés.
- L'agent `redaction-gherkin` écrit un `.feature` sous `docs/init/scenarios/` avec numérotation continue et tags `@nominal/@limite/@erreur`.

Expected: un `.feature` cohérent, fonctionnel, sans détail technique.

- [ ] **Step 2: Confirmer l'absence de référence cassée**

Run: `git grep -n "challenge-po"`
Expected: aucune sortie.

Run: `git grep -ln "make-gherkin\|challenge-gherkin\|redaction-gherkin"`
Expected: liste incluant la command, les deux skills et les deux agents.

- [ ] **Step 3: Commit (si un .feature de démonstration est conservé)**

```bash
git add docs/init/scenarios
git commit -m "Ajoute un .feature de démonstration généré par /make-gherkin"
```

---

## Self-Review

**Spec coverage** (design `2026-06-22-make-gherkin-flow-design.md`) :
- Command `make-gherkin` → Task 5. ✅
- skill+agent `challenge-gherkin` (testabilité) → Tasks 2, 3. ✅
- skill+agent `redaction-gherkin` → Task 4. ✅
- Sortie `docs/init/scenarios/`, numérotation continue → Tasks 4, 5 (format). ✅
- Rename `challenge-po → brainstorm` (5 fichiers) → Task 1. ✅
- Round-trip questions via thread principal → contrats agents Tasks 3, 5. ✅
- Fonctionnel uniquement → contrainte globale + skills. ✅
- Hors scope (agent `tdd-implement`) → non planifié ici. ✅

**Placeholder scan** : contenu complet pour chaque fichier (skills, agents, command). Aucun « TBD/TODO ». ✅

**Type consistency** : contrat `synthese = { feature, scenarios[ {id,titre,type,given,when,then} ], risques[] }` identique entre challenge-gherkin (Tasks 2/3) et redaction-gherkin (Task 4). Récap rédacteur `{ path, features, scenarios, notes }` cohérent entre skill et agent. ✅
