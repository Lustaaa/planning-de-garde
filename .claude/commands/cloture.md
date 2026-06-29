---
description: "Rétro + clôture — le scrum-master fusionne les retours produit du PO dans le backlog vivant (rien ne se perd), édite le(s) sujet(s) de spec EN DIFF, déclenche une rétro méthode CONDITIONNELLE (amélioration ou rien), puis git push → PR → merge main (porte git PO). Reboucle sur /planning."
argument-hint: "[sujet] (optionnel)"
---

# /cloture — Rétro + clôture (scrum-master)

**Tu es orchestrateur en relais pur.** Tu dispatches le `scrum-master` (chapeau CLÔTURE) et tu
poses la seule porte PO restante (git sortant). Seul le thread principal appelle
`AskUserQuestion`.

## Déroulé

1. **Clôture (scrum-master).** Dispatche `scrum-master` (chapeau CLÔTURE) avec le chemin du
   fichier de sprint `docs/sprints/NN-<slug>.md`.
   - **Fallback** : type absent → `general-purpose` + « applique l'agent `scrum-master`, chapeau
     CLÔTURE ».
   - Il : (a) **fusionne les retours** de `# Retours produit (PO)` dans `docs/BACKLOG.md` (vivant,
     rien ne se perd, marque `fait` le livré) ; (b) **édite la spec EN DIFF** (sujets concernés
     sous `docs/specs/` + `index.md` si nouveau sujet, jamais de réécriture intégrale) ;
     (c) **rétro méthode CONDITIONNELLE** — si friction réelle : un edit concret d'un fichier
     pipeline + 1 ligne dans `docs/sprints/JOURNAL-METHODE.md` ; sinon skip **justifié**.
   - Il renvoie `{ "type":"cloture", backlog_maj, spec_maj, retro }`.

2. **Récap.** Affiche le `resume` + ce qui a été fusionné/édité + le verdict rétro (amélioration
   appliquée, ou « pas de friction » justifié).

3. **Git sortant — porte PO.** Propose via `AskUserQuestion` : push → PR → merge `main` ?
   - **Oui** → exécute le skill `git` :
     `pwsh .claude/skills/git/scripts/push.ps1` puis
     `pwsh .claude/skills/git/scripts/pr.ps1 -Title "…" -Body "…"` (gh optionnel) ; après merge,
     `pwsh .claude/skills/git/scripts/push.ps1 -ReturnToMain`.
   - **Non** → s'arrête là, laisse le PO faire.

4. **Reboucle.** De retour sur `main` à jour, **enchaîne automatiquement** : « lance `/planning`
   pour le sprint suivant » (le PO garde la main).

## Notes

- **Relais pur** : tu n'édites ni le backlog ni la spec toi-même — c'est le `scrum-master`.
- **Spec en diff uniquement** : la cause historique du x10 était la réécriture intégrale à chaque
  sprint. Une seule spec vivante éclatée par sujet, éditée au point concerné.
- **Rétro = « amélioration ou rien »** : pas de doc `98-retrospective.md`, juste un edit + 1 ligne
  de journal, et seulement s'il y a eu friction.
- **Retours produit ≠ rétro méthode** : (1) fait avancer l'app (backlog), à chaque sprint ;
  (2) améliore le pipeline, conditionnelle.
