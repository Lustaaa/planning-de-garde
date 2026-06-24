---
description: Implémente un fichier de scénarios make-gherkin (docs/sprints/<sujet>.md) en BDD + TDD, via plusieurs agents — tdd-analyse (produit le dossier de suivi docs/sprints/<sujet>/ : 00-sprint<NN>-suivi.md + un fichier par scénario), tdd-auto (implémente UN scénario à la fois, checkpoint), ihm-builder (phase IHM finale), puis validation-visuelle (gate de livraison impératif de fin de sprint : back+IHM up, retours préparé).
argument-hint: "[sujet] [#scénario] (optionnels)"
---

# /3-tdd-implement — Analyse puis implémentation BDD + TDD (2 agents)

**Tout le travail vit dans deux subagents.** Toi (thread principal) tu es un
**relais pur** : tu ne lis ni le fichier de scénarios ni le code, tu n'écris ni test
ni implémentation. Tu dispatches les agents, relaies leurs questions via
`AskUserQuestion`, et présentes les checkpoints. Objectif : **garder le contexte du
main propre** — tout le raisonnement reste dans les agents ; **toi tu suis
l'avancement dans le tableau de bord** `docs/sprints/<sujet>/00-sprint<NN>-suivi.md`
(`<NN>` = numéro du sprint = préfixe 2 chiffres du dossier, ex. `00-sprint02-suivi.md`).

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip. Communication = `SendMessage`
> (main → agent) et valeur de retour de l'agent (agent → main).

Arguments (optionnels) : $ARGUMENTS — sujet (fichier de scénarios) et/ou numéro de
scénario.

## Déroulé

1. **Contexte.** Repère le **chemin** du fichier de scénarios
   `docs/sprints/NN-<sujet>.md`. **Ne le lis pas toi-même** — les agents s'en
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
     **dossier de suivi est écrit** (`docs/sprints/<sujet>/` : `00-sprint<NN>-suivi.md` + un
     fichier par scénario). `tdd-analyse` scaffolde aussi, dans le même dossier, deux
     templates vides : `99-sprint<NN>-besoins-fin-itération.md` (backlog produit, rempli
     plus tard par `/4-retours`) et `99-sprint<NN>-retours.md` (journal méthode, appendé
     par le thread principal pendant le sprint — voir Notes).

3. **Validation du plan.** Présente brièvement le suivi (nb de scénarios, total de
   tests, scaffolding/doublons signalés) et demande l'accord d'implémenter via
   `AskUserQuestion`. C'est le tableau de bord que l'utilisateur suivra.

4. **Implémentation (agent `tdd-auto`), boucle par scénario.** Dispatche-le avec le
   chemin du dossier de suivi (`docs/sprints/<sujet>/`) + le scénario cible (celui
   demandé, sinon le 1er non terminé). Garde son `agentId`.
   - **Fallback** : type absent → `general-purpose` avec « applique le skill
     `tdd-implement` en agent autonome (cf. agent tdd-auto) » + le chemin du dossier de
     suivi et le scénario cible. Ne bascule **pas** en inline.
   - `{ "type": "question", … }` → `AskUserQuestion` (telle quelle) → `SendMessage`
     (réponse brute). Répète.
   - `{ "type": "result", … }` → l'agent a livré **un** scénario (RED → GREEN →
     commit, suivi mis à jour).

5. **Récap (sans blocage).** Présente le récap **verbatim** depuis l'agent (cycle,
   fichiers, état de la suite, scénario `@vert` + cellules du suivi). **N'appelle pas
   `AskUserQuestion`** : le sprint est mené de façon intégrale, on enchaîne
   automatiquement le scénario suivant. (L'utilisateur garde la main : il peut
   interrompre à tout moment.)

6. **Boucle automatique.** Relance `tdd-auto` pour le scénario suivant (même
   `agentId`, `next_scenario`), sans demander confirmation, jusqu'à ce que **tous les
   scénarios soient `✅ GREEN`** dans `00-sprint<NN>-suivi.md`. **La boucle se suspend dès qu'un
   agent renvoie `{ "type": "question", … }`** — rends-la **telle quelle** via
   `AskUserQuestion`, relaie la réponse brute via `SendMessage`, puis reprends la boucle.
   `tdd-auto` pose notamment une question sur **early green inattendu** (obligatoire) et
   **peut** en poser sur un **problème d'implémentation** détecté. La boucle stoppe aussi
   si l'utilisateur interrompt.

7. **Phase IHM finale (agent `ihm-builder`).** **Uniquement quand tous les scénarios
   sont `✅ GREEN`** dans le `00-sprint<NN>-suivi.md` (backend complet). Propose la construction de
   l'IHM via `AskUserQuestion` ; si l'utilisateur valide, dispatche `ihm-builder` avec
   le chemin du fichier de scénarios + le dossier de suivi.
   - **Fallback** : type absent → `general-purpose` avec « applique la phase IHM finale
     du skill `tdd-implement` (cf. agent ihm-builder) » + les chemins. Pas d'inline.
   - `{ "type": "question", … }` → `AskUserQuestion` (telle quelle) → `SendMessage`
     (réponse brute). Répète.
   - `{ "type": "ihm", … }` → l'IHM est construite (vues + SignalR réel, build + suite
     verts, commit). Présente le récap **verbatim** + la commande de lancement
     (`pwsh .claude/skills/run/scripts/run.ps1`).

8. **Validation visuelle finale (agent `validation-visuelle`) — IMPÉRATIVE.** **Une
   seule fois**, juste après la phase IHM du sprint. Dispatche `validation-visuelle` avec
   le chemin du dossier de sprint (`docs/sprints/<sujet>/`). Garde son `agentId`.
   - **Fallback** : type absent → `general-purpose` avec « applique le rôle de l'agent
     `validation-visuelle` (gate de livraison de fin de sprint) » + le chemin. Pas d'inline.
   - `{ "type": "question", … }` (gate prématuré) → `AskUserQuestion` → `SendMessage`.
   - `{ "type": "probleme", … }` (build/suite rouge) → présente le constat ; la livraison
     est cassée, à réparer par un `/3-tdd-implement` ciblé avant de conclure le sprint.
   - `{ "type": "validation", … }` → **lance l'app** toi-même (thread durable) en tâche de
     fond via `pwsh .claude/skills/run/scripts/run.ps1`, puis **relaie le `message`
     verbatim** : back + IHM up, routes à tester, et le **fichier de retours préparé**
     (`retours_path`). C'est un **gate** : le sprint ne se conclut pas sans cette
     notification. L'utilisateur teste visuellement, remplit le retours, puis lance
     `/4-retours`.

## Notes

- **Relais pur** : si tu te surprends à analyser, écrire un test, lire le code ou
  rédiger le suivi toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par les agents.
- **Deux artefacts de suivi en parallèle** : le dossier `docs/sprints/<sujet>/`
  (`00-sprint<NN>-suivi.md` tableau de bord avec compte `X/N` + un `NN-slug.md` par scénario, mis à
  jour en direct par `tdd-auto`) et les tags de cycle `@rouge`/`@vert` dans le fichier
  de scénarios source (état du test d'acceptation).
- **Un scénario Gherkin par run de `tdd-auto`** — red-green-commit, puis récap et
  enchaînement automatique du scénario suivant. Pas d'implémentation en bloc, mais
  pas de blocage `AskUserQuestion` entre scénarios : le sprint est mené intégralement.
- Le test d'acceptation **doit** échouer d'abord (rouge), sinon il n'observe rien.
- Relance la suite complète avant chaque commit (non-régression).
- **Backend d'abord, IHM en fin** : les scénarios s'arrêtent à la frontière de
  l'Application (use cases + ports doublés) ; l'IHM Blazor + SignalR réel sont une
  **phase finale** (`ihm-builder`, étape 7) après le dernier scénario vert.
- **Lanceur** : au scaffolding, `tdd-auto` génère `.claude/skills/run/` (script +
  skill `/run`) pour lancer l'appli d'une commande.
- Entrée attendue : un fichier produit par `make-gherkin`.
- **Journal méthode** : pendant le sprint, le thread principal consigne dans
  `docs/sprints/<sujet>/99-sprint<NN>-retours.md` chaque retour à la volée du PO sur un
  agent/skill/command (cible + retour + décision), pour traitement par `retro-sprint` en
  fin de sprint. À ne pas confondre avec le backlog produit
  `99-sprint<NN>-besoins-fin-itération.md` ni le retours produit `NN-retours.md`.
- **Clôture de sprint = gate visuel impératif** (étape 8, `validation-visuelle`) : le
  sprint ne se conclut qu'après la notification « back + IHM up + retours préparé ».
  L'utilisateur teste l'IHM, remplit le `NN-retours.md`, puis enchaîne `/4-retours`
  (besoins + archivage) → `/5-consolidation` (nouvelle spec) → `/2-make-gherkin`.
