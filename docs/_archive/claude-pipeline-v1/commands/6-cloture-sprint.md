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

1. **Rétrospective de la méthode (agent `retro-sprint`) — IMPÉRATIVE, AVANT le push.**
   La **sprint retrospective** (sur la méthode, pas le produit) est le dernier maillon de
   la boucle : ses améliorations partent **dans la PR du sprint**, donc elle tourne
   **avant** d'empaqueter la PR. Vérifie d'abord l'état du gate :
   `pwsh -NoProfile -File .claude/skills/retro-sprint/scripts/find-retro.ps1`.
   - Si `gateOpen=false` (le sprint clos n'a **pas** de `98-retrospective.md`) →
     **dispatche l'agent `retro-sprint`** avec le dossier du sprint (`docs/sprints/<sujet>/`)
     et les frictions vécues. L'agent renvoie `{ bilan, actions, questions, … }`. **La
     priorisation des actions passe par le CP** (plus de multiSelect PO systématique) :
     dispatche `chef-de-projet` avec la liste d'`actions`. Le CP **sélectionne les actions à
     appliquer** (tweaks de méthode à faible risque) et n'**escalade au PO (G1)** que les
     changements **structurels/risqués** du pipeline (ex. refonte d'un agent, suppression
     d'un gate). Relaie la sélection (décision CP ou réponse PO) à l'agent via `SendMessage`,
     puis ordonne d'**appliquer** les actions retenues et d'écrire `98-retrospective.md`.
     Chaque action auto-appliquée par le CP est **journalisée** (pilotage a posteriori du PO).
     > **⚠️ Garde Self-Modification (vécu s13 + s14).** L'**application** des éditions ciblant
     > `.claude/` (agents/skills/commands) est **bloquée pour les subagents** même quand la
     > priorisation vient du CP : le garde n'accepte pas la délégation CP pour l'auto-modification
     > du pipeline. La **priorisation reste CP** (tweaks faible risque) mais l'**écriture effective
     > sur `.claude/` exige l'autorisation PO directe** — c'est le **thread principal** qui applique
     > les éditions une fois cet aval obtenu (le subagent `retro-sprint` ne fait que les préparer :
     > cibles + contenu exact). Ne pas redécouvrir ce protocole à chaque rétro.
     - **Fallback** : type `retro-sprint` absent du registre → `general-purpose` avec
       « applique le skill `retro-sprint`, section *Mode agent (orchestré)* » + le dossier
       du sprint + les frictions. Pas d'inline.
   - Si `gateOpen=true` (rétro déjà faite) → passe directement à l'étape 2.
   - **Ce gate est dur** : ne prépare **pas** la PR tant que `98-retrospective.md` n'existe
     pas pour le sprint clos. C'est l'amélioration continue rendue **non contournable**.

1bis. **Archivage de clôture (script) — AVANT le push, pour partir dans la PR.** Une fois
   la rétro écrite (`98-retrospective.md` présent), **range le dossier du sprint clos** :
   `pwsh -NoProfile -File .claude/skills/retours-challenge/scripts/archive-iteration.ps1 -Dossier docs/sprints/<sujet> -Closure`.
   Le mode `-Closure` déplace dans `archive/` **tous** les `.md` de pilotage du sprint clos
   (`99-sprint<NN>-retours.md`, `99-sprint<NN>-besoins-fin-itération.md`, `98-retrospective.md`,
   plus les scénarios déjà archivés en `/4-retours`) en **ne laissant à la racine que
   `00-sprint<NN>-suivi.md`** (seul fichier qu'un agent d'un sprint ultérieur peut lire), et
   réécrit les liens du suivi. **Commite** ce rangement (sans pousser) pour qu'il parte dans
   la PR. Les scripts de détection (`find-retro.ps1`, `cloture-sprint.ps1`) reconnaissent ces
   fichiers à la racine **comme** sous `archive/` (glob `-Recurse`) : le gate de rétro et la
   détection du sprint clos restent corrects après archivage. (Retour PO méthode sprint 10 :
   les agents ne lisent pas `archive/` ; ne conserver hors archive que le suivi.)

2. **Prépare la PR (script).** Exécute
   `pwsh -NoProfile -File .claude/skills/cloture-sprint/scripts/cloture-sprint.ps1`
   (passe `-Sprint $ARGUMENTS` si fourni). Le script **pousse** la branche et renvoie :
   `branch`, `base`, `compareUrl`, `ghPresent`, `bodyPath`, `title`, `commits`.
   - **Confirme le push** au préalable si le PO n'a pas déjà tranché (action sortante).

3. **Crée la PR (selon `ghPresent`).**
   - **`ghPresent = true`** : propose (via `AskUserQuestion`) de créer la PR :
     `gh pr create --base <base> --head <branch> --title "<title>" --body-file <bodyPath>`.
     Après création, propose le merge `gh pr merge --merge` (ou `--squash` selon préférence
     du PO) — **uniquement** sur validation explicite.
   - **`ghPresent = false`** : **présente** au PO le `title`, le **corps** (lis `bodyPath`)
     et l'**`compareUrl`**. Demande-lui de créer **et merger** la PR via l'UI GitHub. Puis
     **attends** qu'il confirme le merge (gate manuel) avant l'étape 3.

4. **Retour sur main + product backlog.** Une fois la PR **mergée** (confirmée),
   `git checkout <base>` puis `git pull`. Confirme que `main` contient bien le merge. Puis
   **mets à jour le product backlog** `docs/BACKLOG.md` : passe à **✅ fait** les besoins du
   sprint clos qui ont été livrés (gate visuel passé), en renseignant leur sprint de
   rattachement. (Le backlog est alimenté en ajout par `/4-retours` et en passage à « fait »
   ici, à la clôture.)

4bis. **Consolidation du product backlog (avant le handoff).** Après le retour sur `main`
   et le passage à ✅ fait, **relis `docs/BACKLOG.md` de bout en bout** et consolide-le à
   partir du `99-sprint<NN>-besoins-fin-itération.md` du sprint clos : épics (vue
   fonctionnelle), paliers (séquence de livraison) et la section **« Prochains sprints
   envisagés »** (les 2 sujets en tête de file). Objectif : que `/2-make-gherkin` du sprint
   suivant s'appuie sur un backlog à jour, pas seulement sur des lignes cochées. (Retour PO
   sprint 04.)

   > **Check obligatoire — numérotation (spec vivante = référence unique).** La **spec vivante**
   > (`docs/NN-specification.md`, la plus récente) est la **référence unique de numérotation des
   > paliers** : **réconcilie ici** la numérotation de `docs/BACKLOG.md` avec celle de la spec
   > courante (le CP la renvoie systématiquement à `/6` lors de `/5-consolidation`). **Résorbe
   > l'écart maintenant, ne le re-diffère pas** : aligne les n° de palier du backlog sur la spec,
   > répercute tout swap acté en `/5` (ex. palier tiré devant un autre). Coche ce point comme
   > fait avant le handoff. (Rétro s12 A4 ; écart récurrent.)

5. **Amorce l'itération suivante — porte G2 (choix du sprint goal).** C'est l'**une des
   deux seules portes PO** du pipeline (avec G3). Dispatche `chef-de-projet` : il propose
   **2 sprint goals candidats** (~2h IA, tirés du backlog consolidé) ; appelle
   `AskUserQuestion` pour que **le PO tranche** (3ᵉ goal injectable). Une fois le goal
   choisi, **invoque automatiquement `/2-make-gherkin`** sur la **nouvelle version de spec**
   (`docs/NN-specification.md`, la plus récente) en ciblant ce goal → un nouveau sprint
   démarre. (Le gate d'entrée de `/2` revérifie que la rétro du dernier sprint clos a bien
   tourné.) Pas d'autre `AskUserQuestion` ici que ce choix G2.

## Notes

- **Chef de projet (CP) dans `/6`.** La **priorisation des actions de rétro** (étape 1) passe
  désormais **par le CP** : il applique les tweaks de méthode à faible risque et n'escalade au
  PO (G1) que les changements structurels/risqués (journalisés pour pilotage a posteriori).
  Restent **PO par conception** : le **push/PR/merge** (actions **sortantes**, gates git non
  délégables au CP) et le **choix du sprint goal** (étape 5, porte **G2** : le CP propose 2 goals
  ~2h IA tirés du backlog, le PO tranche, 3ᵉ injectable). Avec **G3** (revue de sprint, `/3`),
  G2 et les confirmations git sont les **seules** sollicitations PO du pipeline.
- **Jamais de merge ni de push sans validation explicite** — ce sont des actions
  sortantes et (pour `main`) difficilement réversibles.
- Le script **ne merge jamais** lui-même ; il prépare le matériel de PR. Le merge est
  fait par `gh` (sur validation) ou par le PO via l'UI.
- Pas depuis `main` ni avec un working tree sale (le script refuse le 1er cas).
- **Sprint retrospective d'abord (étape 1)** : la rétro de la méthode (`retro-sprint`)
  est **non contournable** — `find-retro.ps1` bloque la clôture tant que le sprint clos
  n'a pas son `98-retrospective.md`. Distincte de `/4-retours` (retours produit).
- **Boucle complète** : `/1 → /2 → /3 (+gate visuel = sprint review + DoD) → /4-retours →
  /5-consolidation → /6-cloture-sprint (retro-sprint, push/PR/merge, consolidation BACKLOG) → /2 …`
  (nouveau sprint). La **sprint retrospective** est le maillon de clôture.
- Enrichissement futur possible : un corps de PR rédigé par un petit agent (résumé
  narratif du sprint) plutôt que templaté — MVP templaté pour l'instant.
