---
description: Implémente un fichier de scénarios make-gherkin (docs/init/scenarios/<sujet>.md) en BDD + TDD, UN scénario à la fois avec checkpoint, via l'agent tdd-implement (.NET backend, Blazor/SignalR front).
argument-hint: "[sujet] [#scénario] (optionnels)"
---

# /tdd-implement — Implémentation BDD + TDD scénario par scénario

Orchestration par **un seul agent** : tu (thread principal) lances l'agent
`tdd-implement`, **poses ses questions via `AskUserQuestion`** (scaffolding,
ambiguïté technique), relaies les réponses, et présentes le checkpoint après
chaque scénario. L'agent ne pose jamais les questions lui-même.

Arguments (optionnels) : $ARGUMENTS — sujet (fichier de scénarios) et/ou numéro de
scénario.

## Déroulé

1. **Contexte.** Repère le fichier de scénarios `docs/init/scenarios/<sujet>.md`.
   Détermine le **scénario cible** : celui demandé, sinon le prochain non
   implémenté (regarde les commits / l'état du code).

2. **Dispatch (agent + round-trip éventuel) :**
   - Dispatche l'agent `tdd-implement` avec le chemin du fichier + le scénario
     cible.
   - S'il renvoie `{ "type": "question", ... }` (scaffolding ou ambiguïté
     technique), rends la `question` via `AskUserQuestion` (option recommandée en
     premier) et relaie la réponse à l'agent via `SendMessage`. Répète si besoin.
   - Sinon il renvoie `{ "type": "result", ... }`.

3. **Checkpoint.** Présente le récap du scénario : cycle rouge → vert → commit,
   fichiers de test et d'implémentation, état de la suite. Demande l'accord pour
   **enchaîner le scénario suivant** (`next_scenario`).

4. **Boucle.** Si l'utilisateur valide, relance pour le scénario suivant. Sinon,
   stoppe.

## Notes

- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par l'agent.
- **Un scénario par run** — red-green-commit, puis checkpoint. Pas d'implémentation
  en bloc.
- **BDD + TDD** : chaque scénario Gherkin → test d'acceptation exécutable (boucle
  externe), satisfait par des cycles unitaires rouge/vert (boucle interne).
- Le test d'acceptation **doit** échouer d'abord (rouge), sinon il n'observe rien.
- Relance la suite complète avant chaque commit (non-régression).
- Entrée attendue : un fichier produit par `/make-gherkin`.
