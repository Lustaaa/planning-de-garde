# Config foyer persistante — ajout d'acteurs & survie au redémarrage

> Sujet : `config-foyer-persistante` · spec `docs/09-specification.md` (v09, palier 5,
> règles 5 / 6 / 30). Produit par `/2-make-gherkin` (mode agent orchestré).
> Découpe **FUSIONNÉE** (tranché CP) : ajout d'acteur (identifiant stable neuf) **et**
> édition du seed durables d'emblée sur store Mongo réel ; le pivot redémarrage couvre
> l'ajout **et** l'édition. Tranche de secours (persister le seed d'abord, reporter
> l'ajout) réservée au `/3` **si** débordement ~2h réel — pas par précaution.

## Analyse technique

Adaptateur de droite **durable** (Mongo) posé **derrière les 3 ports inchangés**
(`IReferentielResponsables` / `IPaletteCouleurs` / `IEditeurConfigurationFoyer`),
doublant/remplaçant `ConfigurationFoyerEnMemoire` câblé **singleton**
(`ServiceCollectionExtensions.cs:28-31`). Le domaine et le CQRS de lecture ne bougent pas.

- **Écriture (CQRS).** (1) **Édition** (existant, désormais durable) :
  `EditerActeurHandler` → `POST /api/canal/editer-acteur` → store durable → diffusion
  SignalR. (2) **Ajout** (neuf) : `AjouterActeurCommand(nom, couleur?)` →
  `AjouterActeurHandler` qui génère un **identifiant stable neuf opaque** (GUID ou
  séquence « autre-N »), **jamais dérivé du libellé**, **unique** (jamais un id
  existant) → persiste nom (+ couleur, sinon repli neutre) via le port d'écriture →
  `POST /api/canal/ajouter-acteur` → diffusion SignalR. La garde « nom non vide » est
  réutilisée (`EditerActeurHandler.cs:38`).
- **Lecture (CQRS).** `GrilleAgendaQuery` **inchangé** : résout `NomDe` / `CouleurDe`
  sur l'identifiant stable ; légende **dédoublonnée par id** (jamais le libellé).
  L'écran de configuration doit **énumérer les acteurs depuis le store durable**
  (aujourd'hui `Foyer.ActeursEditables` est une liste statique front) — sinon un acteur
  ajouté n'apparaît pas dans la liste : un accès de lecture d'énumération est à exposer.
- **Persistance.** Adaptateur Mongo réalisant les 3 ports, singleton. **Seed-au-démarrage
  durable** : seed depuis `Foyer` **seulement si le store est vide** ; ensuite l'état
  persisté est relu **sans re-seeder par-dessus les éditions**. C'est l'**inversion
  exacte de `Scenario10`** (qui documentait le re-seed volatile) : la logique seed-once
  est la principale surface de bug (un re-seed à chaque démarrage ferait échouer le pivot).
- **Bornes.** **Seule** la config foyer (référentiel acteurs : noms, couleurs, acteurs
  ajoutés) passe durable. Slots / périodes / transferts **restent InMemory** (borne
  anti-cliquet, règle 30). Ajout **sans suppression** ; **pas** d'édition du cycle de
  fond ; **cases orphelines hors périmètre**.
- **Outillage.** Aucune infra Mongo/Docker présente à ce jour. Mongo doit **tourner**
  (Docker) pour le test d'intégration du pivot — garde-fou d'outillage sans observable
  métier.

## Scénarios

Couverture règle 6 (ajout + persistance bornée) : nominal (Sc.1, Sc.3, Sc.4), limites
(Sc.5 couleur absente, Sc.6 pas de fantôme, Sc.7 id unique vs libellé), erreurs (Sc.8
nom vide, Sc.9 service injoignable). Règle 5 (édition durable) : nominal (Sc.2) + survie
pivot (Sc.3). Chaque règle porteuse a un nominal, au moins un limite et un erreur.

Feature: Configuration du foyer persistante — un parent ajoute des acteurs au foyer
(parent, autre, nounou) avec un identifiant stable neuf, édite les acteurs déjà semés,
et la grille (case + légende) reflète aussitôt le changement ; l'ajout comme l'édition
**survivent au redémarrage** du serveur, la configuration du foyer étant persistée
derrière les ports de droite.

### Scenario 1 — Ajouter la nounou au foyer génère un identifiant stable neuf

`@nominal` `@vert`

```gherkin
Scenario: Ajouter la nounou au foyer génère un identifiant stable neuf
  Given le foyer compte Alice, Bruno et grand-père dans l'écran de configuration
  When un parent ajoute l'actrice « Carla » avec la couleur rose
  Then Carla apparaît immédiatement dans la liste des acteurs de l'écran de configuration
  And elle est portée par un identifiant distinct de ceux d'Alice, Bruno et grand-père
  And cet identifiant n'est pas dérivé du libellé « Carla »
```

### Scenario 2 — Renommer un acteur déjà semé met à jour la grille

`@nominal` `@vert`

```gherkin
Scenario: Renommer un acteur déjà semé met à jour la grille
  Given Alice garde Léa du 1er au 5 juin dans la fenêtre affichée
  And la légende affiche « Alice » en bleu
  When un parent renomme Alice en « Alicia » depuis l'écran de configuration
  Then les cases du 1er au 5 juin affichent « Alicia » en bleu
  And la légende affiche « Alicia » en bleu
  And la grille suit sans rechargement
```

### Scenario 3 — L'ajout et l'édition survivent au redémarrage du serveur

`@nominal` `@vert`

```gherkin
Scenario: L'ajout et l'édition survivent au redémarrage du serveur
  Given Carla a été ajoutée avec la couleur rose et garde Léa du 8 au 12 juin
  And Alice a été renommée « Alicia » et garde Léa du 1er au 5 juin
  When le serveur est redémarré
  Then l'écran de configuration liste toujours Alicia et Carla sans ressaisie
  And les cases du 1er au 5 juin affichent « Alicia » en bleu, en case comme en légende
  And les cases du 8 au 12 juin affichent « Carla » en rose, en case comme en légende
```

### Scenario 4 — Un acteur ajouté apparaît en légende une fois une période affectée

`@nominal` `@vert`

```gherkin
Scenario: Un acteur ajouté apparaît en légende une fois une période affectée
  Given Carla vient d'être ajoutée avec la couleur rose et n'a encore aucune période
  When un parent affecte à Carla la garde de Léa du 8 au 12 juin
  Then la légende fait apparaître une entrée « Carla » en rose
  And cette entrée est portée par l'identifiant stable neuf de Carla
  And les cases du 8 au 12 juin affichent « Carla » en rose
```

### Scenario 5 — Un acteur ajouté sans couleur retombe sur la teinte neutre

`@limite` `@vert`

```gherkin
Scenario: Un acteur ajouté sans couleur retombe sur la teinte neutre
  Given le foyer affiche déjà ses acteurs colorés
  When un parent ajoute « Papy Jo » sans lui choisir de couleur
  And il affecte à Papy Jo la garde de Léa le 10 juin
  Then la case du 10 juin affiche « Papy Jo » en gris
  And la légende affiche « Papy Jo » en gris
  And le nom « Papy Jo » est conservé
```

### Scenario 6 — Un acteur ajouté sans période ne crée pas d'entrée fantôme

`@limite`

```gherkin
Scenario: Un acteur ajouté sans période ne crée pas d'entrée fantôme
  Given Carla vient d'être ajoutée et n'a aucune période de garde dans la fenêtre affichée
  When la grille du planning est rendue
  Then Carla est présente dans la liste de l'écran de configuration
  And Carla n'apparaît dans aucune entrée de la légende
  And Carla n'apparaît dans aucune case de la grille
```

### Scenario 7 — Deux acteurs de même libellé reçoivent deux identifiants distincts

`@limite`

```gherkin
Scenario: Deux acteurs de même libellé reçoivent deux identifiants distincts
  Given une nounou « Carla » a déjà été ajoutée au foyer
  When un parent ajoute une seconde acteur également nommée « Carla »
  Then le foyer compte deux acteurs « Carla » portés par deux identifiants distincts
  And la légende les dédoublonne par identifiant en deux entrées
  And les deux « Carla » ne sont jamais fusionnées sur leur libellé
```

### Scenario 8 — Ajouter un acteur sans nom est refusé

`@erreur`

```gherkin
Scenario: Ajouter un acteur sans nom est refusé
  Given le foyer affiche ses acteurs dans l'écran de configuration
  When un parent tente d'ajouter un acteur en laissant le nom vide
  Then l'ajout est refusé avec le message « le nom ne peut pas être vide »
  And aucun identifiant n'est généré
  And la liste des acteurs reste inchangée
```

### Scenario 9 — Ajout impossible si le service de configuration est injoignable

`@erreur`

```gherkin
Scenario: Ajout impossible si le service de configuration est injoignable
  Given un parent a saisi l'acteur « Carla » avec la couleur rose dans l'écran de configuration
  When il valide l'ajout alors que le service de configuration est injoignable
  Then un message d'échec clair s'affiche
  And la saisie « Carla / rose » reste à l'écran à resoumettre
  And aucun acteur n'est enregistré
```

## Risques

- **Débordement ~2h (R1).** Ajout + adaptateur Mongo dans la même fenêtre. Tranche de
  secours au `/3` **seulement si** débordement réel : persister le seed d'abord
  (Sc.2/3 sur l'édition), reporter l'ajout (Sc.1/4/6/7). Ne pas couper par précaution.
- **Seed-once vs re-seed.** La logique « seed si vide, sinon relire l'état persisté »
  est le cœur du pivot (Sc.3). Un re-seed au démarrage écraserait les éditions et
  ferait échouer Sc.3 — c'est l'inversion exacte de `Scenario10` à ne pas rater.
- **Énumération des acteurs.** L'écran config doit lister les acteurs **depuis le store
  durable** (pas la liste statique front `Foyer.cs` Web), sinon un acteur ajouté
  n'apparaît pas (Sc.1).
- **Anti vert-qui-ment (R4).** Le pivot (Sc.3) doit tourner sur **Mongo réel** via la
  grille câblée (front WASM + API distante), jamais une doublure ni une grille vide
  statique. Docker/Mongo requis pour le test d'intégration.
- **Borne anti-cliquet (règle 30).** Slots / périodes / transferts **restent InMemory** ;
  ne pas tirer leur persistance en avant au prétexte que la config foyer est durable.
