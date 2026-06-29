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
