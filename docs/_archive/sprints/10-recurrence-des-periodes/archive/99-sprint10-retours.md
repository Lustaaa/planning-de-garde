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

- Pour la visu du planning chaque utilisateur a sa propre vu.
  - Il faut changer la dropdown Role pour mettre l'acteur qu'on souhaite impersonnate.

## IHM - /configuration

- la dropdown "Acteur du foyer" n'est pas mise à jour quand je change le nom d'un acteur
- il manque de quoi choisir le debut du cycle
- il manque de quoi configurer le cycle. Je veux pouvoir configurer finement les cycles
  - La mise a jour doit avoir une date de debut et un date de fin de cycle
  - quelque exemples :
    - 1 semaine sur 2 du vendredi au vendredi (par exemple)
    - 1 semaine sur 2 du lundi au lundi et 2 semaines/2 semaine pendant les grandes vacances
    - 1 WE sur 2 (2 jours toutes les 2 semaines)

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

- la configuration du foyer correspond a la configuration pour un utilisateur. 
  - Il faut un crud complet sur les acteurs (Create - Read - Update - Delete)
  - Tant que tous les acteur ne sont pas des utilisateurs, l'utilisateur principale peut agire sur les acteurs
- Supprimer l'ecran Poser un slot => en faire une dialog qui s'ouvre depuis le bouton de l'ecran planning
- Supprimer l'ecran Affecter une période => en faire une dialog qui s'ouvre depuis le bouton de l'ecran planning
- Supprimer l'ecran Définir un transfert => en faire une dialog qui s'ouvre depuis le bouton de l'ecran planning

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-27 | Tous les agents (prompts) + `/6-cloture-sprint` | PO : les agents ne doivent **pas** lire les éléments archivés (répertoire `archive/`). En fin de sprint, archiver les autres fichiers du sprint passé et ne conserver hors archive que le **fichier de suivi** (`00-sprint<NN>-suivi.md`), que les agents peuvent lire s'ils le souhaitent. | À traiter par `retro-sprint` : (1) ajouter une consigne « ne pas lire `archive/` » dans les agents qui explorent `docs/sprints/` ; (2) `/6-cloture-sprint` déplace tous les fichiers de sprint sauf le suivi dans `archive/`. |

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-27 | Tests runtime SignalR (Web.Tests) | Un test runtime SignalR a échoué 1 fois sur 2 exécutions complètes au gate (flake d'isolation/timing sous charge parallèle) ; vert au re-run et en isolation. | Stabiliser au prochain passage technique (synchronisation déterministe de la pompe de diffusion / attente d'établissement du long polling au lieu d'un timing). Candidat retro-sprint. |

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

## 2026-06-27 — Validation du plan d'implémentation tdd-analyse (palier 6, `/3`)

- **Question (tdd-analyse)** : valider le plan d'implémentation du sprint 10 (8 scénarios,
  11 tests backend) et trancher la divergence vs analyse `/2` avant d'enchaîner l'implémentation.
- **Décision (CP, sans escalade)** : **VALIDÉ — feu vert à l'implémentation**. Le plan
  (Domain parité ISO + invariant N≥1 ; port `IReferentielCycleDeFond` + `DefinirCycleHandler` ;
  extension `GrilleAgendaQuery` branche `periode is null` ; adaptateur **InMemory singleton, PAS
  Mongo**) est conforme aux trois décisions CP du grain/ancrage/concurrence et à la borne
  anti-cliquet (règle 30). Aucune escalade : pas de règle de gestion neuve (règle 11 préexiste)
  → pas de G1 ; cap palier 6 inchangé → pas de G2.
- **Divergence Sc.2 driver→caractérisation : TRANCHÉE EN FAVEUR de tdd-analyse.** Inspection de
  `src/PlanningDeGarde.Application/Classes/GrilleAgendaQuery.cs` (l.63-66) : `CaseJourAu` résout
  via `periode is null ? neutre : période`. Le fond s'ajoute **dans la branche `periode is null`**
  → la branche `else période` (surcharge) reste **structurellement intacte**. La priorité
  surcharge > fond ne requiert **aucun code neuf** : Sc.2 ne peut pas piloter un rouge → c'est
  une **caractérisation (filet de non-régression des périodes explicites)**, pas un driver.
  La synthèse `/2` (« Sc.2 driver ») est **corrigée**. Drivers réels = **4** (Sc.1 ×3 + Sc.7 ×1).
- **Early-greens routés CP (plus de G4) — conservés en filets.** Sc.2, Sc.3, Sc.4, Sc.5, Sc.6,
  Sc.7 #2 et Sc.1 #4 produiront des verts précoces **attendus** (composent du code déjà vert :
  priorité structurelle, repli neutre mirroir `CouleurDe`, `mod 1 = 0`, écrasement singleton).
  **Aucun doublon à supprimer, aucun câblage à investiguer** : ce sont des caractérisations
  légitimes documentant l'absence de version/rejet. Consigne : **ne pas inventer de faux rouge**.
  Aucun trou métier derrière → pas de remontée G1.
- **Bornes rappelées** : cycle de fond **EN MÉMOIRE** (InMemory singleton, durabilité = palier 9) ;
  référentiel acteurs + ports `IReferentielResponsables`/`IPaletteCouleurs` **inchangés** (résolution
  nom/couleur sur id stable, règle 19) ; diffusion SignalR **réutilisée** (Spy backend, runtime
  ihm-builder), jamais reconstruite ; Sc.8 = 0 backend (échec transport runtime, patron s09 Sc.9).
  Tranche de secours (cycle 1 sem) **seulement** si débordement réel ~2h, pas par précaution.
- **Sources** : `00-sprint10-suivi.md` (cadrage scaffolding) ; `src/.../GrilleAgendaQuery.cs`
  l.61-80 (branche `periode is null`, `LegendeDesPresents` agrège les seules périodes) ; trois
  décisions CP du 2026-06-27 ci-dessus ; spec v10 règles 7, 11, 12, 18, 19, 26, 28, 30 ;
  précédents s08 Sc.7 & Sc.9, s01 Sc.10, s05 Sc.6 ; corollaire de découpe (~2h IA).

## 2026-06-27 — Validation de l'écriture du backlog de besoins fin d'itération (palier 6, `/4-retours`)

- **Question (`/4-retours`)** : la synthèse de retours-challenge est-elle dérivable des retours
  PO + backlog, et peut-on autoriser l'écriture de `99-sprint10-besoins-fin-itération.md` ?
  (Le choix du prochain sujet G2 est **déjà tranché par le PO** : GOAL 1 — dialogs depuis le
  planning, palier 8. Pas de réouverture G2.)
- **Décision (CP, sans escalade)** : **VALIDÉ — feu vert à l'écriture du fichier de besoins**.
  La synthèse est intégralement dérivable des retours produit PO et du BACKLOG ; aucun trou
  métier ne subsiste. **Pas de G2** (cap déjà acté par le PO) ; **pas de G1** (aucune règle de
  gestion neuve — les besoins reprennent des épics/règles existantes).
- **Dérivabilité vérifiée (retour PO → besoin) :**
  - **R2 bug dropdown « Acteur du foyer » périmée** (retours `## IHM - /configuration`, l.27 :
    « pas mise à jour quand je change le nom d'un acteur ») → **fix `/3` ciblé léger en tête**,
    **hors make-gherkin** (régression d'affichage du store `_acteurs`, pas un incrément métier).
  - **GOAL 1 — dialogs depuis le planning** (Poser slot / Affecter période ; transfert en
    secours) ⇐ **Consigne pour la suite** PO (l.61-63 : « Supprimer l'écran … ⇒ dialog depuis le
    bouton planning ») = **BACKLOG palier 8** (É12/É6/É7/É8, « écriture en contexte ») +
    dette s03 (édition/suppression période depuis IHM). Réutilise commandes/handlers + canal HTTP
    `poser-slot`/`affecter-période` **déjà livrés** (s04/s05). **Arbitre : l'usage tranche +
    défaut confirmé prime.**
  - **(b) CRUD acteurs complet** (Delete manquant + impersonation **bornée convenance admin**,
    PAS auth) ⇐ Consigne PO (l.58-60 : « CRUD complet … l'utilisateur principal peut agir sur
    les acteurs tant qu'ils ne sont pas des utilisateurs ») = É2 (« Édition acteurs autres :
    ajout/édition/**suppression** ») + amorce É10. **Séquencé derrière GOAL 1, NON abandonné.**
  - **(c) cycle de fond riche** (R3 « choisir le début du cycle » l.28 ; R4 « configurer
    finement … date début/fin, 1 sem/2, WE/2 » l.29-34) ⇐ É7/É1 + palier 9 (config durable).
    **Séquencé derrière, NON abandonné.** ⚠️ **Rouvre explicitement la borne CP « ancrage ISO
    8601, aucune ancre à saisir »** (décision du 2026-06-27 ci-dessus) : c'était l'**évolution
    option 2 anticipée** (« si l'usage réclame de choisir la phase, évolution séquencée ») — la
    réouverture est **conforme**, à traiter quand (c) sera pris (ancre/borne saisie + durabilité
    palier 9). Pas un trou métier maintenant.
  - **Impersonation via dropdown rôle** (retours `## IHM - général`, l.22-23) → versé à (b)
    (convenance admin) / amorce É10, pas le prochain sujet.
  - **Tech (optionnel)** vide (l.40-42) → **bypass confirmé** (placeholder seul, aucune
    contrainte technique).
  - **Idée pour la suite** vide (l.50) → rien à verser au backlog.
- **Risques / bornes** : (a) GOAL 1 réutilise l'existant (pas de commande/handler neuf) — risque
  de débordement faible, tranche verticale ~2h IA ; (b) la borne anti-cliquet règle 30 reste
  tenue (le cycle riche (c) embarque la durabilité **palier 9**, non remontée devant l'usage) ;
  (c) aucun abandon — (b) et (c) restent au backlog, séquencés derrière l'usage.
- **Sources** : `99-sprint10-retours.md` (Retours produit PO : `## IHM - général`,
  `## IHM - /configuration`, `## Tech`, `# Idée pour la suite`, `# Consigne pour la suite`) ;
  `docs/BACKLOG.md` (palier 8 É12/É6/É7/É8 ; É2 CRUD acteurs ; É7/É1 cycle ; palier 9 durabilité ;
  dettes s03) ; spec v10 palier 6/8/9 + règle 30 (borne anti-cliquet) ; décision CP « ancrage ISO »
  du 2026-06-27 ci-dessus (rouverte par (c)) ; **G2 tranché PO** (GOAL 1 dialogs planning).

## 2026-06-27 — Collision `/5-consolidation` : « cycle de fond riche » vs décision CP ancrage ISO (spec v11)

- **Question (spec-consolidation)** : le besoin forward « cycle de fond riche » (R3 ancre/début +
  R4 frontière de jour, plage début/fin, sur-cycle vacances, WE-only) rouvre la décision CP
  « ancrage ISO 8601, aucune ancre à saisir » et chevauche la durabilité (palier 9). Comment v11
  l'intègre ?
- **Décision (CP, sans escalade)** : **option 1 — palier forward séquencé, ancre rouverte au
  cadrage**. v11 consigne « cycle de fond riche » comme **palier forward rang 3** (derrière les
  dialogs GOAL 1 et le CRUD/impersonation), qui **rouvre explicitement la décision ancrage au
  moment de SON make-gherkin**. **Règle 11 NON révisée maintenant** (reste « ISO sans ancre » =
  état courant livré), seulement **enrichie d'une note** « évolution séquencée si l'usage la
  réclame (ancre/borne de saisie + frontière jour + plage validité + sur-cycle/WE-only), à
  trancher au make-gherkin du palier dédié ».
- **Rationale (résolution déterministe, pas de conflit de valeur)** :
  (1) **Convention « Révisions de règle hors boucle »** : une règle se révise **dans le
  make-gherkin de son palier**, pas rétroactivement en consolidation. Option 1 la respecte ;
  option 2 (renverser la décision CP dès v11) la viole frontalement.
  (2) La **décision CP ancrage ISO du 2026-06-27 a pré-autorisé** cette évolution (« option 2
  date d'ancrage écartée AVEC note : évolution séquencée si l'usage la réclame ») — la réouverture
  est **conforme et anticipée**, pas une régression de borne.
  (3) L'**arbitrage `/4-retours` (l.328-334)** a déjà séquencé (c) « cycle de fond riche » comme
  forward **NON abandonné**, embarquant la durabilité palier 9 — v11 ne fait que **transcrire** ce
  séquencement, sans le re-trancher.
  (4) **Option 3 (éclater R3/R4 en 5 paliers a/b/c/d/e dès v11)** écartée : **découpe prématurée**
  avant cadrage make-gherkin, sur-engage la granularité et le grain ~2h IA — la découpe fine est
  un travail de make-gherkin, pas de consolidation (corollaire de découpe). v11 garde le besoin
  **groupé** comme un seul palier forward, à découper au cadrage.
- **Pas d'escalade** : aucune **règle de gestion neuve** actée (règle 11 inchangée) → pas de G1 ;
  aucun **cap de sprint** touché (GOAL 1 dialogs déjà tranché PO) → pas de G2. Pas de trou métier :
  l'état livré (ISO sans ancre) reste cohérent et utilisable tel quel.
- **Sources** : `99-sprint10-besoins-fin-itération.md` (R3 l.36, R4 l.37) ; décision CP « ancrage
  ISO 8601 » du 2026-06-27 (ci-dessus, note « évolution séquencée si l'usage la réclame ») ;
  arbitrage `/4-retours` l.328-334 (séquencement forward (c), réouverture conforme) ; convention
  pipeline « Révisions de règle hors boucle » (CLAUDE.md / spec) ; corollaire de découpe (~2h IA) ;
  spec v10 règle 11 + Risques « coût de saisie du cycle » ; palier 9 durabilité.

## 2026-06-27 — Validation de l'écriture de la spec vivante v11 (`/5-consolidation`)

- **Question (spec-consolidation)** : la consolidation v11 sur v10 est-elle cohérente, et peut-on
  autoriser l'écriture de `docs/11-specification.md` (remplace v10 figée) ?
- **Décision (CP, sans escalade)** : **VALIDÉ — feu vert à l'écriture de v11**. La synthèse est
  intégralement dérivable de l'état livré (palier 6, 8 scénarios @vert) + des besoins fin
  d'itération + des 6 décisions CP du 2026-06-27 ci-dessus. **Aucun conflit de valeur ne subsiste**
  → pas de G1 ; **cap déjà tranché PO** (GOAL 1 dialogs planning) → pas de G2.
- **Cohérence vérifiée (synthèse → source) :**
  - **(1) Palier 6 fondu dans l'état courant** (Contexte/Objectif, Séquence §6, Mécaniques) ⇐
    suivi `00-sprint10-suivi.md` (cycle de fond résolu sous les périodes, EN MÉMOIRE, 8/8 @vert).
    **Règle 12** (surcharge > fond > neutre, **priorité structurelle** branche `periode is null`,
    légende agrège le fond) ⇐ décisions CP grain + validations `/2`/`/3` (Sc.2 caractérisation).
  - **(2) Règle 11 révisée pour DÉCRIRE LE LIVRÉ** (N semaines, parité **ISO sans ancre**, mapping
    sur **id stable** règle 19, refus **N=0**, édition depuis section « Cycle de fond ») —
    **cohérent**, et distinct de la non-révision *forward* : la note « ancre/phase = évolution
    séquencée si l'usage la réclame » consigne le rang 3 **sans** tirer la fonctionnalité d'ancre
    en avant. Conforme à la décision *collision* ci-dessus (option 1, règle 11 « ISO sans ancre »
    + note). **Règle 30 + borne anti-cliquet + §9** : cycle EN MÉMOIRE, durabilité au palier 9.
  - **(3) Prochain sujet basculé → dialogs depuis le planning (palier 8/É12)** ⇐ G2 tranché PO.
  - **(4) Forward séquencés sans abandon** : rang 2 CRUD acteurs complet (Delete + impersonation
    **bornée convenance admin**, **règle 6**) ; rang 3 cycle de fond riche **groupé** (rouvre
    ancrage ISO à SON make-gherkin) ⇐ besoins fin d'itération + décision *collision*.
  - **(5) R2 = `/3` hors-spec, aucune règle** ; **palier 7 (survol enrichi) séquencé** (skippé
    faute de demande PO) ⇐ besoins fin d'itération.
- **Points de vigilance non bloquants** : aucune **règle de gestion neuve** (révisions sur 11/12/30/6
  + note, pas de règle ajoutée) ; **pas de changelog** assumé (cohérent avec les consolidations
  antérieures qui figent l'historique par numéro de version) ; v11 **remplace** v10, figée.
- **Sources** : `00-sprint10-suivi.md` (livré palier 6) ; `99-sprint10-besoins-fin-itération.md`
  (séquence forward, R2/R3/R4/C1/C2-C4) ; **6 décisions CP du 2026-06-27** ci-dessus (grain /
  ancrage ISO / concurrence / écriture scénarios / plan tdd / besoins + collision) ; spec v10
  règles 6, 7, 11, 12, 18, 19, 30 + palier 6/8/9 + Mécaniques + Risques ; convention « Révisions
  de règle hors boucle ».

## 2026-06-27 — Priorisation des actions de rétro sprint 10 (`/6-cloture-sprint`, étape 1)

- **Question (retro-sprint, via CP)** : sur les 5 actions de rétro méthode proposées,
  lesquelles auto-appliquer (tweaks faible risque) et lesquelles escalader au PO (G1,
  structurel/risqué) ? Palier d'autonomie 0 (conservateur).
- **Décision (CP)** : **4 actions auto-appliquées** (faible risque) → transmises à
  `retro-sprint` pour application + écriture `98-retrospective.md` ; **1 action escaladée G1**
  (structurelle).
  - **Action 1 — AUTO-APPLIQUÉE.** Consigne « ne lis JAMAIS sous `archive/` ; hors archive,
    seul `00-sprint<NN>-suivi.md` d'un sprint passé est lisible » ajoutée aux 7 agents qui
    explorent `docs/sprints/` (tdd-analyse, tdd-auto, ihm-builder, retours-challenge,
    spec-consolidation, retro-sprint, chef-de-projet) ; reformuler la mention « scénarios sous
    `archive/` » de retro-sprint comme source **non lue directement**. **Faible risque** :
    édition de prompt **directement mandatée par le PO** (table Méthode, l.72, point 1) ; ne
    touche aucune logique de script.
  - **Action 3 — AUTO-APPLIQUÉE.** Propager le patron UTF-8 +
    `Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()` à `find-retro.ps1`
    (l.30 actuelle : `Set-Location (git rev-parse --show-toplevel)` sans `-LiteralPath` ni
    forçage d'encodage → casse sur le chemin accentué `…/source/privée/…`) ; auditer
    `find-retours`/`find-spec`/`archive-iteration.ps1`. **Faible risque** : **bug confirmé**,
    patron déjà éprouvé sur les 6 scripts git au sprint 05, non propagé ici par oubli.
  - **Action 4 — AUTO-APPLIQUÉE.** Consigne ihm-builder : tests runtime SignalR attendent
    l'**établissement déterministe** du long polling (pas un timing fixe) + **isolation d'état**
    entre tests. **Faible risque** : ajout de consigne cohérent avec l'anti-flake Docker déjà
    présent (ihm-builder l.86-95) ; observation IA, flake 1×/2 au gate.
  - **Action 5 — AUTO-APPLIQUÉE.** Note dans `git/SKILL.md` : « syntaxe selon le shell du tool »
    (Bash POSIX vs PowerShell here-string) pour éviter qu'un here-string fuite dans un commit.
    **Faible risque** : note de doc, aucune logique modifiée.
  - **Action 2 — ESCALADÉE G1 (structurelle, NON auto-appliquée).** Étape d'archivage finale
    déplaçant en `archive/` tous les `.md` de pilotage du sprint clos SAUF
    `00-sprint<NN>-suivi.md` (donc `99-retours`, `99-besoins`, `98-retrospective`) + réécriture
    des liens + extension `archive-iteration.ps1` (mode clôture). **Escaladée car structurelle
    ET porteuse d'un risque de régression non trivial** : déplacer `99-besoins` et
    `98-retrospective` **casse les scripts de détection qui scrutent la racine du dossier de
    sprint sans `-Recurse`** — `find-retro.ps1` (l.48 glob `99-sprint*-besoins-fin-itération.md`,
    l.57 `98-retrospective.md`) et `cloture-sprint.ps1` (l.42-44 détecte le sprint par
    `*-retours.md`). Sans refonte coordonnée de ces gardes, le gate de rétro non-contournable et
    la détection du sprint clos tombent. Hors palier 0 conservateur → revient au PO.
- **Rationale** : actions 1/3/4/5 = **tweaks de méthode à faible risque** (prompt/doc + bug-fix
  d'un patron éprouvé), dont 1 et l'intention de 2 sont **mandatés PO** (table Méthode l.72) ;
  je tranche l'**implémentation** des seules 1/3/4/5. L'action 2, bien que son **intention** soit
  mandatée par le PO, exige une **refonte coordonnée de scripts de détection** (logique +
  déplacement irréversible de fichiers + réécriture de liens) : c'est un **changement structurel
  du pipeline** au sens de `/6-cloture-sprint` (étape 1) → escalade G1, pas d'auto-application.
- **Sources** : `99-sprint10-retours.md` (table **Méthode** l.72, section **IA** l.80) ;
  `.claude/skills/retro-sprint/scripts/find-retro.ps1` (l.30, l.48, l.57 — détection racine non
  récursive) ; `.claude/skills/cloture-sprint/scripts/cloture-sprint.ps1` (l.42-44) ;
  `.claude/skills/git/SKILL.md` (patron UTF-8/`-LiteralPath` l.85-88, propagé au s05) ;
  `.claude/agents/ihm-builder.md` (anti-flake Docker l.86-95) ; `/6-cloture-sprint` étape 1
  (CP sélectionne faible risque, escalade G1 le structurel/risqué).
