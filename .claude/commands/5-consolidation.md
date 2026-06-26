---
description: Consolide le backlog de besoins (99-sprint<NN>-besoins-fin-itération.md, sortie de /4-retours) avec la spec courante en une nouvelle version de spec vivante (NN-specification.md), via l'agent spec-consolidation (round-trip de questions + écriture), puis réamorce /2-make-gherkin sur la nouvelle spec.
argument-hint: "[chemin du backlog 99-sprint<NN>-besoins-fin-itération.md ou dossier de sprint] (optionnel)"
---

# /5-consolidation — Besoins priorisés → spec vivante versionnée

**Tout le travail vit dans le subagent `spec-consolidation`.** Toi (thread principal) tu
es un **orchestrateur** : tu ne lis ni le backlog ni la spec, tu ne nommes pas les points de
consolidation, tu ne tranches pas les collisions. Tu te bornes à : localiser la spec
courante (script), dispatcher l'agent, **router ses questions vers le chef de projet**
(escalade PO seulement sur G1), lui renvoyer les réponses brutes via `SendMessage`, puis lui
ordonner d'écrire. Objectif : **garder le contexte du main propre** — tout le raisonnement
reste chez l'agent, et **le PO n'est sollicité que sur les portes essentielles**.

> ⚠️ Seul le thread principal peut appeler `AskUserQuestion` ; un subagent ne le peut
> pas. C'est la **seule** raison du round-trip. Communication = `SendMessage`
> (main → agent) et valeur de retour de l'agent (agent → main).

> **Protocole d'escalade — chef de projet (CP).** Quand `spec-consolidation` renvoie une
> `question` (collision de règles, question ouverte), **dispatche d'abord l'agent
> `chef-de-projet`** avec : la `question`, `currentSpec`, le backlog, le palier d'autonomie
> (défaut `0 — conservateur`).
> - `{type:"decision",…}` → **relaie la décision** (couvre les collisions à **résolution
>   déterministe** : une règle structurante existante tranche le conflit). Pas d'`AskUserQuestion`.
> - `{type:"escalate", gate:"G1", …}` → `AskUserQuestion` (payload riche) : seuls les **vrais
>   conflits de valeur** entre règles (ex. transfert auto vs « transferts explicites ») reviennent
>   au PO.
> - **Fallback** : type `chef-de-projet` absent → `general-purpose` + « applique le skill
>   `chef-de-projet` ».

Cet étage est le **pont** entre `/4-retours` (qui a produit le backlog priorisé) et
`/2-make-gherkin` : il fait évoluer la **spec vivante** (source de vérité unique,
documentation du *pourquoi*) en une nouvelle version versionnée avant de générer les
prochains scénarios.

Argument (optionnel) : $ARGUMENTS — chemin du backlog `99-sprint<NN>-besoins-fin-itération.md`
(`<NN>` = numéro du sprint = préfixe 2 chiffres du dossier, ex. `99-sprint02-besoins-fin-itération.md`)
ou dossier de sprint le contenant.

## Déroulé

1. **Localise la spec courante (script).** Exécute
   `pwsh -NoProfile -File .claude/skills/spec-consolidation/scripts/find-spec.ps1`.
   Récupère le JSON : `currentSpec`, `currentVersion`, `nextSpec`, `nextVersion`.
   Repère le **backlog** : `$ARGUMENTS` s'il pointe un `99-sprint<NN>-besoins-fin-itération.md` (ou
   un dossier de sprint le contenant), sinon le `99-sprint<NN>-besoins-fin-itération.md` le plus
   récent sous `docs/sprints/*/`. **Ne lis ni la spec ni le backlog toi-même** — l'agent
   s'en charge.

2. **Dispatch (agent `spec-consolidation`).** Lance-le avec : le chemin du backlog,
   `currentSpec`, `nextSpec`, `currentVersion`, `nextVersion`, et le dossier de sprint
   clos pour contexte. Garde son `agentId`.
   - **Fallback** : type absent du registre → `general-purpose` avec « applique le skill
     `spec-consolidation`, mode agent orchestré (réutilise le format `redaction-spec`) »
     + les mêmes chemins. Ne bascule **pas** en inline.

3. **Boucle de consolidation (relais).** À chaque retour, l'agent renvoie
   `{ plan_consolidation, questions, synthese, done }`. Tant que `done` est faux :
   - Au **1er tour**, au plus **une ligne** de contexte (nb de besoins, collisions
     repérées) ; sinon n'écris rien.
   - Pour **chaque** entrée de `questions[]`, applique le **Protocole d'escalade CP** : dispatch
     `chef-de-projet` ; n'appelle `AskUserQuestion` que sur une `escalate` G1, en passant alors
     l'objet `question` du CP **telle quelle**.
   - Renvoie les réponses **brutes** (décision CP ou réponse PO) via `SendMessage` (même `agentId`).
   - Répète. **N'analyse pas**, **ne devine pas** la question suivante. Les collisions à
     résolution déterministe sont tranchées par le **CP** ; seuls les **vrais conflits de
     valeur** (ex. transfert auto vs règle « transferts explicites ») reviennent au **PO** (G1).

4. **Validation (CP).** Quand `done: true`, fais valider l'écriture par le **chef de projet**
   (dispatch `chef-de-projet` avec la `synthese`). `{type:"decision"}` (consolidation cohérente,
   collisions tranchées) → **ordonne d'écrire la nouvelle spec sans déranger le PO**.
   `{type:"escalate", gate:"G1"}` (un conflit de valeur subsiste) → `AskUserQuestion`, puis
   écris. Présente la `synthese` **verbatim** (version, ce qu'elle remplace, objectif/arbitre,
   séquence, règles conservées/révisées/nouvelles, risques) avec la décision CP ou l'escalade.

5. **Écriture (même agent).** À l'accord, `SendMessage` l'ordre d'écrire avec `nextSpec`.
   L'agent écrit `NN-specification.md` (format maison + blockquote de version) et renvoie
   `{ path, version, remplace, regles, notes }`.

6. **Propagation (mécanique).** Exécute
   `pwsh -NoProfile -File .claude/skills/spec-consolidation/scripts/find-spec.ps1 -PropagateReadme` :
   le script réécrit **mécaniquement** le pointeur « Spec courante » de `README.md` vers la
   nouvelle version courante et la ligne « versions précédentes » vers `NN-1` (`readmePropagated:true`).
   **Ne réécris pas le pointeur à la main** (l'étape manuelle a été sautée au sprint 03,
   pointeur périmé de 2 versions — cf. rétro). L'ancienne version reste figée en historique.

7. **Handoff make-gherkin.** Présente la nouvelle spec et **propose** d'enchaîner
   `/2-make-gherkin` sur elle (en ciblant le `prochain_sujet` du backlog) via
   `AskUserQuestion`. Si l'utilisateur valide, invoque `/2-make-gherkin` avec le chemin de
   la nouvelle spec + le slug du prochain sujet.

   > **Gate anti-bypass de la rétro.** L'itération précédente est close (backlog `/4`
   > écrit) : la **rétrospective de la méthode (`retro-sprint`, étape 1 de
   > `/6-cloture-sprint`) est impérative avant tout nouveau cycle** `/2-make-gherkin`.
   > `/2` refuse de démarrer si la rétro du dernier sprint clos manque (gate
   > `find-retro.ps1`). Ne présente pas l'enchaînement comme s'il pouvait sauter la rétro :
   > le chemin canonique est `/5-consolidation → /6-cloture-sprint (retro-sprint + push/PR) → /2-make-gherkin`.

8. **Commit.** Propose un commit de la nouvelle version de spec (sans pousser sauf demande
   explicite).

## Notes

- **Relais pur** : si tu te surprends à lire la spec, fondre un besoin ou trancher une
  collision toi-même, tu as quitté ton rôle — redélègue à l'agent.
- `AskUserQuestion` est appelé **par toi**, jamais par l'agent.
- Une question à la fois — pas de rafale.
- **Nouvelle version, ancienne figée** : la consolidation écrit `<NN+1>-specification.md`
  et ne modifie **jamais** `<NN>-specification.md` (historique).
- **Spec vivante** : la sortie décrit l'**état courant** cohérent (vision + règles), pas
  un changelog ; un lecteur qui ne lit qu'elle comprend l'application.
- Entrée attendue : un backlog `99-sprint<NN>-besoins-fin-itération.md` produit par `/4-retours`.
