# Fondations — canal d'écriture & migration front côté navigateur

> Palier 1 « Fondations » de la spec v04 (`docs/04-specification.md`), au titre de l'**exception bornée** « la fondation technique d'abord, au début du projet ». Sujet : `controllers-wasm-fondation`. Issu de `/4-retours` + `/5-consolidation`.

**Feature:** En tant que client du hub `/planning` (front exécuté côté navigateur, IHM tierce ou agent), je veux émettre les commandes d'écriture du planning via le **canal requête/réponse** côté serveur et voir leur effet se refléter dans la grille agenda, afin que l'app soit un **produit ouvert** dont l'écriture est découplée du front, **sans jamais écrire par le canal de diffusion** temps réel.

## Analyse technique

**Intention.** Extraire l'**adaptateur de gauche** : les commandes d'écriture existantes (poser un slot, affecter une période, définir un transfert) sont désormais confiées à un **canal requête/réponse** côté serveur ; le front est **migré vers une exécution côté navigateur** qui consomme ce canal au lieu d'appeler le back en direct. La diffusion temps réel est **conservée, cantonnée à la lecture seule** : une écriture aboutie la déclenche, mais on n'écrit jamais par elle.

**Couches en jeu.**

- **Adaptateur de gauche (Web)** — un canal requête/réponse expose les commandes d'écriture ; il remplace l'appel direct des handlers depuis les vues. C'est lui que pilotent les scénarios (commande émise → réponse).
- **Application (write)** — handlers et commandes **inchangés** (`PoserSlotCommand`, `AffecterPeriodeCommand`, `DefinirTransfertCommand`). Le canal les invoque, ne les réécrit pas.
- **Domain + Infrastructure (stores)** — agrégats et repositories **inchangés** ; c'est le **store réel** qui est observé en bout de chaîne (pas une doublure), pour fermer le piège du « vert qui ment ».
- **Application (read CQRS)** — `GrilleAgendaQuery.Projeter(dateReference)` **inchangée** : c'est l'observable de bout en bout. Le `Then` lit la **projection réelle** après passage du canal.
- **Diffusion temps réel** — conservée en **lecture seule** ; l'écriture aboutie la déclenche, jamais l'inverse.

**Driver de bout en bout.** Commande émise via le canal requête/réponse → handler → **store réel** → projection `GrilleAgendaQuery`. Aucune doublure sur le chemin observé : c'est ce qui évite l'early-green / la caractérisation pure.

**Invariants de structure — NON codants** (garde-fous de compilation/config, jamais des scénarios Gherkin pilotants ; cf. Sc.6/Sc.8 retirés au sprint 03) : exécution/hosting côté navigateur (WASM) ; convention code-behind systématique ; API explorable/documentée (swagger) ; la séparation des canaux écriture (requête/réponse) vs diffusion (lecture seule) en tant que **câblage**. Aucun n'ouvre de règle de gestion ni d'observable métier.

**Hors périmètre codant.** La commande **définir-transfert** est exposée par le canal d'écriture **mais sans scénario** : `GrilleAgendaQuery` ne lit que slots et périodes, le transfert n'y apparaît pas → pas d'observable de bout en bout, le coder serait de la caractérisation pure.

## Scénarios

### Scenario 1 — Poser un slot via le canal d'écriture le rend visible dans sa case jour/horaire

`@nominal` `@vert`

```gherkin
Scenario: Poser un slot via le canal d'écriture le rend visible dans sa case jour/horaire
  Given le foyer connaît le lieu « école »
  And la grille est projetée à la semaine de référence du lundi 22 juin 2026
  And aucun slot n'est encore enregistré pour le mercredi 24 juin 2026
  When une commande de pose de slot pour l'enfant « Léa » au lieu « école », le mercredi 24 juin 2026 de 08:30 à 16:30, est émise via le canal requête/réponse
  Then le canal confirme l'effet par une réponse de succès
  And dans la grille projetée à la semaine de référence, la case du mercredi 24 juin 2026 porte un slot « école » positionné de 08:30 à 16:30
```

### Scenario 2 — Poser un slot sur un lieu absent du foyer est refusé et ne touche pas la grille

`@erreur`

```gherkin
Scenario: Poser un slot sur un lieu absent du foyer est refusé et ne touche pas la grille
  Given le foyer connaît les lieux « école » et « domicile A » mais pas le lieu « piscine »
  And la grille est projetée à la semaine de référence du lundi 22 juin 2026
  And aucun slot n'est enregistré pour le mercredi 24 juin 2026
  When une commande de pose de slot pour l'enfant « Léa » au lieu « piscine », le mercredi 24 juin 2026 de 08:30 à 16:30, est émise via le canal requête/réponse
  Then le canal renvoie une réponse d'échec « le lieu visé n'existe pas dans les lieux du foyer »
  And dans la grille projetée à la semaine de référence, aucune case ne porte de slot « piscine » et la case du mercredi 24 juin 2026 reste sans slot
```

### Scenario 3 — Affecter une période via le canal colore les cases-jour couvertes à la couleur du responsable

`@nominal`

```gherkin
Scenario: Affecter une période via le canal colore les cases-jour couvertes à la couleur du responsable
  Given le foyer connaît le responsable « Parent A », dont la couleur par défaut est le bleu
  And la grille est projetée à la semaine de référence du lundi 22 juin 2026
  And aucune période n'est affectée sur cette semaine, les cases-jour portant la couleur neutre
  When une commande d'affectation de la période du lundi 22 au vendredi 26 juin 2026 au responsable « Parent A » est émise via le canal requête/réponse
  Then le canal confirme l'effet par une réponse de succès
  And dans la grille projetée à la semaine de référence, les cases-jour du lundi 22 au vendredi 26 juin 2026 portent la couleur bleue de « Parent A »
```

### Scenario 4 — Affecter une période sans responsable est refusée et laisse les cases en couleur neutre

`@erreur`

```gherkin
Scenario: Affecter une période sans responsable est refusée et laisse les cases en couleur neutre
  Given la grille est projetée à la semaine de référence du lundi 22 juin 2026
  And aucune période n'est affectée sur cette semaine, les cases-jour portant la couleur neutre
  When une commande d'affectation de la période du lundi 22 au vendredi 26 juin 2026 sans responsable est émise via le canal requête/réponse
  Then le canal renvoie une réponse d'échec pour responsable manquant
  And dans la grille projetée à la semaine de référence, les cases-jour du lundi 22 au vendredi 26 juin 2026 restent à la couleur neutre, aucune couleur de responsable n'étant appliquée
```

## Risques

- **Vert qui ment / early-green** — le `Then` doit lire l'état **réel** de `GrilleAgendaQuery` après le store réel, jamais un accusé du canal ni une doublure ; sinon le câblage réel peut échouer sous une grille verte. Driver de bout en bout obligatoire.
- **Sprint à valeur d'usage immédiate nulle (assumé)** — aucun incrément produit n'avance ; le grief « les saisies n'apparaissent pas » reste entier jusqu'au Groupe 2. Tenir la séquence pour ne pas le laisser glisser.
- **Réécriture du flux d'écriture** — faire passer les écritures par le canal touche tout le câblage de saisie ; régression à vérifier en **usage réel**, pas seulement en test de composant.
- **Bloc de fondation indivisible** — périmètre potentiellement plus gros que les incréments restants ; borné ici aux 2 commandes pilotées + invariants non-codants, pour éviter la dérive.
- **Diffusion déclenchée par l'écriture** — garantir que l'écriture aboutie déclenche le push de rafraîchissement lecture **sans jamais écrire par le canal de diffusion** ; point de câblage à valider en repro runtime (invariant, non codé en Gherkin).
- **Contraintes du découplage** — sérialisation des commandes, future authentification, échanges inter-domaines, introduits par le passage du front au canal et absents en appel direct ; à surveiller, hors observable Gherkin.
