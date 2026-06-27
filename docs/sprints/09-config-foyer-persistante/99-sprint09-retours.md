# Retours — Sprint 09 (config-foyer-persistante)

> **Fichier unifié.** Il porte trois choses, consommées par des étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
> - **Décisions autonomes (chef de projet)** en fin de fichier → lues par `retro-sprint`.
>
> Section « Décisions autonomes » amorcée au cadrage `/2-make-gherkin` ; sections produit +
> méthode **complétées** par `tdd-analyse` au `/3`, qui **préserve** la section « Décisions
> autonomes (chef de projet) ». La partie produit est préparée vide ici et remplie par le PO
> **après le gate visuel** ; la partie méthode est appendée au fil de l'eau par le thread
> principal. Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1` (Mongo via Docker —
> démarrer le conteneur avant l'API).

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.

## IHM - général

-

## IHM - /configuration

-

## IHM - /planning

-

## Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

# Idée pour la suite

> Idées produit que le PO veut verser au backlog pour de futurs sprints (pas forcément le
> prochain). Consommées par `/4-retours` (classées/séquencées) puis replacées dans les épics
> du BACKLOG. Laisser vide si aucune.

-

# Consigne pour la suite

> Consignes directes du PO sur l'orientation à donner à la suite (priorité, cap, contrainte
> de séquencement). Pèsent sur le choix du prochain sujet en `/4-retours` (G2). Laisser vide
> si aucune.

-

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-27 | pipeline /1→/6 (rythme global) | PO : les sprints prennent trop de temps. Décision PO : faire une **refacto technique HORS processus** (pas via make-gherkin/TDD piloté) avant de reprendre le pipeline. | Sprint 09 clôturé normalement ; le prochain chantier est une refacto hors pipeline. `retro-sprint` doit traiter en priorité la VÉLOCITÉ du pipeline (nombre d'allers-retours, coût des gates, granularité scénarios). |

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-27 | tdd-auto / non-régression | Sc.1 a introduit une énumération async dans `ConfigurationFoyer` qui a rendu flaky/cassé un test runtime s08 (handler stale `UnknownEventHandlerIdException`) — non détecté au commit Sc.1, révélé seulement au RED de Sc.2 par la suite complète. | Confirme la valeur de la non-régression complète sans `--no-build` ; envisager que tdd-auto relance aussi les tests runtime Web.Tests existants après tout ajout touchant un composant partagé. |

| 2026-06-27 | run/test runtime Sc.9 | `FrontWasmConfigApiInjoignableTempsReelTests` (socket réel) échoue quand Docker Desktop tourne : son proxy loopback altère la sémantique `ConnectionRefused`. Préexistant (reproduit sur HEAD propre via `git stash`), indépendant de Sc.3. Brouille la non-régression complète tant que Mongo/Docker tourne. | Rendre le test runtime « API injoignable » robuste au proxy loopback Docker (port garanti fermé / assertion d'échec transport plus large), ou documenter le prérequis d'environnement. À traiter au moment de Sc.9. |
| 2026-06-27 | suite runtime Web.Tests « TempsReel » | Au-delà de Sc.9 : toute la famille bUnit « TempsReel » (sockets réels + timing) est FLAKY quand Docker tourne — échecs non-déterministes, variables d'un run à l'autre (HorsSetNeutre, Recolorier, NomVide…), alors que HEAD propre = 37/37. Constaté pendant Sc.5 (backend pur, ne touche aucun code Web). Risque majeur : masque/imite un vrai RED, brouille la non-régression pour Sc.8/Sc.9 qui AJOUTENT du runtime Web. | **RÉSOLU au Sc.9** : abandon du port loopback réellement libéré (sémantique altérée par le proxy Docker) au profit d'un handler de transport déterministe `GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable` qui lève `HttpRequestException` sur le seul POST ciblé, lecture transitant vers l'API live. Bug bUnit secondaire (`UnknownEventHandlerId` re-render) corrigé par `WaitForState`. Prouvé stable ≥5× Docker actif, suite 161/161. À capitaliser en convention runtime (retro-sprint). |

## Notes de contexte (décisions produit, hors méthode)

-

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées par l'agent `chef-de-projet` sans escalade PO.
> Permet au PO de piloter a posteriori.

## 2026-06-27 — Découpe du palier 5 (ajout d'acteur + persistance Mongo) dans ~2h IA

- **Question (make-gherkin)** : le palier 5 fond ajout d'acteur + persistance Mongo réelle
  dans ~2h IA. Quel ordre de bataille borne le périmètre (et donc ce que couvre le pivot
  « survie au redémarrage ») ?
- **Décision (CP, sans escalade)** : option 1 — **FUSIONNÉ**. L'**ajout** d'acteur (id stable
  neuf généré, jamais dérivé du libellé) **et** l'**édition** du seed (rename/recolor s08) sont
  **durables d'emblée sur store Mongo réel**. Le **pivot redémarrage** couvre l'acteur **ajouté
  ET** l'édition (ex. Alice→Alicia + nounou Carla survivent au redémarrage). **Tranche de
  secours** (décidée au `/3` UNIQUEMENT si débordement réel ~2h, pas maintenant) : persister
  **d'abord le référentiel semé** (pivot sur l'édition seule), **reporter l'ajout** en tranche 2.
- **Rationale** : c'est le **cap PO complet** (ajout **et** Mongo, acté G2 au /4-retours s08).
  La découpe-dans-le-budget relève du **garde-fou découpe** (remit CP, ambition ~2h) ; la
  tranche de secours est **déjà documentée** (spec v09 R1, backlog). Couper *maintenant* (option
  2 ou 3) amputerait le cap sans nécessité prouvée — on coupe au débordement réel, pas par
  précaution. L'option 3 (ajout volatile d'abord) **retire l'observable de durabilité** que le
  PO a explicitement voulu → écartée. Aucun arbitrage métier neuf.
- **Guidage de cadrage transmis à make-gherkin (craft, pas un arbitrage PO)** :
  - **Forme de l'id stable neuf** : opaque, généré (GUID ou séquence « autre-N »), **jamais
    dérivé du libellé** (anti-pattern libellé-comme-identité corrigé au s06) ; **unique** (jamais
    de collision avec un id existant).
  - **Ajout → légende** : un acteur **ajouté sans période** est présent **immédiatement dans
    l'écran config** mais **n'apparaît PAS en légende/case** tant qu'aucune période ne le porte
    (pas d'entrée fantôme, cohérent s08 Sc.6). Inclure un scénario « ajout → affecter une période
    → apparaît en légende avec id neuf + couleur » pour prouver que l'id neuf circule.
  - **Then du pivot** : prouver la survie au redémarrage sur **store Mongo RÉEL** via la **grille
    réellement câblée** (case + légende nommée après redémarrage) — preuve la plus forte
    (anti vert-qui-ment, R4) ; lecture via le port sur instance fraîche acceptable en complément.
  - **Borne** : ajout **sans suppression** d'abord ; **pas** d'édition du cycle de fond ; cases
    orphelines hors périmètre. Le **reste du domaine** (slots/périodes/transferts) reste
    **InMemory** (borne anti-cliquet, règle 30).
- **Sources** : spec v09 règles 6 + 30 + R1 ; besoins /4-retours s08 (G2 PO, révision d'arbitre
  bornée) ; garde-fou découpe ; acquis s08 (ConfigurationFoyerEnMemoire, EditerActeur, ports).

## 2026-06-27 — Contrainte technique PO : Mongo démarrée via Docker

- **Consigne PO (cadrage technique)** : la base **Mongo doit être démarrée via Docker**
  (conteneur), **pas** un serveur embedded/in-process ni un Mongo installé en local.
- **Implications pour `/3` (à cadrer par tdd-analyse/tdd-auto)** :
  - **Outillage** : ajouter un **`docker-compose`** (service Mongo) ; le skill/`run.ps1` doit
    **démarrer le conteneur Mongo** avant l'API (ou documenter le prérequis). Garde-fou
    d'outillage (cf. section BACKLOG « Conteneurisation Docker »), sans observable métier.
  - **Adaptateur** : l'adaptateur de droite durable se connecte au Mongo du conteneur
    (chaîne de connexion configurable, ex. `mongodb://localhost:27017`).
  - **Test d'intégration du pivot (Sc.3)** : exige un **Mongo RÉEL tournant** (conteneur
    Docker) — l'acceptation « survie au redémarrage » se prouve contre ce store, jamais une
    doublure (anti vert-qui-ment, R4). Prévoir le **démarrage/teardown** du conteneur autour
    du test (ou un Mongo de test dédié), et un **skip propre** si Docker indisponible plutôt
    qu'un faux vert.
- **Sources** : consigne PO directe (2026-06-27) ; BACKLOG « Conteneurisation Docker » (garde-fou
  d'outillage) ; spec v09 R4 (acceptation runtime sur store réel).

## 2026-06-27 — Ancrage de la refacto hors-pipeline dans la spec v10 (empreinte minimale)

- **Question (spec-consolidation, id `ancrage-refacto-hors-pipeline`)** : où/comment acter dans
  la spec vivante v10 la direction « refacto technique de restructuration du code applicatif,
  menée HORS make-gherkin, à iso-comportement strict » SANS inventer de mécanique fonctionnelle
  ni la transformer en palier d'usage ?
- **Décision (CP, sans escalade)** : **option 1 — empreinte minimale, aucun palier nouveau**.
  1) **Étendre le blockquote « Garde-fous hors-spec »** (qui porte déjà le code-behind, l'API
     explorable, la conteneurisation) pour y acter la refacto comme **garde-fou de structure
     sans observable métier** : invariant **iso-comportement** + critère de sortie **161/161
     `dotnet test` sans `--no-build` ni filtre, Docker actif** (pivot Mongo inclus). 2) **Risques**
     : consigner « refacto hors gate TDD piloté → régression invisible » avec sa mitigation
     (le critère 161/161 comme condition de fin) + la **borne anti-débordement** (iso-comportement,
     persistance du reste du domaine NON tirée en avant — renvoi règle 30 / borne anti-cliquet).
     3) **Pointeur « Prochain sujet »** : reste **palier 6 — récurrence des périodes**, précédé
     **hors-pipeline** par la refacto (mention de séquencement, pas un incrément produit).
  - **NE PAS** créer de palier technique dans la Séquence (option 2 : se ferait passer pour un
    incrément produit observable — faux). **NE PAS** alourdir le Contexte d'usage d'une note
    d'archi (option 3). **NE PAS** rester muet (option 4) : le séquencement réel — refacto AVANT
    reprise palier 6 — doit rester fidèlement traçable dans la spec, source de vérité du « et après ».
- **Rationale** : convention **« la spec vivante décrit la vision / le pourquoi produit, pas la
  technique ni l'historique »** + principe d'**empreinte minimale**. Le blockquote « Garde-fous
  hors-spec » est précisément le réceptacle existant des chantiers de structure/outillage **sans
  règle de gestion ni palier** — y rattacher la refacto est l'option qui n'invente **aucune**
  mécanique fonctionnelle. Aucun arbitrage métier (pas d'observable, pas de règle neuve) → pas
  de G1. La direction PO « refacto avant reprise du pipeline » est une **consigne de séquencement
  processus**, pas une valeur produit en compétition.
- **Garde-fous de rédaction transmis à spec-consolidation (craft, pas un arbitrage PO)** :
  - **Aucune nouvelle règle de gestion**, **aucun nouveau palier numéroté** : la v10 reste, sur
    le fond produit, **iso-v09** (la refacto ne modifie aucun observable).
  - Le **critère 161/161** se formule comme **garde-fou de sécurité** (non-régression prime sur
    la vitesse, arbitre de sécurité non négociable du backlog s09), pas comme une mécanique d'usage.
  - Réutiliser les bornes **déjà écrites** (règle 30, borne anti-cliquet, R « édition vs
    persistance ») plutôt que d'en réécrire : la refacto **ne tire pas** la persistance du reste
    du domaine devant l'usage (slots/périodes/transferts restent InMemory).
  - Garder le wording **court** : 1–3 lignes ajoutées au blockquote + 1 puce Risques. Pas de
    section dédiée.
- **Sources** : convention spec vivante (pourquoi produit, pas la technique/l'historique) ;
  principe empreinte minimale ; spec v09 blockquote « Garde-fous hors-spec » + règle 30 + borne
  anti-cliquet + R « édition vs persistance » ; backlog s09 `99-besoins-fin-itération` (arbitre
  de sécurité 161/161 non négociable, refacto hors-pipeline, palier 6 séquencé derrière).
