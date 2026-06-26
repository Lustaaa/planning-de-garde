---
description: Implémente un fichier de scénarios make-gherkin (docs/sprints/<sujet>.md) en BDD + TDD, via plusieurs agents — tdd-analyse (produit le dossier de suivi docs/sprints/<sujet>/ : 00-sprint<NN>-suivi.md + un fichier par scénario), tdd-auto (implémente UN scénario à la fois, checkpoint), ihm-builder (phase IHM finale), puis validation-visuelle (gate de livraison impératif de fin de sprint : back+IHM up, retours préparé).
argument-hint: "[sujet] [#scénario] (optionnels)"
---

# /3-tdd-implement — Analyse puis implémentation BDD + TDD (2 agents)

**Tout le travail vit dans deux subagents.** Toi (thread principal) tu es un
**orchestrateur** : tu ne lis ni le fichier de scénarios ni le code, tu n'écris ni test
ni implémentation. Tu dispatches les agents, **routes leurs questions vers le chef de
projet** (escalade PO seulement sur les portes), et présentes les checkpoints. Objectif :
**garder le contexte du main propre** — tout le raisonnement reste dans les agents ; **toi
tu suis l'avancement dans le tableau de bord** `docs/sprints/<sujet>/00-sprint<NN>-suivi.md`
(`<NN>` = numéro du sprint = préfixe 2 chiffres du dossier, ex. `00-sprint02-suivi.md`).

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip. Communication = `SendMessage`
> (main → agent) et valeur de retour de l'agent (agent → main).

> **Protocole d'escalade — chef de projet (CP).** Quand `tdd-analyse` ou `tdd-auto` renvoie
> `{type:"question", …}`, **dispatche d'abord l'agent `chef-de-projet`** avec : la `question`,
> la **spec courante** (`docs/NN-specification.md`, la plus récente), le dossier de sprint, le
> palier d'autonomie (défaut `0 — conservateur`).
> - `{type:"decision",…}` → **relaie la décision** à l'agent dev via `SendMessage`. **Pas**
>   d'`AskUserQuestion`. (Couvre le **scaffolding**, le **routage backend/IHM**, l'**early-green
>   attendu**, un **problème d'implémentation** tranchable par la spec/convention.)
> - `{type:"escalate", gate:"G1", …}` → appelle `AskUserQuestion` (payload riche du CP).
> - **Fallback** : type `chef-de-projet` absent → `general-purpose` + « applique le skill
>   `chef-de-projet` ».
>
> **⚠️ Deux exceptions câblées DIRECT PO — ne passent JAMAIS par le CP :**
> 1. **G4 — early-green INATTENDU** (`tdd-auto`, `type:"question"` signalant un early green non
>    anticipé) → `AskUserQuestion` **directement au PO**. C'est une porte essentielle.
> 2. **G3 — validation visuelle** (étape 8, `validation-visuelle`) → notification/gate **direct
>    PO**.
> Le **routage IHM** (refus d'un scénario IHM par `tdd-auto`) reste mécanique : re-dispatch vers
> `ihm-builder` (pas besoin du CP ni du PO).

> **Agents requis dans le registre.** Cette command dispatche `tdd-analyse`, `tdd-auto`,
> `ihm-builder` et `validation-visuelle`. Les fichiers `.claude/agents/ihm-builder.md` et
> `.claude/agents/validation-visuelle.md` existent, mais s'ils ne sont **pas chargés dans
> le registre de la session**, le dispatch tombe en **fallback `general-purpose`** à chaque
> fois (observé aux sprints 03 et 04). Le registre n'étant **pas pilotable depuis le dépôt**,
> ce fallback est le **régime nominal documenté** quand ces types ne sont pas chargeables :
> dispatcher `general-purpose` avec « applique le skill … (cf. agent ihm-builder /
> validation-visuelle) » n'est **pas une dégradation à corriger**, c'est le mode attendu.
> (La table des agents du `README-claude.md` les liste tous, `retro-sprint` inclus.)

> **`/clear` après le plan.** Si un `/clear` est fait, c'est **après** l'écriture du plan
> Gherkin de `/2` (`docs/sprints/NN-<sujet>.md`), jamais avant — retour PO adopté au sprint 03.

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
   - S'il renvoie `{ "type": "question", … }` (ambiguïté métier / scaffolding / axe
     backend-IHM), applique le **Protocole d'escalade CP** (dispatch `chef-de-projet` ;
     relaie sa `decision`, ou `AskUserQuestion` sur une `escalate` G1), relaie la réponse
     **brute** via `SendMessage`. Répète.
   - Sinon `{ "type": "analyse", "suivi": …, "scenarios": n, "tests": … }` : le
     **dossier de suivi est écrit** (`docs/sprints/<sujet>/` : `00-sprint<NN>-suivi.md` + un
     fichier par scénario). `tdd-analyse` scaffolde aussi, dans le même dossier, deux
     templates vides : `99-sprint<NN>-besoins-fin-itération.md` (backlog produit, rempli
     plus tard par `/4-retours`) et le **fichier unifié** `99-sprint<NN>-retours.md` —
     section `# Retours produit (PO)` (remplie par le PO après le gate) PLUS sections
     `# Méthode (agents)` + `## IA` (appendées par le thread principal pendant le sprint —
     voir Notes).

3. **Validation du plan.** Présente brièvement le suivi (nb de scénarios, total de
   tests, scaffolding/doublons signalés, **et le routage backend vs IHM** : quels
   scénarios sont étiquetés `🖥️ scénario IHM` → `ihm-builder`, lesquels sont backend →
   `tdd-auto`) et demande l'accord d'implémenter via `AskUserQuestion`. C'est le tableau
   de bord que l'utilisateur suivra.

   > **Routage des scénarios (Cause B/C).** `tdd-analyse` étiquette chaque scénario sur
   > l'axe **backend vs IHM**. Le **niveau du test d'acceptation suit le niveau du
   > symptôme** : scénario **backend** (domaine/Application) → `tdd-auto` (test
   > handler/intégration à la frontière Application) ; scénario **IHM**
   > (interactivité, render mode, `@onclick`/`@bind`, DI réelle, SignalR) → `ihm-builder`
   > **piloté par un test rouge de niveau RUNTIME** (E2E / hôte réel), **pas** un test
   > bUnit composant (qui « ment au vert » faute d'attraper un render mode manquant).
   > `ihm-builder` n'est donc **plus seulement** la phase finale de construction : il mène
   > aussi des cycles **RED→GREEN** sur les scénarios IHM.

4. **Implémentation, boucle par scénario — routée backend vs IHM.** Pour le scénario
   cible (celui demandé, sinon le 1er non terminé), regarde son étiquette dans
   `00-sprint<NN>-suivi.md` :
   - **Scénario backend** → dispatche **`tdd-auto`** avec le chemin du dossier de suivi
     (`docs/sprints/<sujet>/`) + le scénario cible. Garde son `agentId`.
   - **Scénario IHM** (`🖥️ scénario IHM`) → dispatche **`ihm-builder`** (cycle
     **RED→GREEN runtime**, pas la phase finale) avec le dossier de suivi + le scénario
     cible.
   - **Fallback** : type absent → `general-purpose` avec « applique le skill
     `tdd-implement` en agent autonome (cf. agent tdd-auto pour un scénario backend, ou
     agent ihm-builder pour un scénario IHM mené RED→GREEN runtime) » + le chemin du
     dossier de suivi et le scénario cible. Ne bascule **pas** en inline.
   - `{ "type": "question", … }` → applique le **Protocole d'escalade CP** (dispatch
     `chef-de-projet` ; relaie sa `decision`, ou `AskUserQuestion` sur une `escalate` G1).
     **Exception G4** : un **early-green inattendu** va **direct au PO** (pas par le CP).
     **Si `tdd-auto` refuse un scénario comme IHM** (question de routage), **re-dispatche le
     scénario vers `ihm-builder`** (mécanique — ni CP ni PO) au lieu de forcer un test bUnit.
   - `{ "type": "result", … }` (tdd-auto) ou `{ "type": "ihm-scenario", … }`
     (ihm-builder) → l'agent a livré **un** scénario (RED → GREEN → commit, suivi mis à
     jour).

5. **Récap (sans blocage).** Présente le récap **verbatim** depuis l'agent (cycle,
   fichiers, état de la suite, scénario `@vert` + cellules du suivi). **N'appelle pas
   `AskUserQuestion`** : le sprint est mené de façon intégrale, on enchaîne
   automatiquement le scénario suivant. (L'utilisateur garde la main : il peut
   interrompre à tout moment.)

6. **Boucle automatique.** Relance l'agent **routé** (backend → `tdd-auto` ; IHM →
   `ihm-builder`) pour le scénario suivant (`next_scenario`), sans demander confirmation,
   jusqu'à ce que **tous les scénarios soient `✅ GREEN`** dans `00-sprint<NN>-suivi.md`.
   **La boucle se suspend dès qu'un agent renvoie `{ "type": "question", … }`** —
   applique le **Protocole d'escalade CP**, relaie la réponse brute (décision CP ou PO) via
   `SendMessage`, puis reprends la boucle. Routage des questions de `tdd-auto` :
   - **early-green inattendu** (obligatoire) → **direct PO** (porte G4), **pas** le CP ;
   - **routage IHM** (refus d'un scénario IHM) → **mécanique**, re-dispatch vers `ihm-builder` ;
   - **scaffolding** / **problème d'implémentation** détecté → **CP** (qui tranche depuis la
     spec/convention, ou escalade G1 si c'est un vrai choix métier).
   La boucle stoppe aussi si l'utilisateur interrompt.

7. **Phase IHM finale (agent `ihm-builder`).** **Uniquement quand tous les scénarios
   sont `✅ GREEN`** dans le `00-sprint<NN>-suivi.md` (backend **et** scénarios IHM
   complets). Propose la construction de l'IHM restante (vues/écrans non encore couverts
   par un scénario IHM) via `AskUserQuestion` ; si l'utilisateur valide, dispatche
   `ihm-builder` en **mode construction** avec le chemin du fichier de scénarios + le
   dossier de suivi. (Les scénarios IHM déjà menés RED→GREEN à l'étape 4/6 ne sont pas
   refaits ici.)
   - **Fallback** : type absent → `general-purpose` avec « applique la phase IHM finale
     du skill `tdd-implement` (cf. agent ihm-builder) » + les chemins. Pas d'inline.
   - `{ "type": "question", … }` → applique le **Protocole d'escalade CP** (dispatch
     `chef-de-projet` ; relaie sa `decision`, ou `AskUserQuestion` sur une `escalate` G1) →
     `SendMessage` (réponse brute). Répète.
   - `{ "type": "ihm", … }` → l'IHM est construite (vues + SignalR réel, build + suite
     verts, commit). Présente le récap **verbatim** + la commande de lancement
     (`pwsh .claude/skills/run/scripts/run.ps1`).

8. **Validation visuelle finale (agent `validation-visuelle`) — IMPÉRATIVE.** **Une
   seule fois**, juste après la phase IHM du sprint. Dispatche `validation-visuelle` avec
   le chemin du dossier de sprint (`docs/sprints/<sujet>/`). Garde son `agentId`.
   - **Fallback** : type absent → `general-purpose` avec « applique le rôle de l'agent
     `validation-visuelle` (gate de livraison de fin de sprint) » + le chemin. Pas d'inline.
   - `{ "type": "question", … }` (gate prématuré) → **Protocole d'escalade CP** (un gate
     prématuré est une vérification de précondition : le CP tranche en général) → `SendMessage`.
   - `{ "type": "probleme", … }` (build/suite rouge) → présente le constat ; la livraison
     est cassée, à réparer par un `/3-tdd-implement` ciblé avant de conclure le sprint.
   - `{ "type": "validation", … }` → **lance l'app** toi-même (thread durable) en tâche de
     fond via `pwsh .claude/skills/run/scripts/run.ps1`, puis **relaie le `message`
     verbatim** : back + IHM up, routes à tester, et le **fichier de retours unifié préparé**
     (`retours_path` = `99-sprint<NN>-retours.md`, section `# Retours produit (PO)`). C'est
     un **gate** : le sprint ne se conclut pas sans cette notification. L'utilisateur teste
     visuellement, remplit la section produit, puis lance `/4-retours`.

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
- **Routage par niveau de symptôme** : un scénario **backend** s'arrête à la frontière
  de l'Application (use cases + ports doublés) et va à `tdd-auto` ; un scénario **IHM**
  (interactivité, render mode, `@onclick`/`@bind`, DI réelle, SignalR) va à
  `ihm-builder`, **piloté par un test rouge de niveau runtime** (E2E / hôte réel) — pas
  un test bUnit composant qui « ment au vert ». L'IHM **restante** (écrans sans scénario
  IHM dédié) est construite en **phase finale** (`ihm-builder`, étape 7) après le dernier
  scénario vert.
- **bUnit ≠ preuve d'acceptation runtime** : un bug d'usage (ex. `@rendermode
  InteractiveServer` manquant → `@onclick`/`@bind` morts) n'est **jamais** attrapé par
  bUnit (il force l'interactivité). La preuve d'un scénario IHM est un test E2E/runtime
  sur l'app réellement câblée.
- **Lanceur** : au scaffolding, `tdd-auto` génère `.claude/skills/run/` (script +
  skill `/run`) pour lancer l'appli d'une commande.
- Entrée attendue : un fichier produit par `make-gherkin`.
- **Journal méthode** : pendant le sprint, le thread principal consigne dans la section
  `# Méthode (agents)` du **fichier unifié** `docs/sprints/<sujet>/99-sprint<NN>-retours.md`
  chaque retour à la volée du PO sur un agent/skill/command (cible + retour + décision), et
  ses propres observations dans la section `## IA` — pour traitement par `retro-sprint` en
  fin de sprint. Le même fichier porte la section `# Retours produit (PO)` (remplie par le
  PO après le gate, lue par `/4-retours`). À ne pas confondre avec le backlog produit
  `99-sprint<NN>-besoins-fin-itération.md`.
- **Clôture de sprint = gate visuel impératif** (étape 8, `validation-visuelle`) : le
  sprint ne se conclut qu'après la notification « back + IHM up + retours préparé ».
  L'utilisateur teste l'IHM, remplit la section `# Retours produit (PO)` de
  `99-sprint<NN>-retours.md`, puis enchaîne `/4-retours` (besoins + archivage) →
  `/5-consolidation` (nouvelle spec) → `/2-make-gherkin`.
