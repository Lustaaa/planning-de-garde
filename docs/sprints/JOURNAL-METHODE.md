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
- 2026-07-08 — s29 : **scope creep répété au gate G3** — le PO a empilé 4 demandes successives au gate
  (dialog unifiée + retrait du champ enfant fantôme, bicolore sur pastille, puis D1 conditionnement à la
  garde, D2 multi-jours/config foyer, D3 transfert auto-dérivé), dont **3 hors goal G2** touchant un
  invariant (D1) et une porte métier (D3) → **2 reworks + 2 escalades G1** sous pression de gate. Fix :
  **discipline anti-scope-creep au gate** dans `/sprint` (étape 5) — au gate, un ajout est trié **par
  défaut vers le backlog** ; **absorption seulement si finition triviale DANS le goal** (aucun nouveau
  handler, aucune révision d'invariant/règle, aucune nouvelle surface IHM) ; tout ajout touchant un
  invariant/une porte métier/un nouveau volet **retourne au `/planning`** (G2/G1), jamais tranché sous la
  pression du gate.
- 2026-07-10 — s31 : **vert-qui-ment au gate G3** — Sc.10 (transfert dérivé) **vert** sur un store **semé
  sur-mesure** mais **invisible sur les données réelles** (la dérivation ne voyait que les successions de
  **périodes saisies**, jamais les **bascules du cycle de fond** qui pilotent le planning réel) → rework
  G3 (2ᵉ chemin « cycle-résolu »). Fix : garde **« acceptation runtime sur PROFIL DE DONNÉES RÉALISTE,
  jamais un seed sur-mesure adjacent »** dans `dev-team` (fixture représentative de l'état réel + rejeu
  sur store réel courant hors seed de test avant de déclarer vert).
- 2026-07-10 — s31 : **incident git concurrent** — un process/agent concurrent a fait `git checkout`
  (HEAD détaché hors branche) + committé pendant un scénario ; or `commit.ps1` faisait `git commit` **nu**
  qui committe **tout l'index** → risque de **happer des fichiers étrangers pré-stagés**. Fix : `commit.ps1`
  **scope le commit aux pathspecs** (`git commit -- @Files`), **refuse un HEAD détaché**, et **signale un
  index déjà peuplé** avant staging ; garde-fou documenté dans `git/SKILL.md`.
- 2026-07-10 — s32 : **refonte de SURFACE** (retirer l'édition inline des acteurs + brancher la modal) —
  un découpage naïf 1 scénario = 1 commit aurait ouvert une **fenêtre rouge multi-scénarios** (retirer
  l'inline avant que la modal ne porte l'écriture = suite non verte ; ~34 fichiers d'acceptation à migrer
  d'un parcours à l'autre). Le SM l'a anticipé en groupant Sc.1-4 en un **lot atomique « swap de surface »**
  (un commit, chaque scénario individuellement asserté) — sprint 7/7 sans rework, mais l'heuristique
  n'était **pas** encodée et le **2ᵉ incrément de l'épic Refonte Config foyer** (harmonisation rôles/cycle/
  enfants) rejouera le même motif. Fix : garde du **LOT ATOMIQUE sur une refonte de surface** dans le
  chapeau PLANNING du `scrum-master` (repérer le swap ancienne→neuve surface AVANT d'écrire les scénarios,
  grouper les scénarios inséparables en un seul commit, erreur/gating/temps-réel en incréments propres,
  jamais de coexistence durable ancienne+neuve).
- 2026-07-11 — s33 : **vert-qui-ment bUnit sur interaction clavier** — la fermeture Échap des modals Config
  foyer, testée verte via `@onkeydown`+focus sur le backdrop, était **non fonctionnelle en navigateur réel**
  (le keydown part de `document`, pas du div non focus) → 2 allers-retours au gate avant capture au niveau
  document via port `IEcouteurEchapModal`. Fix : garde-fou dans `dev-team` — les interactions clavier/focus
  (Échap, raccourcis) ne se prouvent **jamais** par `@onkeydown` bUnit ; capter au niveau `document` via port
  hexagonal (attaché à l'ouverture / détaché à la fermeture), preuve finale = **gate navigateur PO**.
- 2026-07-13 — s36 : **5ᵉ montée du flake P1 SignalR *TempsReel* (blast-radius), coût désormais tangible
  AU GATE** — le runner `test.ps1` (parallèle) a viré rouge **~1 fois sur 2** (4 runs : rouge/rouge/vert/
  vert ; Web.Tests isolé 258/258 vert ; `dotnet test slnx` **sérialisé** vert), soit un 2e/3e run manuel à
  CHAQUE gate. Contrairement aux sprints où « le protocole a tenu → skip », le coût récurrent est chiffrable
  et le remède identifié. Fix (2 volets, sans masquer de régression) : (1) `test.ps1` gagne un switch
  **`-Serial`** (`-- RunConfiguration.MaxCpuCount=1`) — suite COMPLÈTE mais assemblies sérialisées, mode gate
  anti-flake documenté dans `dotnet/SKILL.md` (relancer en série plutôt que rejouer toute la suite en
  parallèle) ; un rouge déterministe reste rouge en série, le triage flake s21 reste la règle. (2) Backlog :
  la dette « rétrofit *TempsReel* » (P1) est **remontée en TÊTE** avec la solution technique (collections
  xUnit non parallèles pour les tests I/O), à traiter avant tout nouveau client SignalR.
- 2026-07-14 — s37 : le switch `-Serial` (s36) a **prouvé sa valeur au gate** (parallèle 681/682 rouge sur
  le flake P1 *TempsReel*, `-Serial` 682/682 vert **déterministe en un run**, plus de rejeu manuel « en
  espérant un vert ») — mais l'étape gate de `/sprint` (5.1) prescrivait **encore `test.ps1` nu (parallèle)**,
  laissant l'opérateur du gate re-découvrir le flake puis basculer à la main à chaque sprint. Friction non
  la dette *TempsReel* (déjà tracée, candidat de tête) mais l'**absence de prescription du bon runner AU gate**.
  Fix : `/sprint` étape 5.1 exécute désormais **`test.ps1 -Serial` par défaut** au gate (mode déterministe
  anti-flake, aucune régression masquée), parallèle réservé au cycle TDD rapide de la dev-team. Le rétrofit à
  la cause (collections xUnit non parallèles pour l'I/O) reste la dette de fond au backlog.
- 2026-07-14 — s38 : **1er jet du graphe foyer temps-réel = GET `/api/foyer/graphe` sur push SignalR** →
  **amplification du flake P1 *TempsReel*** de ~40-50% (baseline mesuré sur `main` pré-s38) à **~100%** de rouge
  full-suite Web.Tests ; corrigé en **reprojetant côté client** depuis le payload diffusé (**0 GET**, ramené au
  baseline, aucune aggravation). Le `-Serial` par défaut au gate (s37) MASQUE ce coût, mais chaque nouveau client
  SignalR peut l'amplifier. Fix : garde-fou dans `dev-team` — **un nouveau client SignalR reprojette côté client
  depuis la diffusion, jamais un GET sur push** ; **mesurer le parallèle avant/après** l'ajout d'un client SignalR
  (aggravation = signal de conception, pas flake à cataloguer). Le rétrofit à la cause (collections xUnit non
  parallèles pour l'I/O) reste la dette de fond au backlog (candidat de tête).
- 2026-07-14 — s39 : **dette flake P1 SignalR *TempsReel* SOLDÉE À LA CAUSE** (candidat de tête depuis s36,
  5ᵉ+ montée chiffrée). Baseline mesuré **4/11 ≈ 36 % rouge** full-suite parallèle (victime UNIQUE
  `FrontWasmConfigEnfantsTempsReelTests`, isolé 3/3 vert = course de charge, pas régression). Remède = collection
  xUnit `SignalRTempsReelCollection` (`DisableParallelization=true`) **ciblée** sur les 55 `FrontWasm*TempsReel*`
  (**pas un rideau** : ~213 autres Web.Tests parallèles, `Tests`/`Api.Tests` inchangés ; le blast-radius SMTP/Mongo
  hypothétisé s29 n'a **jamais** rougi au baseline). La sérialisation a **démasqué 2 courses de convergence de TEST**
  (vertes en isolation = PAS des régressions produit) neutralisées par gardes déterministes (**0 assertion, 0
  `src/` produit touchés** ; course d'énumération s13 intacte). Résultat **36 % → 0 %** (12 runs) + `-Serial`
  695/695. **Décision pipeline (rétro) : `-Serial` RESTE le défaut au gate `/sprint` en ceinture + bretelles**
  (coût quasi nul, plus un contournement de flake), **la concurrence réelle étant désormais éprouvée par le cycle
  TDD parallèle de la dev-team** (fiable). Fix : `sprint.md` 5.1 + `dotnet/SKILL.md` (`-Serial`) requalifiés
  « flake soldé s39 » (fin du récit « parallèle rougit couramment »). Le triage durci s21 (re-run isolé x2-3,
  `N/N rouge = régression`) a **discriminé** flake vs régression à chaque étape — il tient et reste la règle.
- 2026-07-15 — s44 : **3 retours PO successifs au gate G3 = rework @ihm en cascade** — la surface d'écriture
  de la délégation (bouton sur la carte du jour), puis l'**existence même** des surfaces de lecture, ont été
  arbitrées **après** implémentation : (1) surface carte→**menu clic-case**, (2) **retrait du panneau « À venir »**
  (s43), (3) **retrait de la carte « Aujourd'hui »** (s42) — Sc.4-6 refaits + un Sc.7 de démolition ajouté en cours
  de sprint. Le choix de surface (emplacement + affordance + son existence) est une **décision de CONCEPTION PO**
  qui aurait dû tomber **au cadrage**, pas au gate visuel. Fix : garde de la **PORTE DE CONCEPTION « surface »**
  dans le chapeau PLANNING du `scrum-master` — dès qu'un goal introduit une surface d'action d'écriture ou de
  lecture **neuve/déplacée**, la remonter comme **point de conception explicite** (emplacement retenu + alternatives
  écartées) pour validation PO **avec/juste après G2**, et ne mener les scénarios @ihm en RED→GREEN **qu'une fois la
  surface arbitrée** (coût nul au cadrage vs @ihm refaits au gate).
- 2026-07-18 — s49 : **3 échecs au gate G3 sur le drag alors que le CODE ÉTAIT CORRECT** — cause réelle
  trouvée seulement au 4ᵉ tour (via un harnais Playwright) = le PO testait un **BUILD SERVI PÉRIMÉ**. Le
  service `build` one-shot de `docker-compose` compile le Web dans le volume `build-artifacts` ; le conteneur
  `web` sert `--no-build` **depuis ce volume** et n'ayant **pas été recréé**, resservait un artefact WASM
  antérieur au câblage drag s49. Les hard refresh du PO ne changeaient rien (serveur = ancien WASM) → **3
  diagnostics/correctifs de code inutiles** (la source était déjà correcte). Fix (2 volets) : `/sprint` étape
  5.1 — (1) **GARANTIR que le build servi = source courante AVANT de solliciter le PO** (rebuild explicite du
  stack : `docker compose up build --force-recreate` puis `up -d --force-recreate web api`) ; (2) **geste
  navigateur (drag souris, `elementFromPoint`, interop JS) = HORS bUnit → prévoir un SMOKE Playwright**
  (projet `tests/PlanningDeGarde.Web.E2E`, hors `.slnx`) pour observer sur l'app servie plutôt qu'itérer à
  l'aveugle via le PO.
- 2026-06-30 — s18 Sc.7 : flake P2 `FrontWasmInvitePlageIndisponibleTempsReel` rouge **2/3 runs
  full-suite** (vert isolé + re-run), visibilité en hausse sous charge SignalR → risque de blocage du
  gate de non-régression ou de mauvais diagnostic « régression ». Fix : garde-fou de **triage du flake
  *TempsReel* catalogué** dans `dev-team` (re-run ciblé pour confirmer le vert, consigne dans `notes`
  sans investiguer `src/`, ni RED ni vert-qui-ment, et signale la **montée de sévérité** au
  `scrum-master` pour prioriser le rétrofit P2). Dette + candidat +2 du backlog mis à jour.
