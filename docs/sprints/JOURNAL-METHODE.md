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
- 2026-07-01 — s21 : régression `FrontWasmConfigSupprimerActeurTempsReelTests` (rouge **3/3 en
  isolation, déterministe** — `RechargerRoles()` diffusait contre l'accusé de suppression) étiquetée
  « flake *TempsReel* » par dev-team et **a failli passer le gate** (détectée au G3 par le thread
  principal : re-run isolé + baseline s20 288/288 verte ; corrigée à la cause, commit `37bced4`). Le
  garde-fou triage-flake (rétro s18) était **sur-appliqué** faute de **discriminateur**. Fix : dans
  `dev-team`, exiger un **re-run EN ISOLATION x2-3** AVANT tout étiquetage — **N/N rouge déterministe =
  régression** (STOP, jamais « flake », jamais continuer sur re-run) ; seul un rouge **intermittent**
  (vert ≥1/N isolé, ou rouge seulement sous charge de suite) reste flake catalogué.
- 2026-07-02 — s25 : sous entorse G2 de preuve (doublure de port pour OAuth/mail/jetons non testables
  runtime), le tableau a affiché **16/16 ✅** alors que le câblage réel (adaptateurs SMTP/store jetons/
  providers OAuth + DI + écrans IHM) restait **en dette non branchée** → le login est vert en logique
  mais **non opérationnel** ; un ✅ franc sur les scénarios `@preuve-doublure` surestime l'état et trompe
  le PO. Fix : garde anti-✅-qui-ment dans le chapeau PLANNING du `scrum-master` — statut distinct
  (`✅ logique / ⚠️ câblage`) sur les lignes `doublure+manuel` tant que le câblage réel n'est pas prouvé,
  + ligne « dette de câblage » explicite au tableau, dette portée au backlog en `à faire` (P0 si non
  opérationnel).
- 2026-07-08 — s28 : au G3, le login démo (`deveaux.cyril@gmail.com`) échouait car le seed du compte
  de démo **court-circuitait** sur un compte email-only **préexistant** dans le store durable Mongo →
  mot de passe jamais posé (insert-si-absent au lieu de convergent). Le rework post-gate a fait **staller
  dev-team** (watchdog 600s) ; il a été **repris et fini par le thread principal**. Fix : garde-fou
  **« seed / amorçage par le chemin réel = CONVERGENT, jamais insert-si-absent »** dans `dev-team`
  (réconcilier l'état partiel préexistant + prouver les DEUX cas — absent ET partiel — sur store réel).
- 2026-06-30 — s18 Sc.7 : flake P2 `FrontWasmInvitePlageIndisponibleTempsReel` rouge **2/3 runs
  full-suite** (vert isolé + re-run), visibilité en hausse sous charge SignalR → risque de blocage du
  gate de non-régression ou de mauvais diagnostic « régression ». Fix : garde-fou de **triage du flake
  *TempsReel* catalogué** dans `dev-team` (re-run ciblé pour confirmer le vert, consigne dans `notes`
  sans investiguer `src/`, ni RED ni vert-qui-ment, et signale la **montée de sévérité** au
  `scrum-master` pour prioriser le rétrofit P2). Dette + candidat +2 du backlog mis à jour.
