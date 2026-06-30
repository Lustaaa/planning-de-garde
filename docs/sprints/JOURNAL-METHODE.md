# Journal méthode

Une ligne par amélioration de pipeline issue d'une **rétro conditionnelle** (`/cloture`).
Pas de doc de rétro dédié : « amélioration ou rien ». Format : `AAAA-MM-JJ — friction → fix`.

- 2026-06-29 — s16 Sc.3 incohérent (date 23/06 nommée « index 1 » alors que `ISOWeek(23/06) % 2
  = 0`) → dev-team stoppée et escalade. Fix : garde de cohérence date↔index/parité dans le chapeau
  PLANNING du `scrum-master` (vérifier `index = ISOWeek(date) % N` avant d'écrire tout scénario qui
  nomme date ET index/parité de cycle).
- 2026-06-29 — Refonte de la couche agile (hors pipeline) : 6 commands → 3 (`/planning`,
  `/sprint`, `/cloture`) ; ~10 agents → 3 (`scrum-master`, `dev-team`, `architecte`) ;
  paperasse de sprint → 1 fichier/sprint avec tableau en tête ; spec monolithique → `docs/specs/`
  éclatée éditée en diff ; rétro impérative → conditionnelle. Skill `dotnet` ajouté.
- 2026-06-30 — Migration intégrale (architecte, hors pipeline) : monolithe `docs/15-specification.md`
  (666 l.) → `docs/specs/` éclaté par sujet (11 fichiers neufs + `periodes-et-cycle-de-fond.md`
  préexistant). `docs/specs/` est désormais la **source complète et courante** ; le monolithe peut
  être archivé. Anti-duplication : une règle = un texte canonique (R11/12/14/15+R15bis dans
  `periodes-…`, référencées par le catalogue `regles-de-gestion.md`). `index.md` + `CLAUDE.md`
  (Source de vérité) resynchronisés.
- 2026-06-30 — s18 Sc.7 : flake P2 `FrontWasmInvitePlageIndisponibleTempsReel` rouge **2/3 runs
  full-suite** (vert isolé + re-run), visibilité en hausse sous charge SignalR → risque de blocage du
  gate de non-régression ou de mauvais diagnostic « régression ». Fix : garde-fou de **triage du flake
  *TempsReel* catalogué** dans `dev-team` (re-run ciblé pour confirmer le vert, consigne dans `notes`
  sans investiguer `src/`, ni RED ni vert-qui-ment, et signale la **montée de sévérité** au
  `scrum-master` pour prioriser le rétrofit P2). Dette + candidat +2 du backlog mis à jour.
