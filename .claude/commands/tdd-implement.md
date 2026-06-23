---
description: Implémente un fichier de scénarios make-gherkin (docs/scenarios/<sujet>.md) en BDD + TDD, UN scénario à la fois avec checkpoint, via l'agent tdd-implement (.NET backend, Blazor/SignalR front).
argument-hint: "[sujet] [#scénario] (optionnels)"
---

# /tdd-implement — Implémentation BDD + TDD scénario par scénario

**Tout le travail vit dans le subagent `tdd-implement`** (lecture du fichier, cycles
rouge/vert, commit). Toi (thread principal) tu es un **relais pur** : tu n'écris ni
test ni implémentation, tu ne lis pas le code. Tu te bornes à : dispatcher l'agent,
rendre ses questions via `AskUserQuestion`, relayer les réponses via `SendMessage`,
et présenter le checkpoint. Objectif : **garder le contexte du main propre** — tout
le raisonnement reste chez l'agent.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le
> peut pas. C'est la **seule** raison du round-trip. La communication =
> `SendMessage` (main → agent) et la valeur de retour de l'agent (agent → main).

Arguments (optionnels) : $ARGUMENTS — sujet (fichier de scénarios) et/ou numéro de
scénario.

## Déroulé

1. **Contexte.** Repère le **chemin** du fichier de scénarios
   `docs/scenarios/<sujet>.md` et le **scénario cible** : celui demandé, sinon
   le prochain non implémenté (commits / état du code). **Ne lis pas le fichier ni le
   code toi-même** — l'agent s'en charge.

2. **Dispatch (agent + round-trip éventuel) :**
   - Dispatche l'agent `tdd-implement` avec le chemin du fichier + le scénario
     cible. Garde son `agentId`.
     - **Fallback** : si le type `tdd-implement` n'est pas dans le registre de la
       session, dispatche `general-purpose` avec « applique le skill `tdd-implement`,
       mode agent orchestré » + le chemin et le scénario cible. Ne bascule **pas** en
       exécution inline dans le main.
   - S'il renvoie `{ "type": "question", ... }` (scaffolding ou ambiguïté
     technique), rends la `question` **telle quelle** via `AskUserQuestion` et relaie
     la réponse **brute** à l'agent via `SendMessage` (même `agentId`). Répète si
     besoin.
   - Sinon il renvoie `{ "type": "result", ... }`.

3. **Checkpoint.** Présente le récap du scénario (verbatim depuis l'agent) : cycle
   rouge → vert → commit, fichiers de test et d'implémentation, état de la suite, et
   le scénario marqué `@vert`. Demande l'accord pour **enchaîner le scénario
   suivant** (`next_scenario`).

4. **Boucle.** Si l'utilisateur valide, relance pour le scénario suivant. Sinon,
   stoppe.

## Notes

- **Relais pur** : si tu te surprends à écrire un test, lire le code ou implémenter
  toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par l'agent.
- **Un scénario par run** — red-green-commit, puis checkpoint. Pas d'implémentation
  en bloc.
- Chaque scénario livré est taggé `@vert` dans le fichier de scénarios (état
  d'avancement) ; le prochain run cible le 1er scénario sans `@vert`.
- **BDD + TDD** : chaque scénario Gherkin → test d'acceptation exécutable (boucle
  externe), satisfait par des cycles unitaires rouge/vert (boucle interne).
- Le test d'acceptation **doit** échouer d'abord (rouge), sinon il n'observe rien.
- Relance la suite complète avant chaque commit (non-régression).
- Entrée attendue : un fichier produit par `/make-gherkin`.
