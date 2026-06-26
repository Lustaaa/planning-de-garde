# Fermeture des fondations — hôte d'API détaché & front côté navigateur

> Palier 1 « Fondations » de la spec v05 (`docs/05-specification.md`), au titre de l'**exception bornée** « la fondation technique d'abord, au début du projet ». Sujet : `host-api-separable`. Issu de `/4-retours` (`docs/sprints/04-controllers-wasm-fondation/99-sprint04-besoins-fin-itération.md`) + `/5-consolidation`. Ce sprint **referme la fondation** : le back démarre seul (hôte d'API détaché), le front migre côté navigateur (WASM complet) et consomme l'**API distante**. Ensuite, l'arbitre d'usage reprend la main → bascule rapide sur le lot « saisie visible ».

**Feature:** En tant que client du hub `/planning` (front exécuté côté navigateur, IHM tierce ou agent), je veux que le **back démarre seul** derrière un **hôte d'API détaché** qui expose son **canal d'écriture** et une **UI d'exploration interactive**, et que le **front WASM** consomme cette **API distante** (URL configurable, CORS) en signalant clairement un échec quand l'API est injoignable, afin que l'app soit un **produit ouvert** dont l'écriture est découplée du front et démarrable sans lui.

## Analyse technique

**Intention.** Détacher l'**hôte d'API** de l'hôte du front. Aujourd'hui un unique `Program.cs` (`PlanningDeGarde.Web`, SDK Web, Blazor Server interactif) porte le front, le canal d'écriture (`MapperCanalEcriture`), l'OpenAPI (`MapOpenApi` en dev) et un `HttpClient` dont `BaseAddress = nav.BaseUri` (son propre hôte) ; il n'existe **aucun projet API séparé** et l'UI d'exploration interactive n'est **pas livrée** (seul le document OpenAPI l'est). On extrait un **hôte d'API démarrable seul** et on migre le **front en WASM** qui parle à cette API par HTTP distant.

**Couches en jeu.**

- **Hôte d'API (nouveau projet, SDK Web)** — référence Application + Infrastructure, porte `MapperCanalEcriture` (canal requête/réponse d'écriture), l'OpenAPI et l'**UI d'exploration interactive** (Scalar ou Swagger-UI). Il **ne référence pas le projet front** : c'est la preuve qu'il démarre seul. CORS y autorise l'origine du front.
- **Front (Web, migré WASM complet)** — Blazor Server → WebAssembly, s'exécute dans le navigateur. Son `HttpClient` cible une **URL d'API configurable** (config, non plus `nav.BaseUri`). Aucune vue n'écrit le domaine en direct : toute écriture part vers l'API distante.
- **Application (write)** — handlers et commandes **inchangés** (`PoserSlotCommand`, `AffecterPeriodeCommand`…) ; l'hôte d'API les invoque via le canal, ne les réécrit pas.
- **Domain + Infrastructure (stores)** — agrégats et repositories **inchangés** ; c'est le **store réel** derrière l'API qui est observé en bout de chaîne, pour fermer le piège du « vert qui ment ».
- **Application (read CQRS)** — projection `GrilleAgendaQuery` **inchangée** ; observable de bout en bout après passage de l'API.
- **Diffusion temps réel** — canal SignalR (`PlanningHub`) conservé en **lecture seule** ; l'écriture aboutie le déclenche, jamais l'inverse. Après migration WASM, il est consommé par le client navigateur ; sa localisation d'hôte est un **point de câblage**, pas une règle métier nouvelle.

**Driver de bout en bout.** Commande émise par le front WASM → HTTP vers l'**hôte d'API détaché** → canal requête/réponse → handler → **store réel** → projection `GrilleAgendaQuery`. Aucune doublure sur le chemin observé.

**Invariants de structure — NON codants** (garde-fous de compilation/config, jamais des scénarios Gherkin pilotants) : convention code-behind systématique ; **API explorable** (document OpenAPI **et** UI interactive Scalar/Swagger-UI) ; séparation des canaux écriture (requête/réponse) vs diffusion (lecture seule) en tant que **câblage**. Aucun n'ouvre de règle de gestion ni d'observable métier — l'UI d'exploration est un confort d'outillage sans observable métier.

**Hors périmètre.** L'**amorce PWA** (mise en cache + file d'écritures rejouée hors-ligne) est **reportée** à un prochain sprint, une fois le front WASM en place ; ce sprint se borne à l'**échec clair** sans file ni rejeu. La commande **définir-transfert** reste sans scénario (la projection ne lit pas les transferts → pas d'observable de bout en bout).

## Scénarios

### Scenario 1 — Le back démarre seul : l'API détachée enregistre une affectation sans le front

`@nominal @vert`

```gherkin
Scenario: Le back démarre seul et son canal d'écriture enregistre une affectation
  Given l'hôte d'API est démarré seul, sans le front, et ne référence pas le projet front
  And le foyer connaît le responsable « Parent A », dont la couleur par défaut est le bleu
  And aucune période n'est affectée sur la semaine du lundi 22 juin 2026
  When une commande d'affectation de la période du lundi 22 au vendredi 26 juin 2026 au responsable « Parent A » est émise sur le canal d'écriture de l'hôte d'API
  Then le canal confirme l'effet par une réponse de succès
  And dans la grille projetée à la semaine du lundi 22 juin 2026, les cases-jour du lundi 22 au vendredi 26 juin 2026 portent la couleur bleue de « Parent A »
```

### Scenario 2 — Le front WASM consomme l'API distante : un slot posé apparaît dans sa case

`@nominal @vert`
<!-- vert — b42cb5b -->

```gherkin
Scenario: Le front côté navigateur pose un slot via l'API distante et le voit dans la grille
  Given l'hôte d'API est démarré seul à l'adresse « https://api.planning.local »
  And le front s'exécute dans le navigateur et est configuré pour émettre ses écritures vers « https://api.planning.local »
  And le foyer connaît le lieu « école »
  And aucun slot n'est enregistré pour le mercredi 24 juin 2026
  When le front émet, vers l'API distante, une pose de slot pour l'enfant « Léa » au lieu « école », le mercredi 24 juin 2026 de 08:30 à 16:30
  Then l'API distante confirme l'effet par une réponse de succès
  And dans la grille projetée à la semaine du lundi 22 juin 2026, la case du mercredi 24 juin 2026 porte un slot « école » positionné de 08:30 à 16:30
```

### Scenario 3 — L'UI d'exploration interactive de l'API liste les endpoints du canal d'écriture

`@nominal @vert`

```gherkin
Scenario: L'UI d'exploration interactive de l'hôte d'API est ouverte et liste le canal d'écriture
  Given l'hôte d'API est démarré seul
  When un utilisateur outillage ouvre la page d'exploration interactive de l'hôte d'API
  Then la page affiche les endpoints du canal d'écriture, dont « poser un slot » et « affecter une période »
  And chaque endpoint listé peut être essayé directement depuis la page
```

### Scenario 4 — L'hôte d'API démarre en mode headless et sert la description et l'exploration

`@limite @vert`

```gherkin
Scenario: L'hôte d'API démarre dans un environnement sans front et sert sa description
  Given un environnement headless où aucun front n'est déployé ni référencé
  When l'hôte d'API est démarré seul dans cet environnement
  Then l'hôte répond et sert le document de description OpenAPI de son canal d'écriture
  And la page d'exploration interactive des endpoints est accessible
```

### Scenario 5 — Le front sur une origine distincte est autorisé par le CORS de l'API distante

`@limite`

```gherkin
Scenario: Une écriture cross-origin du front vers l'API distante est autorisée par le CORS
  Given l'hôte d'API est démarré seul à l'adresse « https://api.planning.local »
  And le front s'exécute dans le navigateur depuis l'origine « https://app.planning.local »
  And cette origine « https://app.planning.local » est autorisée par le CORS de l'API
  And le foyer connaît le lieu « école » et aucun slot n'est enregistré pour le mercredi 24 juin 2026
  When le front, depuis « https://app.planning.local », émet vers « https://api.planning.local » une pose de slot pour l'enfant « Léa » au lieu « école », le mercredi 24 juin 2026 de 08:30 à 16:30
  Then la requête cross-origin est acceptée par l'API distante, qui confirme l'effet par une réponse de succès
  And dans la grille projetée à la semaine du lundi 22 juin 2026, la case du mercredi 24 juin 2026 porte un slot « école » positionné de 08:30 à 16:30
```

### Scenario 6 — API distante injoignable : la saisie est refusée et n'est pas appliquée

`@erreur`

```gherkin
Scenario: Quand l'API distante est injoignable, l'écriture échoue clairement et rien n'est enregistré
  Given le front s'exécute dans le navigateur et est configuré pour émettre ses écritures vers « https://api.planning.local »
  And l'hôte d'API à « https://api.planning.local » est arrêté, donc injoignable
  And le foyer connaît le lieu « école » et aucun slot n'est enregistré pour le mercredi 24 juin 2026
  When le front tente d'émettre une pose de slot pour l'enfant « Léa » au lieu « école », le mercredi 24 juin 2026 de 08:30 à 16:30
  Then le front affiche le message « Enregistrement impossible : le service est injoignable, réessayez. »
  And la saisie n'est pas appliquée et reste à resoumettre
  And aucun slot n'est enregistré pour le mercredi 24 juin 2026, aucune écriture silencieuse ni mise en file n'ayant eu lieu
```

## Risques

- **Bloc de fondation gros** — hôte d'API détaché + migration WASM complète + recâblage du front + CORS dans un même sprint : c'est le palier le plus lourd de la séquence. Borné ici aux endpoints pilotés + invariants non-codants ; surveiller la dérive.
- **Faux sentiment de progrès** — sprint à valeur d'usage immédiate **nulle** (assumé) ; aucun incrément produit n'avance, le grief « les saisies n'apparaissent pas » reste entier. **Basculer vite** ensuite sur le lot « saisie visible » pour ne pas le laisser glisser.
- **Vert qui ment / early-green** — prouver que c'est l'**API détachée** (hôte ne référençant pas le projet front) qui répond, et que l'écriture WASM **aboutit réellement** via HTTP distant jusqu'au store réel lu par `GrilleAgendaQuery` ; jamais un accusé du canal ni une doublure. Ne **pas** promettre de file/rejeu non implémenté.
- **Diffusion déclenchée par l'écriture** — garantir qu'une écriture aboutie déclenche le rafraîchissement temps réel des autres écrans **sans jamais écrire par le canal de diffusion**, après migration WASM (SignalR consommé par le client navigateur) ; point de câblage à valider en repro runtime.
- **Contraintes du découplage distant** — sérialisation des commandes, CORS, future authentification, latence/échecs réseau introduits par l'API **distante** et absents quand le front parlait au back en direct ; à surveiller, hors observable Gherkin.
- **PWA reporté** — la mise en cache + file d'écritures rejouée hors-ligne est différée à un prochain sprint (une fois le front WASM en place). Ce sprint se borne à l'échec clair (message + saisie non appliquée), sans file ni rejeu.
- **Piste pour le palier PWA futur (avis, non implémenté ici)** — **event sourcing + outbox** est pertinent comme socle d'une file d'écritures rejouable : l'outbox garantit qu'une commande acceptée hors-ligne sera rejouée puis diffusée **exactement une fois** (couplage écriture → diffusion fiable, cohérent avec « l'écriture aboutie déclenche la diffusion »). L'event sourcing aide à reconstruire/rejouer l'état et à résoudre les conflits de rejeu, mais c'est un **changement de modèle de persistance lourd** : à n'adopter que si le besoin offline/rejeu/audit le justifie ; sinon un simple **outbox + file côté client (IndexedDB)** suffit pour l'amorce PWA. À trancher au palier PWA, pas maintenant.
