# CLAUDE.md — planning-de-garde

App de planning de garde d'enfants partagé entre parents/intervenants. Pilotée par un **pipeline d'agents Claude Code** en boucle de sprint SCRUM. Ce fichier oriente ; il ne duplique pas les docs canoniques.

## Source de vérité

- **Produit** : `README.md` (pitch) · spec vivante **éclatée par sujet** sous `docs/specs/` (index navigable `docs/specs/index.md`), éditée **en diff**. Migration en cours : tant qu'un sujet n'y est pas découpé, la source figée reste `docs/15-specification.md` (dernière monolithique). Les `docs/NN-specification.md` restent figées en historique.
- **Backlog** : `docs/BACKLOG.md` (backlog produit **vivant** : retours persistants fait / à faire, source des goals candidats).
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

3 projets : `PlanningDeGarde.Tests` (domaine/app), `Api.Tests`, `Web.Tests`. Non-régression = **suite COMPLÈTE verte (161/161)** via `dotnet test` **sans `--no-build` ni filtre, Docker actif** (pivot Mongo). Skill `dotnet` (JSON compact) : `pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1` (aussi `build.ps1`, `restore.ps1`).

## Lancer l'app

Skill `run` (script `run.ps1`) : démarre `Api` détaché puis le front `Web` WASM. Préférer ce skill à un `dotnet run` manuel. `docker-compose.yml` monte le store Mongo.

## Pipeline de sprint (relais pur)

Le thread principal **ne raisonne pas** : il dispatche des subagents et relaie via `AskUserQuestion`. Boucle : **`/planning` → `/sprint` → `/cloture` → reboucle `/planning`**.

- **3 agents** : `scrum-master` (décision + orchestration méthode, ex-CP ; chapeaux planning / décision / clôture), `dev-team` (seul à coder : TDD/DDD/BDD/CQRS/hexa, backend puis IHM), `architecte` (**hors-sprint**, bypass méthodo, fait exactement la consigne technique du PO puis resynchronise la doc ; exclusif avec `dev-team`).
- **Backend d'abord, IHM en fin** : scénarios `@back` s'arrêtent à la frontière Application ; scénarios `@ihm` menés RED→GREEN runtime ; IHM restante en phase finale.
- **Portes PO (2 seules)** : **G2** sprint goal (3-4 goals candidats proposés par le SM depuis le backlog), **G3** gate visuel. + git sortant confirmé. Tout le reste tranché par le **`scrum-master`**.
- **1 fichier par sprint** `docs/sprints/NN-<slug>.md` : **tableau d'avancement en tête** (X/N, ⏳/🔴/✅) + scénarios Gherkin + section retours. Plus de dossier de suivi ni de fichier-par-scénario.
- **Rétro méthode conditionnelle** : seulement si friction réelle → un edit pipeline + 1 ligne dans `docs/sprints/JOURNAL-METHODE.md` (« amélioration ou rien »).
- **Acceptation runtime obligatoire** (rempart anti vert-qui-ment) : prouver sur câblage réel / store réel, pas par doublures.

## Conventions

- Réponses en **français** ; identifiants/classes/méthodes en anglais.
- Branche `ia-{type}/{slug}` ; **jamais** de commit sur `main` ni de `git add -A`. PR via `gh`. Préférer le skill `git`.
- Plan avant implémentation pour toute tâche non triviale.

## État courant

Pipeline **refondu** (branche `ia-refactor/refonte-pipeline-agile`) : 6 commands → 3 (`/planning`, `/sprint`, `/cloture`), ~10 agents → 3 (`scrum-master`, `dev-team`, `architecte`), skill `dotnet` ajouté, ancien pipeline archivé sous `.claude/_archive/`. Design : `docs/superpowers/specs/2026-06-29-refonte-pipeline-agile-design.md`. **Reste à faire** : migrer `docs/15-specification.md` vers `docs/specs/` par sujet (au fil des sprints), puis reprendre la boucle via `/planning`. Dernier produit livré = palier 14 (calendrier navigable + persistance Mongo).
