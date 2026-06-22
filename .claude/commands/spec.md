---
description: Cadre un produit ou une feature de bout en bout — challenge le PO (via agent) puis rédige/maj la spec (via agent) au format maison.
argument-hint: "[sujet ou feature à cadrer] (optionnel)"
---

# /spec — Cadrage produit orchestré

Orchestration par **agents** : tu (thread principal) ne fais que dispatcher les
agents et **poser les questions via `AskUserQuestion`**. Les agents ne posent
jamais les questions eux-mêmes.

Sujet (optionnel) : $ARGUMENTS

## Déroulé

1. **Contexte.** Repère la spec/docs/commits pertinents (chemins à passer aux agents).

2. **Challenge (agent + round-trip) :**
   - Dispatche l'agent `brainstorm` avec le sujet + les chemins de contexte.
   - Il renvoie un JSON `{ tensions, questions, synthese, done }`.
   - Affiche les `tensions` au PO, puis rends **chaque** `questions[]` via `AskUserQuestion` (option recommandée en premier).
   - Renvoie les réponses à l'agent via `SendMessage` (continue le même agent).
   - Répète tant que `done` est faux. Si le PO répond « tous », l'agent reposera une question de séquencement — c'est voulu.

3. **Validation PO.** Quand `done: true`, présente la `synthese` (objectif, arbitre, séquence, risques) et demande l'accord avant de rédiger.

4. **Rédaction (agent) :**
   - Dispatche l'agent `redaction-spec` avec le chemin cible + la `synthese`.
   - Il écrit le fichier et renvoie `{ path, sections, regles, notes }`.

5. **Propagation.** Mets à jour les docs qui référencent la spec (README, roadmap) ; garde une seule source de vérité, pointe les brouillons obsolètes vers elle.

6. **Commit.** Propose un commit (sans pousser sauf demande explicite).

## Notes

- `AskUserQuestion` est appelé **par toi** (thread principal), jamais par les agents.
- Une question à la fois pendant le challenge — pas de rafale.
- Fonctionnel uniquement dans la spec : aucun choix technique.
- Le challenge n'est pas une formalité : pas de complaisance, on tranche.
