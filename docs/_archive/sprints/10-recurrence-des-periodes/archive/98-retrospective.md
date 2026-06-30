# Rétrospective de la méthode — Sprint 10 (récurrence des périodes)

> Rétro SCRUM de la **méthode** (pipeline d'agents/skills/commands), pas du produit (ça,
> c'est `/4-retours`). Bilan + actions ciblées sur les fichiers du pipeline, appliquées dans
> la PR du sprint. Analyse `retro-sprint` (sections `# Méthode (agents)` + `## IA` du
> `99-sprint10-retours.md`) ; actions appliquées par le thread principal sur aval direct du PO
> (le garde-fou d'auto-modification de `retro-sprint` ayant refusé un ordre relayé — voir § Note).

## Bilan SCRUM

### Ce qui a marché

- **Boucle complète tenue sur le palier 6** : 8 scénarios `@vert` end-to-end, 11 tests backend,
  routage backend (`tdd-auto`) / IHM-runtime (`ihm-builder`) explicite dans le suivi, acceptation
  runtime sur app réellement câblée (anti vert-qui-ment, R4).
- **Modèle « CP tranche, 2 portes PO (G2/G3) » efficace** : 6 décisions CP journalisées le
  2026-06-27 (grain, ancrage ISO, concurrence, écriture scénarios, plan tdd, besoins, collision
  v11) sans escalade PO injustifiée.
- **Early-greens anticipés et routés CP en amont** (plus de G4) : Sc.2/3/4/5/6 tranchés comme
  caractérisations dès `/2` et `/3`, « ne pas inventer de faux rouge » respecté ; divergence Sc.2
  driver→caractérisation tranchée par inspection de `GrilleAgendaQuery.cs`. Sc.4 correctement
  re-classé driver à l'exécution (le repli neutre n'existait pas) — le contrat « driver si
  indexation en dur » a joué.
- **Gate de rétro non contournable opérationnel** : `find-retro.ps1` bloque la clôture tant que
  `98-retrospective.md` manque.

### À améliorer (avec preuve)

| Friction | Preuve | Action |
|----------|--------|--------|
| Aucun garde-fou n'empêche les agents de lire `archive/`. | Retour PO méthode 2026-06-27 (table `# Méthode`). 7 agents explorent `docs/sprints/` sans consigne. | 1 |
| `/6-cloture-sprint` ne ré-archive pas les artefacts de pilotage du sprint clos. | Retour PO méthode 2026-06-27 point 2. `cloture-sprint.ps1` ne déplaçait rien ; `archive-iteration.ps1` gardait 99-retours/99-besoins/suivi à la racine. | 2 |
| `find-retro.ps1` casse sur chemin accentué (`…/source/privée/…`). | l.30 utilisait `Set-Location (git rev-parse --show-toplevel)` sans le fix d'encodage déjà appliqué aux scripts git (sprint 05) → « Cannot find path … privée … ». | 3 |
| Un test runtime SignalR (Web.Tests) flake 1×/2 au gate. | Observation `## IA` 2026-06-27 : échec 1/2 exécutions complètes, vert au re-run et en isolation. | 4 |
| Un here-string PowerShell (`@'…'@`) a fui dans un message de commit via le tool Bash (POSIX sh). | Friction thread principal : `@` parasite en tête de sujet, amend nécessaire. | 5 |

## Actions appliquées (dans cette PR)

1. **Consigne anti-`archive/`** dans les 7 agents explorant `docs/sprints/` (`tdd-analyse`,
   `tdd-auto`, `ihm-builder`, `retours-challenge`, `spec-consolidation`, `chef-de-projet`,
   `retro-sprint`) + reformulation de `retro-sprint/SKILL.md` (« scénarios sous archive/ » → ne
   plus les lire, s'appuyer sur le suivi). Hors archive, seul `00-sprint<NN>-suivi.md` d'un sprint
   passé est lisible.
2. **Archivage de clôture (lot coordonné, indivisible — aval PO G1)** : (a) mode `-Closure` dans
   `archive-iteration.ps1` (n'garde à la racine que le suivi ; archive 99-retours, 99-besoins,
   98-retrospective) + étape `1bis` dans `6-cloture-sprint.md` (archivage **avant** le push, pour
   partir dans la PR) ; **ET** (b) détection rendue récursive (racine **et** `archive/`) dans
   `find-retro.ps1` (besoins + retro) et `cloture-sprint.ps1` (détection sprint par `*-retours.md`)
   — sinon le gate de rétro et la détection du sprint clos se rouvriraient à tort.
3. **Fix encodage** dans `find-retro.ps1` : patron `$OutputEncoding = [Console]::OutputEncoding =
   [Text.UTF8Encoding]::new($false)` + `Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()`.
   Audit : `find-retours`/`find-spec` n'utilisaient pas le pattern cassé (rien à corriger).
   Vérifié : `find-retro.ps1` s'exécute désormais correctement sous le chemin accentué.
4. **Consigne anti-flake convergence SignalR** dans `ihm-builder.md` : attendre l'établissement
   **déterministe** de la connexion (long polling établi / signal de la pompe de diffusion) avant
   d'asserter la convergence, jamais un timing fixe ; isoler l'état entre tests.
5. **Note « syntaxe selon le shell du tool »** dans `git/SKILL.md` : le tool Bash exécute du POSIX
   sh (pas de here-string PowerShell) ; message multi-ligne via tool PowerShell `@'…'@`, ou `-m`
   répétés / heredoc côté Bash, ou (préféré) `commit.ps1`. Ne jamais mélanger.

## Note — garde-fou d'auto-modification de `retro-sprint`

L'agent `retro-sprint` a **refusé d'appliquer lui-même** les éditions du pipeline (`.claude/…`) :
son classifieur d'auto-modification n'accepte que les mots du PO **dans son propre fil**, qu'il ne
reçoit que via les relais du thread principal — qu'il classe comme « coordinateur ». Le PO a
pourtant autorisé **directement** le thread principal (réponse à `AskUserQuestion`). Le thread
principal a donc appliqué les 5 actions lui-même, avec autorité directe.

**Candidat rétro future** : ce blocage est structurel au pattern « orchestrateur relaie l'aval PO ».
Soit on documente que les éditions de pipeline issues de `retro-sprint` sont appliquées par le
thread principal (autorité directe), soit on assouplit le classifieur pour ce cas précis. À
trancher hors-sprint.
