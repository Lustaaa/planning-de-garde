---
description: Ferme la boucle d'itération — challenge la section `# Retours produit (PO)` du fichier unifié 99-sprint<NN>-retours.md (retours IHM/Tech) via l'agent retours-challenge, classe et priorise les besoins, écrit le backlog 99-sprint<NN>-besoins-fin-itération.md, archive les scénarios de l'itération, puis enchaîne /5-consolidation (nouvelle version de spec) en vue de /2-make-gherkin.
argument-hint: "[dossier de scénarios ou chemin du 99-sprint<NN>-retours.md] (optionnel)"
---

# /4-retours — Retours utilisateur → besoins priorisés

**Tout le travail vit dans le subagent `retours-challenge`.** Toi (thread principal) tu
es un **orchestrateur** : tu ne classes pas les retours, tu ne nommes pas les tensions, tu
ne calcules ni priorisation ni synthèse. Tu te bornes à : localiser le retours (script),
gérer le bypass Tech, dispatcher l'agent, **router ses questions vers le chef de projet**
(escalade PO seulement sur G1/G2), lui renvoyer les réponses brutes via `SendMessage`, puis
lui ordonner d'écrire. Objectif : **garder le contexte du main propre** — tout le
raisonnement reste chez l'agent, et **le PO n'est sollicité que sur les portes essentielles**.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le peut
> pas. C'est la **seule** raison du round-trip. Communication = `SendMessage`
> (main → agent) et valeur de retour de l'agent (agent → main).

> **Protocole d'escalade — chef de projet (CP).** Quand `retours-challenge` renvoie une
> `question`, **dispatche d'abord l'agent `chef-de-projet`** avec : la `question`, la **spec
> courante** (`docs/NN-specification.md`), `docs/BACKLOG.md`, le dossier de sprint, le palier
> d'autonomie (défaut `0 — conservateur`).
> - `{type:"decision",…}` → **affiche le `resume` du CP en une ligne** (`🧭 CP — <resume>`) pour
>   le suivi du PO (sans `AskUserQuestion`), puis **relaie la décision** (couvre la
>   **classification** des retours, la confrontation `bug` vs HEAD, le **séquencement** dans une
>   règle d'arbitrage déjà actée).
> - `{type:"escalate", gate:"G1"|"G2", …}` → `AskUserQuestion` (payload riche). Le **choix du
>   prochain sujet / cap** est un **G2** : le CP propose **2 goals candidats** (~2h IA, tirés du
>   backlog) et **le PO tranche** (3ᵉ injectable).
> - **Fallback** : type `chef-de-projet` absent → `general-purpose` + « applique le skill
>   `chef-de-projet` ».

> **Cadrage à rappeler au PO si besoin.** `/4-retours` **priorise** le backlog des retours
> produit (classer, départager, désigner un prochain sujet) — il **ne conçoit pas le
> sprint**. La conception (scénarios) se fait plus tard en `/2-make-gherkin`, après
> `/5-consolidation`. (Confusion observée au sprint 04.)

Cette command **ferme la boucle** : `/3-tdd-implement` (+ IHM) livre un incrément,
l'utilisateur le teste et dépose ses retours dans la section `# Retours produit (PO)` du
fichier unifié `99-sprint<NN>-retours.md`, puis `/4-retours` les transforme en besoins
priorisés qui réamorcent `/2-make-gherkin`.

Argument (optionnel) : $ARGUMENTS — dossier de scénarios ou chemin du `99-sprint<NN>-retours.md`.

## Déroulé

1. **Localise le retours (script).** Exécute
   `pwsh -NoProfile -File .claude/skills/retours-challenge/scripts/find-retours.ps1`
   (avec `-Dossier <chemin>` si `$ARGUMENTS` désigne un dossier ; sinon le script retient
   le `99-sprint<NN>-retours.md` le plus récent sous `docs/sprints/*/`). Récupère le JSON :
   `retoursPath` (= `99-sprint<NN>-retours.md`), `dossier`, `hasIHM`, `hasTech` (détectés
   dans la section `# Retours produit (PO)`), `sections`, `nextBesoins`.
   - Si `found=false` → préviens l'utilisateur qu'aucun `99-sprint<NN>-retours.md` n'existe
     et stoppe. **Ne lis pas** le retours toi-même — l'agent s'en charge.

2. **Bypass Tech (conditionnel).** Si `hasTech=false` (pas de sous-section `## Tech` dans
   la section `# Retours produit (PO)`), **demande** via `AskUserQuestion` :
   « La section Retours produit (PO) ne contient pas de sous-section Tech. Y a-t-il des
   contraintes techniques à injecter (dette, perf, archi, issues d'une revue GitHub) ? » — options :
   *Aucune (Recommandé)* / *Oui, je les précise* (l'utilisateur saisit le texte) /
   *Les chercher dans une revue de code*. Conserve la réponse **brute** pour la passer à
   l'agent. Si `hasTech=true`, saute cette étape (les retours Tech sont déjà dans le
   fichier).

3. **Dispatch (agent `retours-challenge`).** Lance-le avec : `retoursPath`, le chemin
   cible `nextBesoins`, le résultat du bypass Tech (étape 2), et les chemins de contexte
   (`<dossier>/00-sprint<NN>-suivi.md`, `docs/01-specification.md`). Garde son `agentId`.
   - **Fallback** : type absent du registre → `general-purpose` avec « applique le skill
     `retours-challenge`, mode agent orchestré » + les mêmes chemins. Ne bascule **pas**
     en inline.

4. **Boucle de challenge (relais).** À chaque retour, l'agent renvoie
   `{ classification, tensions, questions, synthese, done }`. Tant que `done` est faux :
   - Au **1er tour**, au plus **une ligne** de contexte pour l'utilisateur (résumé des
     `tensions` ou du nombre de retours classés) ; sinon n'écris rien.
   - Pour **chaque** entrée de `questions[]`, applique le **Protocole d'escalade CP** : dispatch
     `chef-de-projet` ; n'appelle `AskUserQuestion` que sur une `escalate` (G1 arbitrage / G2 cap),
     en passant alors l'objet `question` du CP **tel quel**.
   - Renvoie les réponses **brutes** (décision CP ou réponse PO) à l'agent via `SendMessage`
     (même `agentId`).
   - Répète. **N'analyse pas**, **ne devine pas** la question suivante. Si l'utilisateur
     répond « tout prioritaire », l'agent reposera une question d'arbitrage — c'est voulu.

5. **Validation (CP).** Quand `done: true`, fais valider l'écriture par le **chef de projet**
   (dispatch `chef-de-projet` avec la `synthese`). `{type:"decision"}` (priorisation dérivable
   de l'arbitrage acté) → **ordonne d'écrire le backlog sans déranger le PO**.
   `{type:"escalate", gate:"G2"}` (le **prochain sujet / cap** est à fixer) → `AskUserQuestion`
   (2 goals candidats du CP, 3ᵉ injectable), puis écris. Présente la `synthese` **verbatim**
   (classification, arbitre, séquence, prochain sujet, risques). Si le PO semble attendre un
   découpage de sprint, **rappelle** qu'on **priorise** ici, on ne **conçoit** pas le sprint
   (ça, c'est `/2-make-gherkin`).

6. **Écriture (même agent).** À l'accord, `SendMessage` l'ordre d'écrire avec le chemin
   `nextBesoins`. L'agent écrit `99-sprint<NN>-besoins-fin-itération.md` (`<NN>` = numéro du
   sprint = préfixe 2 chiffres du dossier, ex. `99-sprint02-besoins-fin-itération.md` ; au
   format imposé du skill) et renvoie `{ path, besoins, prochain_sujet, notes }`.

7. **Archivage de l'itération (script).** Une fois le backlog écrit, **clôs l'itération** :
   exécute
   `pwsh -NoProfile -File .claude/skills/retours-challenge/scripts/archive-iteration.ps1 -Dossier <dossier>`.
   Le script déplace les fichiers de scénario (`NN-slug.md`) dans `<dossier>/archive/`, ne
   laissant à la racine que `00-sprint<NN>-suivi.md`, le fichier unifié `99-sprint<NN>-retours.md`
   et `99-sprint<NN>-besoins-fin-itération.md`, et réécrit les liens de `00-sprint<NN>-suivi.md` vers `archive/`.
   Présente le récap (champ `archived` / `kept`).

8. **Handoff consolidation.** Présente le `prochain_sujet` et **propose** d'enchaîner
   `/5-consolidation` via `AskUserQuestion` : l'étage de consolidation fusionne ce backlog
   `99-sprint<NN>-besoins-fin-itération.md` avec la spec courante pour produire la **nouvelle version
   de spec** (`NN-specification.md`), qui devient ensuite l'entrée de `/2-make-gherkin`.
   Si l'utilisateur valide, invoque `/5-consolidation`. (Ne saute **pas** vers
   `/2-make-gherkin` directement : la consolidation de la spec vivante vient d'abord.)

   > **Gate anti-bypass de la rétro (amélioration continue).** Écrire le backlog
   > `99-sprint<NN>-besoins-fin-itération.md` **clôt l'itération** : à partir d'ici, le sprint
   > est « clos non-rétrospecté » tant que `retro-sprint` n'a pas tourné. La
   > **rétrospective de la méthode est impérative avant tout nouveau cycle**
   > `/2-make-gherkin` — elle est l'étape 1 de `/6-cloture-sprint`, et `/2` refuse de
   > démarrer si elle manque (gate `find-retro.ps1`). Ne présente jamais l'enchaînement
   > comme s'il pouvait sauter la rétro. Le chemin canonique reste
   > `/4-retours → /5-consolidation → /6-cloture-sprint (retro-sprint + push/PR) → /2-make-gherkin`.

9. **Commit.** Propose un commit du backlog + de l'archivage (sans pousser sauf demande
   explicite).

## Notes

- **Relais pur** : si tu te surprends à lire le retours, classer un item ou rédiger la
  synthèse toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par l'agent ;
  c'est l'unique chose qu'il ne peut pas faire — sauf le **bypass Tech** (étape 2) qui
  est de ta responsabilité avant même le dispatch.
- Une question à la fois pendant le challenge — pas de rafale.
- **Un seul prochain sujet** désigné pour `/2-make-gherkin` ; le reste est séquencé dans
  le backlog. Un `bug` (comportement vert cassé) repart par `/3-tdd-implement` ciblé,
  pas par make-gherkin.
- L'agent ne touche **que** le `99-sprint<NN>-besoins-fin-itération.md` cible — jamais le
  fichier unifié `99-sprint<NN>-retours.md` ni le `00-sprint<NN>-suivi.md` / les `NN-slug.md`.
- Entrée attendue : la section `# Retours produit (PO)` de `99-sprint<NN>-retours.md`,
  remplie par l'utilisateur après test d'un incrément.
