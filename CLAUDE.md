# CLAUDE.md — planning-de-garde

App de planning de garde d'enfants partagé entre parents/intervenants. Pilotée par un **pipeline d'agents Claude Code** en boucle de sprint SCRUM. Ce fichier oriente ; il ne duplique pas les docs canoniques.

## Source de vérité

- **Produit** : `README.md` (pitch) · spec vivante **éclatée par sujet** sous `docs/specs/` (index navigable `docs/specs/index.md`), éditée **en diff**. **Migration intégrale faite** : `docs/specs/` est la **source complète et courante** (tout `docs/15-specification.md` y est migré). Les monolithes `docs/NN-specification.md` restent **figés en historique** — ne plus s'y référer comme source.
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

3 projets : `PlanningDeGarde.Tests` (domaine/app), `Api.Tests`, `Web.Tests`. Non-régression = **suite COMPLÈTE verte (458/458 au sprint 26)** via `dotnet test` **sans `--no-build` ni filtre, Docker actif** (pivot Mongo). Skill `dotnet` (JSON compact) : `pwsh -NoProfile -File .claude/skills/dotnet/scripts/test.ps1` (aussi `build.ps1`, `restore.ps1`).

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

Pipeline refondu (3 commands `/planning`,`/sprint`,`/cloture` · 3 agents `scrum-master`,`dev-team`,`architecte` · skill `dotnet` ; ancien pipeline archivé `.claude/_archive/`). Migration spec vers `docs/specs/` **faite**.

**26 sprints livrés** (suite 458/458). Dernier produit = **refonte graphique « Studio »** (s26 : thème clair/sombre persisté, typo Fraunces/Inter) ; auth complète (s22-s25), calendrier navigable + persistance Mongo de tout le domaine (s15). Backlog éclaté : `docs/BACKLOG-Done.md` (fait) + `docs/BACKLOG.md` (reste). Sprints clos archivés `docs/_archive/sprints/` (seul `JOURNAL-METHODE.md` reste dans `docs/sprints/`).

**Prochains gros items** (backlog) : câbler les adaptateurs auth réels (SMTP / OAuth providers / store jetons — dette assumée G2 s25, TÊTE), rétrofit du flake SignalR `*TempsReel*` (P1), cohérence config→planning, panneau cloche (palier 11). Reprendre la boucle via `/planning`.
