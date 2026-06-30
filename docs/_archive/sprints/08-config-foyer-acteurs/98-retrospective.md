# Rétrospective de la méthode — Sprint 08 (config-foyer-acteurs)

> **Sprint retrospective SCRUM** sur la **méthode** (pipeline d'agents / skills / commands),
> distincte de `/4-retours` (rétro produit). Dernier maillon de la boucle : les améliorations
> retenues partent dans la PR du sprint. Actions priorisées par le PO (multiSelect).

## Bilan

### Ce qui a marché (avec preuve)

- **Pipeline robuste à la mort d'un agent.** Malgré un watchdog 600s sur l'agent IHM au Sc.6,
  les 10 scénarios sont @vert (runtime IHM 9/9 + Sc.10 store-level) — preuve
  `00-sprint08-suivi.md`. La reprise hors-agent par le thread principal (vérif test
  signifiant + vert, tag `@vert`, fix compteur, commit) a fonctionné.
- **Ancrage `--no-build` partiellement efficace.** L'agent IHM a lancé son réflexe
  `--no-build` puis s'est **auto-corrigé** avec un run autoritaire sans `--no-build` (l'action
  rétro s07 a mordu, même chez un fallback general-purpose).
- **Sections forward livrées en cours de sprint.** Demande PO au gate G3 → `tdd-analyse`
  scaffolde désormais « Idée pour la suite » + « Consigne pour la suite » dans tout fichier de
  retours, consommées par `retours-challenge` (commit `69ee4b9`).
- **`chef-de-projet` — 3 décisions autonomes** (périmètre incrément volatile, exception Mongo
  bornée règle 29, concurrence dernière-écriture-gagne) tranchées sans escalade PO,
  journalisées (`99-sprint08-retours.md` § Décisions autonomes).

### À améliorer (avec preuve)

1. **Mort d'agent au watchdog 600s (Sc.6 runtime).** L'agent IHM a calé en plein milieu, test
   runtime écrit mais ni vérifié ni commité, scénario source resté `@pending` ; reprise
   manuelle hors-agent nécessaire. `3-tdd-implement.md` ne prévoyait aucune procédure de
   reprise.
2. **Réflexe `--no-build` réapparu chez le fallback IHM.** L'ancrage s07 ne vivait que dans
   `tdd-auto.md` + skill `tdd-implement`, non lus par un general-purpose dispatché sur
   `ihm-builder` ; `ihm-builder.md` n'en parlait pas.
3. **Désync du compteur de suivi.** À la mort de l'agent, l'agrégat « Acceptation runtime IHM
   X/N » était resté en retard sur la liste détaillée des scénarios (nombre ≠ lignes ✅).
   Mineur, corrigé par le thread principal.

## Actions appliquées (validées PO : 2, 3, 4 — l'action 1 « efficacité anti-watchdog » écartée)

| # | Cible | Édition appliquée |
|--:|-------|-------------------|
| 2 | `.claude/commands/3-tdd-implement.md` (Notes) | **Procédure de reprise sur mort d'agent** (watchdog/stall) gravée : vérifier test signifiant + vert (sans `--no-build`), taguer `@vert` source + slug, resynchroniser le compteur du suivi, commiter ; ne pas re-dispatcher aveuglément sur un état à moitié. |
| 3 | `.claude/agents/ihm-builder.md` (VERIFY + Anti-règles) **+** `.claude/commands/3-tdd-implement.md` (fallback IHM étapes 4 et 7) | **Interdiction `--no-build` ancrée côté IHM** : la non-régression recompile tous les projets, jamais `--no-build` ni filtre partiel (vert qui ment, cf. Sc.1 s07) ; rappel d'une ligne ajouté aux prompts de dispatch IHM/runtime fallback. |
| 4 | `.claude/agents/ihm-builder.md` (COMMIT) **+** `.claude/agents/tdd-auto.md` (garde-fou suivi) | **Cohérence agrégat/liste du suivi** : mettre à jour l'agrégat « N/N » en même temps que la liste détaillée — le nombre doit toujours égaler le nombre de lignes ✅. |

> **Action écartée par le PO** : 1 — « efficacité anti-watchdog (ihm-builder) » (un seul run
> autoritaire, commit prompt). Non retenue ce sprint.

> **Note de process.** Les éditions sous `.claude/` (self-modification du pipeline) ont été
> appliquées par le thread principal après **réponse directe du PO** à l'`AskUserQuestion` de
> priorisation (le subagent `retro-sprint` est bloqué par le classifieur self-modification, qui
> n'accepte pas une autorisation relayée). Garde-fou respecté.

## Suite

- Améliorations embarquées dans la PR du sprint 08 (`/6-cloture-sprint`).
- Prochain sujet (backlog `/4-retours`) : **config-foyer-persistante** (ajout d'acteurs +
  persistance Mongo bornée à la config foyer), à amorcer en `/2-make-gherkin` sur la spec v09.
