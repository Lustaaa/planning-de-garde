---
description: Implémente un fichier de scénarios make-gherkin (docs/scenarios/<sujet>.md) en BDD + TDD, via deux agents — tdd-analyse (produit le markdown de suivi docs/scenarios/<sujet>.suivi.md) puis tdd-auto (implémente UN scénario à la fois, met à jour le suivi en direct, checkpoint).
argument-hint: "[sujet] [#scénario] (optionnels)"
---

# /3-tdd-implement — Analyse puis implémentation BDD + TDD (2 agents)

**Tout le travail vit dans deux subagents.** Toi (thread principal) tu es un
**relais pur** : tu ne lis ni le fichier de scénarios ni le code, tu n'écris ni test
ni implémentation. Tu dispatches les agents, relaies leurs questions via
`AskUserQuestion`, et présentes les checkpoints. Objectif : **garder le contexte du
main propre** — tout le raisonnement reste dans les agents ; **toi tu suis
l'avancement dans le markdown de suivi** `docs/scenarios/<sujet>.suivi.md`.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip. Communication = `SendMessage`
> (main → agent) et valeur de retour de l'agent (agent → main).

Arguments (optionnels) : $ARGUMENTS — sujet (fichier de scénarios) et/ou numéro de
scénario.

## Déroulé

1. **Contexte.** Repère le **chemin** du fichier de scénarios
   `docs/scenarios/NN-<sujet>.md`. **Ne le lis pas toi-même** — les agents s'en
   chargent.

2. **Analyse (agent `tdd-analyse`).** Dispatche-le avec le chemin du fichier. Garde
   son `agentId`.
   - **Fallback** : type absent du registre → `general-purpose` avec « applique le
     skill `tdd-implement` en agent d'analyse seule (cf. agent tdd-analyse) » + le
     chemin. Ne bascule **pas** en inline.
   - S'il renvoie `{ "type": "question", … }` (ambiguïté métier / scaffolding), rends
     la `question` **telle quelle** via `AskUserQuestion`, relaie la réponse **brute**
     via `SendMessage`. Répète.
   - Sinon `{ "type": "analyse", "suivi": …, "scenarios": n, "tests": … }` : le
     **markdown de suivi est écrit**.

3. **Validation du plan.** Présente brièvement le suivi (nb de scénarios, total de
   tests, scaffolding/doublons signalés) et demande l'accord d'implémenter via
   `AskUserQuestion`. C'est le tableau de bord que l'utilisateur suivra.

4. **Implémentation (agent `tdd-auto`), boucle par scénario.** Dispatche-le avec le
   chemin du suivi + le scénario cible (celui demandé, sinon le 1er non terminé).
   Garde son `agentId`.
   - **Fallback** : type absent → `general-purpose` avec « applique le skill
     `tdd-implement` en agent autonome (cf. agent tdd-auto) » + le chemin du suivi et
     le scénario cible. Ne bascule **pas** en inline.
   - `{ "type": "question", … }` → `AskUserQuestion` (telle quelle) → `SendMessage`
     (réponse brute). Répète.
   - `{ "type": "result", … }` → l'agent a livré **un** scénario (RED → GREEN →
     commit, suivi mis à jour).

5. **Checkpoint.** Présente le récap **verbatim** depuis l'agent (cycle, fichiers,
   état de la suite, scénario `@vert` + cellules du suivi). Demande l'accord pour
   **enchaîner le scénario suivant** (`next_scenario`).

6. **Boucle.** Si l'utilisateur valide, relance `tdd-auto` pour le scénario suivant
   (même `agentId`). Sinon, stoppe.

## Notes

- **Relais pur** : si tu te surprends à analyser, écrire un test, lire le code ou
  rédiger le suivi toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par les agents.
- **Deux artefacts de suivi en parallèle** : le markdown `…suivi.md` (tableau de bord
  des tests unitaires, mis à jour en direct par `tdd-auto`) et les tags de cycle
  `@rouge`/`@vert` dans le fichier de scénarios source (état du test d'acceptation).
- **Un scénario Gherkin par run de `tdd-auto`** — red-green-commit, puis checkpoint.
  Pas d'implémentation en bloc.
- Le test d'acceptation **doit** échouer d'abord (rouge), sinon il n'observe rien.
- Relance la suite complète avant chaque commit (non-régression).
- Entrée attendue : un fichier produit par `make-gherkin`.
