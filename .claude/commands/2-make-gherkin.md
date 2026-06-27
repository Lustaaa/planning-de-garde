---
description: Transforme la spec fonctionnelle (docs/) en un fichier d'analyse technique légère + scénarios Gherkin numérotés, via l'agent make-gherkin (round-trip de questions + écriture).
argument-hint: "[sujet ou feature à scénariser] (optionnel)"
---

# /2-make-gherkin — Analyse & scénarios Gherkin

**Tout le travail vit dans le subagent `make-gherkin`.** Toi (thread principal) tu
es un **orchestrateur** : tu ne lis pas la spec, tu ne nommes pas les tensions, tu ne
calcules ni scénarios ni synthèse. Tu te bornes à : dispatcher l'agent, **router ses
questions vers le chef de projet** (escalade PO via `AskUserQuestion` seulement sur
G1/G2), lui renvoyer les réponses brutes via `SendMessage`, puis lui ordonner d'écrire.
Objectif : **garder le contexte du main propre** — tout le raisonnement reste chez
l'agent, et **le PO n'est sollicité que sur les portes essentielles**.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip : l'agent renvoie les questions,
> tu les poses, tu lui rends les réponses. La communication = `SendMessage`
> (main → agent) et la valeur de retour de l'agent (agent → main).

> **Protocole d'escalade — chef de projet (CP).** Tu ne poses plus directement au PO les
> questions de `make-gherkin` (périmètre, cas limites, valeurs observables…) — elles sont quasi
> toutes dérivables de la spec. Quand l'agent renvoie une `question`, **dispatche d'abord
> l'agent `chef-de-projet`** avec : la `question`, la **spec courante** résolue à l'étape 1
> (`currentSpec`), `docs/BACKLOG.md`, le palier d'autonomie (défaut `0 — conservateur`).
> - `{type:"decision",…}` → **affiche le `resume` du CP en une ligne** (`🧭 CP — <resume>`) pour
>   le suivi du PO (sans `AskUserQuestion`), puis **relaie la décision** à `make-gherkin` via
>   `SendMessage`.
> - `{type:"escalate", gate:"G1"|"G2", …}` → **seulement là** appelle `AskUserQuestion` (payload
>   riche du CP au-dessus des `options`). Renvoie la réponse brute à l'agent.
> - **Fallback** : type `chef-de-projet` absent → `general-purpose` + « applique le skill
>   `chef-de-projet` » + les mêmes entrées.

Sujet (optionnel) : $ARGUMENTS

## Déroulé

0. **Gate rétro (dur — avant tout).** Exécute
   `pwsh -NoProfile -File .claude/skills/retro-sprint/scripts/find-retro.ps1`.
   - Si `gateOpen=false` : le **dernier sprint clos** (`lastClosedSprint`) n'a **pas** de
     `98-retrospective.md`. **STOP** : ne démarre **pas** ce nouveau cycle. Préviens le PO
     qu'il faut d'abord lancer la **sprint retrospective** (`retro-sprint`, via
     `/6-cloture-sprint` étape 1) sur ce sprint. C'est le garde-fou d'amélioration continue :
     **un nouveau cycle make-gherkin ne démarre jamais tant que la rétro du sprint
     précédent n'a pas tourné.**
   - Si `gateOpen=true` : aucun sprint clos en attente de rétro → continue.

1. **Résous la spec courante (script) puis dispatch.** Exécute
   `pwsh -NoProfile -File .claude/skills/spec-consolidation/scripts/find-spec.ps1` et prends
   `currentSpec` (le plus grand `NN-specification.md`). **N'utilise jamais** un
   `01-`/`02-specification.md` en dur : cela provoque une **course** quand
   `/5-consolidation` écrit la version suivante en tâche de fond (l'agent partirait sur une
   spec périmée). Lance ensuite l'agent `make-gherkin` en lui passant **le chemin**
   `currentSpec` + le sujet `$ARGUMENTS`. **Ne lis pas la spec toi-même** — l'agent s'en
   charge. Garde son `agentId` pour tout le round-trip.
   - **Fallback** : si le type `make-gherkin` n'est pas dans le registre de la
     session (« Agent type not found »), dispatche `general-purpose` avec la consigne
     « applique le skill `make-gherkin`, section *Mode agent (orchestré)* » + le
     chemin de la spec. Même protocole de round-trip ensuite. Ne bascule **pas** en
     exécution inline dans le main.

2. **Boucle de challenge (relais).** À chaque retour, l'agent renvoie un JSON
   `{ tensions, questions, synthese, done }`. Tant que `done` est faux :
   - Pour **chaque** entrée de `questions[]`, applique le **Protocole d'escalade CP** ci-dessus :
     dispatche d'abord `chef-de-projet` ; n'appelle `AskUserQuestion` que sur une `escalate`
     (G1/G2), en passant alors l'objet `question` du CP **tel quel** (ne le reformule pas).
   - Au plus **une ligne** de contexte pour le user si l'agent fournit des `tensions`
     (les énumérer en bref) ; sinon, n'écris rien.
   - Renvoie les réponses **brutes** (décision du CP ou réponse du PO) à l'agent via
     `SendMessage` (même `agentId`).
   - Recommence. **N'analyse pas** le contenu, **ne devine pas** la question suivante.

3. **Validation (CP).** Quand `done: true`, fais valider l'écriture par le **chef de projet** :
   dispatche `chef-de-projet` avec la `synthese`. S'il renvoie `{type:"decision"}` (rien de
   bloquant : les scénarios dérivent de la spec actée) → **ordonne d'écrire sans déranger le
   PO**. S'il renvoie `{type:"escalate", gate:"G1"|"G2"}` (un arbitrage métier ou un cap à
   fixer subsiste) → appelle `AskUserQuestion` (payload riche), puis écris. Affiche la
   `synthese` verbatim soit avec la décision du CP, soit avec l'escalade.

4. **Écriture (même agent).** À l'accord, calcule le chemin cible numéroté :
   `docs/sprints/NN-<sujet-kebab>.md` où `NN` = `(plus grand préfixe NN
   existant dans le dossier) + 1` sur 2 chiffres (à défaut `01`) — un simple `Glob`
   du dossier suffit. `SendMessage` l'ordre d'écrire avec ce chemin. L'agent écrit le
   fichier (au format imposé du skill) et renvoie `{ path, scenarios, notes }`.

5. **Commit (automatique).** Commite le plan Gherkin (sans pousser). Pas de demande
   d'accord : le commit est local et réversible.

6. **`/clear` (après le plan).** Le `/clear` de fin de cycle se fait **APRÈS** la
   rédaction du plan Gherkin — c.-à-d. une fois `docs/sprints/NN-<sujet>.md` écrit (étape 4),
   jamais avant. Garder le contexte du cadrage disponible jusqu'à l'écriture du plan
   (retour PO adopté au sprint 03).

## Notes

- **Relais pur** : si tu te surprends à lire la spec, lister des scénarios ou rédiger
  une synthèse toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par l'agent ;
  c'est l'unique chose que l'agent ne peut pas faire.
- Une question à la fois pendant le challenge — pas de rafale.
- **Formatage imposé du fichier** (cf. skill, « Format du fichier de sortie ») :
  `Feature:` en intro hors fence, puis **un scénario = un en-tête
  `### Scenario N — <titre> ` + tag inline en code + son propre bloc
  ` ```gherkin `**. Numérotation continue ; chaque scénario autonome (pas de
  `Background:`).
- L'analyse technique du fichier reste **légère** (amorce d'implémentation).
- Entrée attendue : une spec déjà produite par `/1-spec`.
- Ce fichier sera l'entrée de la future pipeline `tdd-implement`.
