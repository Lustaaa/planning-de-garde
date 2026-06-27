# Retours — Sprint 10 (recurrence-des-periodes)

> **Fichier unifié.** Il porte trois choses, consommées par des étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
> - **Décisions autonomes (chef de projet)** en fin de fichier → lues par `retro-sprint`.
>
> Section « Décisions autonomes » amorcée au cadrage `/2-make-gherkin` ; sections produit +
> méthode **complétées** par `tdd-analyse` au `/3`, qui **préserve** la section « Décisions
> autonomes (chef de projet) ». La partie produit est préparée vide ici et remplie par le PO
> **après le gate visuel** ; la partie méthode est appendée au fil de l'eau par le thread
> principal.

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

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|

## Notes de contexte (décisions produit, hors méthode)

-

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées par l'agent `chef-de-projet` sans escalade PO.
> Permet au PO de piloter a posteriori.

## 2026-06-27 — Grain du plus petit incrément observable du cycle de fond (palier 6)

- **Question (make-gherkin)** : quel est le plus petit incrément observable de la
  « récurrence des périodes » (grain du cycle de fond) pour ce palier 6 ?
- **Décision (CP, sans escalade)** : **option 1 — alternance hebdomadaire paire/impaire**.
  Cycle de **N semaines** avec **UN responsable de fond par semaine** (ex. 2 sem. :
  paire→A, impaire→B), **définissable/éditable depuis la config foyer**. La grille **résout
  le responsable de fond** d'un jour sans période explicite ; les **périodes saisies restent
  des surcharges ponctuelles** (priorité surcharge > fond > neutre). Le cycle vit **EN
  MÉMOIRE ici** (durabilité séquencée au palier 9, borne anti-cliquet règle 30 respectée).
  **Option 2 (grain journalier)** écartée : saisie lourde, débordement probable > ~2h IA —
  contraire au corollaire de découpe. **Option 3 (responsable unique, sans alternance)**
  écartée comme cap : ne livre **PAS** la règle 11 (cycle multi-semaines) ; conservée
  **uniquement comme tranche de secours** si débordement réel constaté au `/3`.
- **Rationale** : option 1 colle **mot pour mot** à la **règle 11** (« le planning se répète
  selon un cycle de plusieurs semaines — ex. semaine paire/impaire — définissable et éditable
  depuis la config foyer ») et à la **règle 7** (responsabilité de fond en config, calendrier
  = surcharges) + **règle 12** (exception ponctuelle sans casser le cycle). C'est le **plus
  petit incrément qui livre l'observable** du palier 6 sans amputer la règle. Aucune **règle
  de gestion neuve** (règle 11 préexiste) → pas de G1 métier. Le **sprint goal** (palier 6,
  cap déjà acté) n'est pas redéfini → pas de G2. Le **grain** est une décision de **découpe**
  (remit CP, ambition ~2h IA). La mise EN MÉMOIRE respecte la **borne anti-cliquet** (règle 30 :
  durabilité du cycle de fond explicitement portée par le palier 9). Le **risque « coût de
  saisie du cycle »** (spec, Risques & questions ouvertes) est tenu par option 1 (un seul
  responsable/semaine), aggravé par option 2.
- **Guidage de cadrage transmis à make-gherkin (craft, pas un arbitrage PO)** :
  - **Couche orthogonale** : le cycle de fond est une **couche de résolution** sous les
    périodes explicites. Priorité de résolution **surcharge (période saisie) > fond (cycle) >
    neutre**. Prévoir un scénario prouvant qu'une **période explicite surcharge** le fond sur
    le jour concerné, et que le **fond reprend** hors de la surcharge (règle 12).
  - **Observable de grille** : un jour **sans période explicite** affiche le **responsable de
    fond** résolu par le cycle (case **nommée + colorée** sur l'**identifiant stable** de
    l'acteur — pas le libellé, R « couleur résolue sur id stable », règle 19), et la **légende**
    suit. Acceptation runtime sur **grille réellement câblée** (anti vert-qui-ment, R4).
  - **Édition en config** : définir le **nombre de semaines** du cycle et **affecter un
    responsable par index de semaine** depuis l'écran config foyer ; rééditer le mapping met
    **immédiatement** à jour la grille (cohérent avec l'édition acteurs s08).
  - **Acteurs du cycle** : les responsables affectables sont les **acteurs du foyer** déjà
    persistés (référentiel acteurs, ports `IReferentielResponsables`/`IPaletteCouleurs`
    inchangés) — réutiliser, ne pas redéclarer.
  - **Borne** : cycle de fond **EN MÉMOIRE** ici (pas d'adaptateur durable — c'est le palier 9) ;
    **pas** de transfert dérivé du cycle (palier 11) ; **pas** de navigation passé/futur
    enrichie (palier 8). Le reste du domaine reste **InMemory** (règle 30, borne anti-cliquet).
  - **Tranche de secours** (au `/3` UNIQUEMENT si débordement réel ~2h, pas maintenant) :
    livrer d'abord un **cycle à 1 semaine = responsable de fond unique** (option 3) qui pose la
    couche de résolution fond/surcharge, puis **étendre à N semaines** (l'alternance) en
    tranche 2. Ne pas couper par précaution.
- **Sources** : spec v10 **règles 7, 11, 12, 19, 30** + palier 6 (§ Séquence de livraison) +
  Mécaniques (« cycle de plusieurs semaines qui se répète », « le calendrier ne porte que les
  surcharges ponctuelles ») + Risques (« coût de saisie du cycle », R4 acceptation runtime) ;
  BACKLOG palier 6 (É7/É1, « cycle multi-semaines non affiché/éditable » — dette IHM, modèle
  existant) ; corollaire de découpe + garde-fou découpe (~2h IA).

## 2026-06-27 — Ancrage déterministe de la parité « semaine paire / impaire » (palier 6)

- **Question (make-gherkin)** : comment ancrer de façon **déterministe** la parité du cycle
  pour que la résolution du responsable de fond soit **testable sur n'importe quelle date** ?
- **Décision (CP, sans escalade)** : **option 1 — numéro de semaine ISO 8601**. L'index de
  semaine du cycle = **(numéro de semaine ISO de la date) mod N** (N = longueur du cycle ;
  N=2 ⇒ parité paire/impaire). **Aucune ancre à saisir**, phase imposée par le calendrier ISO,
  résolution = **fonction pure de la date**. **Option 2 (date d'ancrage + rang modulo N)**
  écartée : ajoute une donnée à saisir/éditer **et** un cas limite (dates avant l'ancre) —
  contraire au corollaire de découpe et au risque « coût de saisie du cycle ». **Option 3
  (parité année+numéro)** écartée (non demandée).
- **Rationale** : (1) la **règle 11** nomme littéralement « semaine **paire / impaire** » — en
  français la lecture canonique est la **parité du numéro de semaine** (ISO), pas une ancre
  saisie ; option 1 colle au mot de la spec. (2) **Corollaire de découpe** : option 1 est le
  **plus petit incrément** — zéro saisie, zéro champ neuf, **zéro cas limite**, cohérent avec
  le grain « cycle EN MÉMOIRE » déjà tranché. (3) **Risque « coût de saisie du cycle »** (spec,
  Risques) : option 1 le minimise, option 2 l'aggrave. (4) **Testabilité** (le besoin même de
  la question) : fonction pure date→index, déterministe et universelle, **sans fixture d'ancre**.
  (5) La **perte de contrôle** redoutée (le parent ne choisit pas « quelle semaine est A ») est
  **illusoire pour une alternance** : le parent maîtrise déjà **qui** est sur paire / impaire
  via le **mapping index→responsable** (décision de grain précédente) ; **inverser la phase =
  échanger les deux affectations**. Aucune règle de gestion neuve, aucun trou métier → **pas de
  G1** ; cap du palier 6 inchangé → **pas de G2**. Décision de **découpe technique** (remit CP).
- **Guidage de cadrage transmis à make-gherkin** :
  - **Fonction de résolution pure** : `index = ISOWeek(date) mod N` → responsable de fond.
    Scénarios sur **plusieurs dates** (semaine ISO paire ET impaire) prouvant l'alternance ;
    ISO gère proprement le passage d'année (52/53 → 01).
  - **Réutiliser le fournisseur de date déterministe** (`IDateTimeProvider`, déjà injecté au
    s06) pour tester n'importe quelle date sans horloge réelle.
  - **Pas d'écran d'ancrage** : l'édition config porte le **N** et le **mapping
    index→responsable** (déjà acté), **rien de plus**. Si l'usage réclame un jour de choisir
    explicitement la phase, c'est une **évolution séquencée** (option 2), pas ce palier.
- **Sources** : spec v10 **règles 7, 11, 12, 19** + Risques « coût de saisie du cycle » +
  Mécaniques (« cycle de plusieurs semaines qui se répète ») ; décision de grain CP du même jour
  (mapping index→responsable, cycle EN MÉMOIRE) ; port `IDateTimeProvider` (s06, palier 2) ;
  corollaire de découpe + garde-fou ~2h IA. ISO 8601 (numéro de semaine calendaire standard).

## 2026-06-27 — Édition concurrente du cycle de fond : dernière écriture gagne + convergence (palier 6)

- **Question (make-gherkin)** : le cycle de fond est une **unité de cohérence partagée
  unique** ; quand deux parents l'éditent quasi simultanément, quel **comportement
  observable** ?
- **Décision (CP, sans escalade)** : **option 1 — dernière écriture gagne + diffusion**. Les
  deux éditions sont **acceptées dans l'ordre d'arrivée**, le **dernier mapping écrase**, et la
  **diffusion temps réel** pousse l'état final aux deux écrans (**convergence**, sans
  rechargement ni rejet). **Option 2 (rejet de la 2ᵉ écriture périmée / optimistic concurrency)**
  écartée : elle introduirait un **contrôle de version absent de la config foyer**, un surcoût
  non justifié et une incohérence avec le précédent acté.
- **Rationale** : (1) le cycle de fond est une donnée de **config foyer** (règle 7 :
  « responsabilité de fond en config » ; règle 11 : cycle « définissable/éditable depuis la
  config foyer »), édité depuis l'écran config — il vit dans le **même store partagé singleton**
  que les acteurs. Le comportement d'édition concurrente de ce store est **déjà tranché** :
  **s08 Sc.7** (`07-deux-ecrans-derniere-ecriture-gagne.md`), décision CP explicite « store
  partagé serveur, **dernière écriture gagne**, sans version ni rejet », convergence par
  diffusion SignalR. Option 1 = **alignement strict** sur ce précédent. (2) La **règle 26
  (modification directe)** confirme : un parent applique son changement immédiatement, **pas de
  workflow de validation** — le rejet optimiste (option 2) le contredirait. (3) Le **rejet sur
  état périmé existe**, mais **uniquement pour les périodes de garde au calendrier** (s01 Sc.10,
  `10-edition-concurrente.md`, port `IPeriodeRepository`) — un **agrégat différent**
  (surcharges ponctuelles), pas la config foyer. Importer ce contrôle de version dans la config
  foyer **briserait l'homogénéité** et ajouterait un jeton optimiste là où le domaine n'en a pas.
  (4) Aucune **règle de gestion neuve**, aucun **trou métier** (le comportement découle d'un
  précédent acté) → **pas de G1** ; cap du palier 6 inchangé → **pas de G2**. Décision de
  **cohérence de comportement** (remit CP).
- **Guidage de cadrage transmis à make-gherkin (craft, pas un arbitrage PO)** :
  - **Réutiliser le canal de diffusion existant** (SignalR, palier 1) : on **assert** la
    convergence des deux grilles, on ne reconstruit pas le hub. L'écriture passe par le canal
    requête/réponse ; la diffusion est **lecture seule** (jamais d'écriture par la diffusion).
  - **Driver runtime/IHM** (comme s08 Sc.7) : deux écrans éditent le mapping
    index→responsable successivement → les **deux grilles convergent** vers le dernier mapping,
    **sans rechargement ni rejet**. Acceptation sur **app réellement câblée** (anti
    vert-qui-ment, R4) — bUnit seul ne prouve jamais ce câblage.
  - **Filet backend** (caractérisation, ⚠️ early green attendu) : le store **écrase** par
    affectation (pas de version ni garde de conflit) → dernière écriture gagne **par
    construction**. Documenter l'**absence** de rejet/version ; ne pas inventer de faux rouge.
  - **Borne** : pas de jeton optimiste, pas de message « rechargez » sur la config foyer
    (réservé aux périodes calendaires, agrégat distinct, hors palier 6).
- **Sources** : spec v10 **règles 7, 11, 26** (modification directe) + Mécaniques (« deux canaux
  distincts », « la diffusion se déclenche par l'écriture aboutie ») ; **précédent acté s08 Sc.7**
  `docs/sprints/08-config-foyer-acteurs/archive/07-deux-ecrans-derniere-ecriture-gagne.md`
  (store partagé, dernière écriture gagne, convergence SignalR) ; **contre-précédent borné s01
  Sc.10** `docs/sprints/01-semaine-de-garde/archive/10-edition-concurrente.md` (rejet optimiste
  réservé aux **périodes**, agrégat distinct) ; décisions de grain CP du même jour (cycle =
  config foyer, mapping index→responsable, EN MÉMOIRE).

## 2026-06-27 — Validation de l'écriture du fichier de scénarios (palier 6, post-challenge make-gherkin)

- **Question (make-gherkin)** : la passe de challenge est terminée ; valider l'écriture du
  fichier de scénarios du palier 6, ou escalader si un arbitrage métier / cap subsiste.
- **Décision (CP, sans escalade)** : **VALIDÉ — feu vert à l'écriture**. La synthèse (8
  scénarios + découpe composants + risques) est **conforme** aux trois décisions CP déjà
  actées (grain alternance hebdo paire/impaire, ancrage ISO 8601, concurrence dernière
  écriture gagne) et à la spec v10. **Aucune escalade** : pas de **règle de gestion neuve**
  (règle 11 préexiste) → pas de G1 ; **cap du palier 6 inchangé** (sprint goal déjà acté) →
  pas de G2.
- **Couverture des 8 scénarios vérifiée** : (1 driver) alternance par parité ISO sur
  plusieurs dates ⇒ décision *ancrage ISO* ; (2 driver) surcharge > fond puis reprise du
  cycle ⇒ décision *grain* + règle 12 ; (3 caract.) inversion du mapping → grille MAJ sans
  rechargement ⇒ *édition config immédiate* (cohérent s08) ; (4 caract. Outline) index sans
  responsable → neutre ⇒ priorité fond>neutre, règles 18/19 ; (5 caract.) cycle 1 sem sans
  alternance ⇒ pose la **couche de résolution** (tranche de secours option 3 caractérisée,
  pas livrée en bloc) ; (6 driver) deux parents → dernière écriture gagne ⇒ décision
  *concurrence* (s08 Sc.7) ; (7 caract.) cycle zéro semaine refusé ⇒ **invariant N≥1** du
  Domain ; (8 caract.) service injoignable → échec clair ⇒ règle 28 (précédent s05 Sc.6 /
  s08 Sc.9).
- **Découpe composants validée (craft, remit CP)** : Domain = fonction pure `ISOWeek(date)
  mod N` + invariant N≥1 ; Application = port `IReferentielCycleDeFond` + `DefinirCycleHandler`
  + extension `GrilleAgendaQuery`/`ResponsabiliteQuery` (couche de résolution fond) ; Infra =
  adaptateur **InMemory singleton** (PAS Mongo — **borne anti-cliquet règle 30 tenue**),
  endpoint `POST /api/canal/definir-cycle`, écran config, **diffusion SignalR existante
  réutilisée** (jamais reconstruite). Réutilise `IDateTimeProvider` (s06) et le référentiel
  acteurs persisté (ports `IReferentielResponsables`/`IPaletteCouleurs` inchangés).
- **Early-greens attendus tranchés (plus de G4 — routés CP)** : Sc.5 (cycle 1 sem), Sc.6
  (filet backend dernière-écriture-par-construction) et Sc.4 (repli neutre) produiront des
  **verts précoces attendus** ⇒ **conservés comme filets de caractérisation** (documenter
  l'absence de version/rejet, ne pas inventer de faux rouge, ne pas supprimer). Aucun trou
  métier derrière → pas de remontée G1.
- **Risques acceptés** : (a) **borne anti-cliquet** tenue par l'InMemory ; (b) **discontinuité
  de parité ISO à la jonction d'année** (52/53 → 01) — **assumée et documentée**, conséquence
  directe de l'ancrage ISO déjà acté, **pas un trou métier** ; (c) non-régression des
  **périodes explicites** gardée par Sc.2 ; (d) **tranche de secours** = cycle 1 sem
  (responsable unique) si débordement réel au `/3`, **pas par précaution**.
- **Sources** : spec v10 **règles 7, 11, 12, 18, 19, 26, 28, 30** + palier 6 + Mécaniques +
  Risques (« coût de saisie du cycle », R4 acceptation runtime) ; **trois décisions CP du
  2026-06-27** ci-dessus (grain / ancrage ISO / concurrence) ; précédents s08 Sc.7 & Sc.9,
  s01 Sc.10, s05 Sc.6 ; BACKLOG palier 6 (É7/É1) ; corollaire de découpe (~2h IA).
