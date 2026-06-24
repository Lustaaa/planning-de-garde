---
description: Clôt un sprint terminé (gate visuel passé, /4-retours et /5-consolidation faits) — pousse la branche, prépare la PR vers main (gh-optionnel), puis après merge revient sur main et amorce l'itération suivante (/2-make-gherkin sur la nouvelle spec). Rituel mécanique adossé au script cloture-sprint.ps1.
argument-hint: "[nom du sprint] (optionnel)"
---

# /6-cloture-sprint — Push, PR, merge main, itération suivante

Le **rituel de clôture** en bout de boucle. Mécanique (pas de subagent) : tu enchaînes
des étapes git/PR déterministes et tu **confirmes** les actions conséquentes (push, merge
dans `main`) avant de les faire. Pousser et merger sont **outward-facing** : valide avec
le PO à chaque palier.

Argument (optionnel) : $ARGUMENTS — nom du sprint (sinon déduit du dernier
`docs/sprints/*` contenant un `*-retours.md`).

## Préconditions

- Sur une **branche de sprint** (pas `main`).
- Working tree **propre** (tout commité — la spec `/5-consolidation`, les besoins
  `/4-retours`, l'archivage). Si `git status` n'est pas propre → commit d'abord, ne pousse
  pas un état partiel.

## Déroulé

1. **Prépare la PR (script).** Exécute
   `pwsh -NoProfile -File .claude/skills/cloture-sprint/scripts/cloture-sprint.ps1`
   (passe `-Sprint $ARGUMENTS` si fourni). Le script **pousse** la branche et renvoie :
   `branch`, `base`, `compareUrl`, `ghPresent`, `bodyPath`, `title`, `commits`.
   - **Confirme le push** au préalable si le PO n'a pas déjà tranché (action sortante).

2. **Crée la PR (selon `ghPresent`).**
   - **`ghPresent = true`** : propose (via `AskUserQuestion`) de créer la PR :
     `gh pr create --base <base> --head <branch> --title "<title>" --body-file <bodyPath>`.
     Après création, propose le merge `gh pr merge --merge` (ou `--squash` selon préférence
     du PO) — **uniquement** sur validation explicite.
   - **`ghPresent = false`** : **présente** au PO le `title`, le **corps** (lis `bodyPath`)
     et l'**`compareUrl`**. Demande-lui de créer **et merger** la PR via l'UI GitHub. Puis
     **attends** qu'il confirme le merge (gate manuel) avant l'étape 3.

3. **Retour sur main.** Une fois la PR **mergée** (confirmée), `git checkout <base>` puis
   `git pull`. Confirme que `main` contient bien le merge.

4. **Amorce l'itération suivante.** Propose (via `AskUserQuestion`) d'enchaîner
   `/2-make-gherkin` sur la **nouvelle version de spec** (`docs/NN-specification.md`, la
   plus récente) en ciblant le `prochain_sujet` du backlog du sprint clos
   (`99-sprint<NN>-besoins-fin-itération.md`, `<NN>` = numéro du sprint = préfixe 2 chiffres
   du dossier, ex. `99-sprint02-besoins-fin-itération.md`). Si le PO valide, invoque `/2-make-gherkin` avec le
   chemin de la spec + le slug du prochain sujet → un nouveau sprint démarre.

## Notes

- **Jamais de merge ni de push sans validation explicite** — ce sont des actions
  sortantes et (pour `main`) difficilement réversibles.
- Le script **ne merge jamais** lui-même ; il prépare le matériel de PR. Le merge est
  fait par `gh` (sur validation) ou par le PO via l'UI.
- Pas depuis `main` ni avec un working tree sale (le script refuse le 1er cas).
- **Boucle complète** : `/1 → /2 → /3 (+gate visuel) → /4-retours → /5-consolidation →
  /6-cloture-sprint → /2 …` (nouveau sprint).
- Enrichissement futur possible : un corps de PR rédigé par un petit agent (résumé
  narratif du sprint) plutôt que templaté — MVP templaté pour l'instant.
