---
description: "Sprint Planning — le scrum-master propose 3-4 goals candidats depuis le backlog vivant priorisé (porte PO G2), puis écrit le fichier de sprint léger (tableau d'avancement en tête + scénarios Gherkin). Amorce la boucle de sprint."
argument-hint: "[goal imposé] (optionnel)"
---

# /planning — Sprint Planning (scrum-master)

**Tu es orchestrateur en relais pur.** Tu ne lis ni n'écris la spec, le backlog ou les
scénarios — tu dispatches le `scrum-master` et tu poses les portes au PO. Seul le thread
principal appelle `AskUserQuestion` ; un subagent ne le peut pas (round-trip via `SendMessage`
et valeur de retour).

## Déroulé

1. **Proposer les goals.** Dispatche `scrum-master` (chapeau PLANNING) — il lit
   `docs/BACKLOG.md` (retours prioritaires) + `docs/specs/index.md`.
   - **Fallback** : type absent du registre → `general-purpose` + « applique l'agent
     `scrum-master`, chapeau PLANNING ».
   - Il renvoie `{ "type":"goals", goals:[{titre, scope:[…]}] }` (3-4 cartes, ~1h IA chacune,
     bullets de scope concret).

2. **Porte G2 — choix du sprint goal.** Affiche le `resume`, puis `AskUserQuestion` : présente
   les 3-4 goals (label = titre, description = les bullets de scope). Le PO tranche **(il peut
   injecter un goal via "Autre")**. Si `$ARGUMENTS` impose déjà un goal, saute la question et
   passe-le tel quel.

3. **Écrire le fichier de sprint.** Renvoie le goal tranché au `scrum-master` via `SendMessage`.
   Il écrit `docs/sprints/NN-<slug>.md` : **tableau d'avancement en tête** (X/N + une ligne par
   scénario) + scénarios Gherkin `@back`/`@ihm` `@pending` + section `# Retours produit (PO)`
   vide. Il renvoie `{ "type":"sprint", fichier, scenarios }`.

4. **Récap.** Affiche le `resume` + le chemin du fichier + le nb de scénarios (back vs IHM).
   **Enchaîne automatiquement** : « lance `/sprint` » (pas d'`AskUserQuestion` — le PO garde la
   main, il peut interrompre).

## Notes

- **Relais pur** : si tu lis le backlog/la spec ou écris le fichier de sprint toi-même, tu as
  quitté ton rôle — redélègue.
- **Une seule porte ici = G2.** Tout le reste est tranché par le `scrum-master`.
- Le **numéro de sprint** `NN` = prochain libre dans `docs/sprints/`.
