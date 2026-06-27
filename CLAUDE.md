# CLAUDE.md — planning-de-garde

App de planning de garde d'enfants partagé entre parents/intervenants. Pilotée par un **pipeline d'agents Claude Code** en boucle de sprint SCRUM. Ce fichier oriente ; il ne duplique pas les docs canoniques.

## Source de vérité

- **Produit** : `README.md` (pitch) · spec vivante versionnée `docs/NN-specification.md` (courante = la plus haute, **v10**). Les versions antérieures restent figées en historique.
- **Backlog** : `docs/BACKLOG.md` (fait / à faire, séquence des paliers).
- **Pipeline / méthode** : `README-claude.md` (cycle de sprint, étages, conventions de relais).

## Architecture (Clean / hexagonale, DDD + CQRS)

Solution `PlanningDeGarde.slnx` — 8 projets `src/` + 3 projets `tests/`.

- **Domain** — modèle métier pur, aucune dépendance techno.
- **Application** — use cases (canal requête/réponse), ports gauche/droite.
- **AdapterDroite.InMemory** / **AdapterDroite.Mongo** — adaptateurs de droite par techno (dépôts ; config foyer persistée Mongo).
- **SignalR** — adaptateur de **gauche** (diffusion temps réel, lecture seule).
- **Api** — hôte d'API **détaché** (démarre seul, expose OpenAPI + UI explorable, CORS).
- **Web** — front **Blazor WebAssembly**, consomme l'API comme une **API distante**.
- **Infrastructure** — câblage / DI transverse.

Règles d'or : le front n'appelle jamais le domaine en direct (tout passe par l'API) ; **écriture** = canal requête/réponse, **diffusion** = SignalR lecture seule, jamais confondus ; données derrière des ports (jamais figées dans le code).

## Tests

3 projets : `PlanningDeGarde.Tests` (domaine/app), `Api.Tests`, `Web.Tests`. Non-régression = **suite COMPLÈTE verte (161/161)** via `dotnet test` **sans `--no-build` ni filtre, Docker actif** (pivot Mongo). Outil compact : `.claude/.../test-count.ps1`.

## Lancer l'app

Skill `run` (script `run.ps1`) : démarre `Api` détaché puis le front `Web` WASM. Préférer ce skill à un `dotnet run` manuel. `docker-compose.yml` monte le store Mongo.

## Pipeline de sprint (relais pur)

Le thread principal **ne raisonne pas** : il dispatche des subagents et relaie via `AskUserQuestion`. Boucle : `/1-spec` → `/2-make-gherkin` → `/3-tdd-implement` (+ gate visuel) → `/4-retours` → `/5-consolidation` → `/6-cloture-sprint` → reboucle `/2`.

- **Backend d'abord, IHM en fin** : scénarios s'arrêtent à la frontière Application ; IHM Blazor + SignalR réel = phase finale (`ihm-builder`).
- **Portes PO (2 seules)** : **G2** sprint goal, **G3** gate visuel. + git sortant confirmé. Tout le reste tranché par le **chef de projet (CP)**, décisions journalisées dans `99-sprint<NN>-retours.md`.
- Suivi : `docs/sprints/<sujet>/00-sprint<NN>-suivi.md` + un `NN-slug.md` par scénario (⏳/🔴/✅).
- **Acceptation runtime obligatoire** (rempart anti vert-qui-ment) : prouver sur câblage réel / store réel, pas par doublures.

## Conventions

- Réponses en **français** ; identifiants/classes/méthodes en anglais.
- Branche `ia-{type}/{slug}` ; **jamais** de commit sur `main` ni de `git add -A`. PR via `gh`. Préférer le skill `git`.
- Plan avant implémentation pour toute tâche non triviale.

## État courant

Spec **v10** livrée jusqu'au **palier 5** (config foyer persistante : ajout d'acteurs + Mongo). Refacto technique hors-pipeline **faite** (PR #21 mergée : adaptateurs droite par techno, SignalR adaptateur gauche, rangement par type, pipeline allégé). **Prochain sujet = palier 6, récurrence des périodes**, via `/2-make-gherkin` sur `docs/10-specification.md`.
