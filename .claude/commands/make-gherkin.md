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
