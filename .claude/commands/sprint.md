---
description: "Exécution + Review — la dev-team implémente tous les scénarios du fichier de sprint en BDD+TDD (backend puis IHM Blazor/SignalR réel), met à jour le tableau d'avancement, commite par scénario, puis gate visuel impératif (porte PO G3). Les questions dev sont tranchées par le scrum-master."
argument-hint: "[sujet] [#scénario] (optionnels)"
---

# /sprint — Exécution + Review (dev-team, puis gate PO)

**Tu es orchestrateur en relais pur.** Tu ne lis ni le fichier de scénarios ni le code, tu
n'écris ni test ni implémentation. Tu dispatches la `dev-team`, **routes ses questions vers le
`scrum-master`**, et présentes les checkpoints. Tu suis l'avancement dans le **tableau en tête**
du fichier de sprint `docs/sprints/NN-<slug>.md`.

> Seul le thread principal appelle `AskUserQuestion`. Communication = `SendMessage` (main →
> agent) et valeur de retour (agent → main).

## Protocole d'escalade — scrum-master

Quand la `dev-team` renvoie `{ "type":"question", … }` :
- **Routage IHM** (refus d'un scénario `@ihm`) → **mécanique** : redis à la `dev-team` de mener
  le scénario en **RED→GREEN runtime** (ni scrum-master ni PO).
- **Sinon** (scaffolding, early-green inattendu, problème d'implémentation) → dispatche le
  `scrum-master` (chapeau DÉCISION) avec la question + le fichier de sprint + la spec.
  - `{ "type":"decision", … }` → affiche `🧭 SM — <resume>` puis relaie la décision via
    `SendMessage`.
  - `{ "type":"escalate", gate:"G1", … }` → `AskUserQuestion` (payload du SM).
  - **Fallback** : type `scrum-master` absent → `general-purpose` + « applique l'agent
    `scrum-master`, chapeau DÉCISION ».

## Déroulé

1. **Contexte.** Repère le chemin `docs/sprints/NN-<slug>.md` (`$ARGUMENTS` = sujet et/ou #
   scénario). **Ne le lis pas** — la `dev-team` s'en charge.

2. **Implémentation, boucle par scénario.** Dispatche la `dev-team` sur le premier scénario non
   `✅` (ou celui demandé). Garde son `agentId`.
   - **Fallback** : type absent → `general-purpose` + « applique l'agent `dev-team` » + le chemin.
     **Rappel à porter** : « non-régression via `.claude/skills/dotnet/scripts/test.ps1`, suite
     complète, jamais `--no-build` ni filtre partiel ».
   - `{ "type":"question", … }` → Protocole d'escalade ci-dessus, puis reprends.
   - `{ "type":"result", … }` → un scénario livré (RED→GREEN→commit, tableau à jour).

3. **Boucle automatique.** Relance la `dev-team` sur `next_scenario` **sans demander
   confirmation**, jusqu'à ce que **tous les scénarios soient `✅`** dans le tableau en tête.
   La boucle se suspend sur une `{ "type":"question" }` (escalade) puis reprend. Présente chaque
   récap **verbatim** (pas d'`AskUserQuestion` entre scénarios).

4. **Phase IHM finale.** Quand tous les scénarios sont `✅`, **enchaîne automatiquement** la
   `dev-team` en mode construction IHM (vues restantes). `{ "type":"ihm", … }` → IHM construite
   (build + suite verts, commit) + commande de lancement.

5. **Gate visuel — porte G3 (IMPÉRATIVE, direct PO).**
   1. **Prouve back + IHM up** : `pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1 -Serial`
      (suite COMPLÈTE, **assemblies sérialisées = mode gate par défaut**, s37) puis **lance l'app** en tâche
      de fond via `pwsh .claude/skills/run/scripts/run.ps1`. **Pourquoi `-Serial` au gate** : le flake P1
      *TempsReel* qui motivait ce mode est **soldé à la cause s39** (collection xUnit `SignalRTempsReelCollection`
      sur les 55 `FrontWasm*TempsReel*` ; parallèle mesuré **0 % rouge sur 12 runs**). `-Serial` **reste** au gate
      en **ceinture + bretelles** (coût quasi nul, supprime tout résidu de course cross-assembly sous machine
      chargée) — **plus un contournement de flake**. La **concurrence réelle est éprouvée par la dev-team** au
      **cycle TDD rapide en parallèle** (désormais fiable). Un rouge déterministe reste rouge en série — aucune
      régression masquée, triage flake s21 inchangé. *(Récit : JOURNAL-METHODE s36/s37/s39.)*
   2. **Relaie** : routes à tester, fichier de retours préparé (section `# Retours produit (PO)`
      du fichier de sprint).
   3. **`AskUserQuestion`** — *« Revue de sprint NN — la livraison est-elle validée ? »* :
      - **Validée** → invite le PO à remplir `# Retours produit (PO)`, puis **enchaîne
        `/cloture`**.
      - **À retravailler** → recueille ce qui cloche, relance `/sprint` ciblé ; le gate se rejoue.
        Le sprint **ne se clôt pas**.

   C'est l'**unique interruption de livraison** : aucun sprint ne se conclut sans cette validation.

   **Discipline anti-scope-creep au gate (obligatoire).** Le gate valide le **goal G2**, il n'en
   ouvre pas de nouveau. Tout ajout exprimé par le PO au gate est trié **par défaut vers le backlog**
   (`# Retours produit (PO)` → item candidat à la `/cloture`), **PAS** absorbé dans le sprint courant.
   **Seule exception — absorption immédiate** : une **finition triviale DANS le goal** (correctif de
   présentation, libellé, micro-ajustement d'un scénario déjà livré) **sans** nouveau handler/commande,
   **sans** révision d'invariant ou de règle, **sans** nouvelle surface IHM. Dès qu'un ajout touche un
   **invariant**/une **porte métier** ou demande un **nouveau volet**, il **retourne au `/planning`**
   (G2/G1), jamais tranché sous la pression du gate. *(Récit : JOURNAL-METHODE s29.)*

## Notes

- **Relais pur** : analyser, écrire un test, lire le code = sortie de rôle, redélègue.
- **Acceptation runtime obligatoire** : la preuve d'un scénario `@ihm` est un test runtime sur
  l'app réellement câblée, **jamais** bUnit (qui ment au vert sur un render mode manquant).
- **Reprise sur mort d'agent** : si un agent meurt en laissant un test non commité, finalise à la
  main (vérifie le test signifiant + vert via `test.ps1`, tague `@vert`, resynchronise le compte
  `X/N`, commite) plutôt que de re-dispatcher aveuglément.
- **Un scénario = un commit** (sauf lot de caractérisations early-green anticipées consécutives).
