# Récurrence des périodes — définir le cycle de fond — Analyse & scénarios

> Sujet : `recurrence-des-periodes` · spec `docs/10-specification.md` (v10, palier 6,
> règles 7 / 11 / 12). Produit par `/2-make-gherkin` (mode agent orchestré).
> Cadrage tranché (CP, sans escalade PO) : grain = **alternance hebdomadaire paire/impaire**
> (1 responsable de fond par semaine) ; ancrage = **numéro de semaine ISO 8601**
> (`index = ISOWeek(date) mod N`, fonction pure de la date) ; concurrence = **dernière
> écriture gagne + diffusion**. Cycle **EN MÉMOIRE** ici (durabilité = palier 9, borne
> anti-cliquet règle 30 — Mongo non tiré en avant). Tranche de secours : cycle 1 semaine
> (responsable de fond unique, sans alternance) **uniquement** si débordement ~2h réel au `/3`.

## Analyse technique

- **Composants impactés** — *Domain* : fonction pure de parité (`index = ISOWeek.GetWeekOfYear(date) mod N`,
  via `System.Globalization`, sans framework) + invariant « cycle ≥ 1 semaine ». *Application* :
  nouveau modèle `CycleDeFond` derrière un port (`IReferentielCycleDeFond`), `DefinirCycleHandler`
  (écriture), et **extension** de `GrilleAgendaQuery` + `ResponsabiliteQuery` pour résoudre le fond
  quand aucune période explicite ne couvre le jour (`IDateTimeProvider` déjà injecté sprint 06).
  *Infrastructure/Api/Web* : adaptateur **InMemory** singleton du port cycle (**pas Mongo**),
  endpoint `POST /api/canal/definir-cycle`, écran de configuration du foyer portant l'édition du
  mapping index→responsable (sélecteur alimenté par les acteurs du foyer, règle 7), diffusion
  SignalR existante (lecture seule) pour la convergence.
- **Couches & dépendances** — la parité et l'invariant `N ≥ 1` vivent dans le **domaine**
  (testables sans framework) ; le cycle est une **donnée de config derrière un port**
  (Application/Infra remplaçable) ; la résolution du fond vit dans la **projection de lecture**
  (`GrilleAgendaQuery`), jamais dans une vue. Dépendances vers l'intérieur, domaine sans `using`
  de framework.
- **Contrats de données** — `CycleDeFond { int NombreSemaines (N ≥ 1) ; mapping index 0..N-1 →
  responsableId (identifiant stable, jamais le libellé ; index non mappé = pas de fond) }` ;
  `DefinirCycleCommand(nombreSemaines, affectations: index→responsableId)` → `Result` (échec si N=0).
- **Write vs read (CQRS)** — *écriture* : `DefinirCycleHandler` protège l'invariant `N ≥ 1` et
  n'accepte que des responsables du foyer. *Lecture* : pour chaque jour, résolution **surcharge
  ponctuelle (période explicite) > responsable de fond `mapping[ISOWeek(jour) mod N]` > neutre**.
- **Invariants** — (1) un cycle compte au moins une semaine (gardé par le handler d'écriture,
  pas la projection) ; (2) résolution déterministe, au plus un responsable de fond par jour
  (responsable unique, règle 10), fonction pure de la date ISO reproductible via `IDateTimeProvider` ;
  (3) la période explicite prime toujours sur le fond et ne déborde jamais sur les jours voisins.
- **Points d'attention TDD** — tester la résolution sur une semaine ISO **paire ET impaire** via
  `IDateTimeProvider` (lun 29/06/2026 = ISO 27 impaire ; lun 06/07/2026 = ISO 28 paire), jamais
  `Now` ; doubler **uniquement** le port cycle (InMemory), **pas** d'adaptateur Mongo (cycle
  volatile ici) ; ne pas régresser la résolution des périodes explicites existantes. Drivers réels
  neufs : Sc.1 (modèle + résolution fond), Sc.2 (priorité surcharge > fond), Sc.7 (validation N ≥ 1).
  Lot de caractérisations probablement **early-green**, groupable côté `tdd-auto` : Sc.3 (édition +
  diffusion), Sc.4 + Sc.5 (résolution neutre / N=1, consécutives), Sc.6 (concurrence singleton,
  pattern s08 Sc.7), Sc.8 (échec clair, pattern s09 Sc.9).

## Scénarios

Feature: Récurrence des périodes — définir le cycle de fond. Un parent déclare depuis la
configuration du foyer un cycle de N semaines qui répartit la responsabilité de garde **par
défaut** (semaine paire / impaire), `index = numéro de semaine ISO 8601 modulo N`. La grille
résout le responsable de fond pour tout jour **sans** période explicite (case + nom + couleur +
légende) ; les périodes saisies au calendrier restent des **surcharges ponctuelles** qui priment
puis laissent le cycle reprendre. L'édition est immédiate et converge entre écrans. Le cycle vit
en mémoire (sa durabilité est portée par le palier 9).

### Scenario 1 — Définir un cycle de 2 semaines : le fond alterne par parité ISO `@nominal`

```gherkin
Scenario: Définir un cycle de 2 semaines : le fond alterne par parité ISO
  Given le foyer compte Parent A en bleu et Parent B en orange
  And aucune période de garde explicite n'a été saisie
  And la date de référence est le lundi 29 juin 2026 (semaine ISO 27)
  When un parent définit, depuis la configuration du foyer, un cycle de fond de 2 semaines :
    index pair → Parent A, index impair → Parent B
  Then les jours du 29 juin au 5 juillet 2026 (ISO 27, impaire) affichent « Parent B » en orange,
    en case comme en légende
  And les jours du 6 au 12 juillet 2026 (ISO 28, paire) affichent « Parent A » en bleu
  And l'alternance se poursuit sur les semaines suivantes de la fenêtre sans aucune saisie supplémentaire
```

### Scenario 2 — Une surcharge ponctuelle prime sur le fond puis le cycle reprend `@nominal`

```gherkin
Scenario: Une surcharge ponctuelle prime sur le fond puis le cycle reprend
  Given un cycle de fond de 2 semaines est défini : index pair → Parent A bleu, index impair → Parent B orange
  And la semaine du 6 au 12 juillet 2026 (ISO 28, paire) revient par défaut à Parent A
  When un parent affecte explicitement Parent B à la seule journée du 8 juillet 2026
  Then la case du 8 juillet 2026 affiche « Parent B » en orange
  And les cases du 7 et du 9 juillet 2026 affichent toujours « Parent A » en bleu
  And le cycle de fond reprend de part et d'autre de la surcharge
```

### Scenario 3 — Inverser le mapping du cycle met à jour la grille sans rechargement `@nominal`

```gherkin
Scenario: Inverser le mapping du cycle met à jour la grille sans rechargement
  Given un cycle de fond de 2 semaines est défini : index pair → Parent A bleu, index impair → Parent B orange
  And la grille affiche « Parent A » en bleu sur la semaine ISO 28 (6-12 juillet 2026)
  When un parent inverse le mapping depuis l'écran de configuration : index pair → Parent B, index impair → Parent A
  Then les cases du 6 au 12 juillet 2026 affichent désormais « Parent B » en orange, en case comme en légende
  And la grille suit sans rechargement
```

### Scenario 4 — Un index de cycle sans responsable retombe sur la teinte neutre `@limite`

```gherkin
Scenario Outline: Un index de cycle sans responsable retombe sur la teinte neutre
  Given le foyer compte Parent A en bleu et Parent B en orange
  And aucune période explicite n'est saisie
  And le cycle de fond est défini avec <mapping>
  When la grille est rendue à la semaine <semaine>
  Then les cases de fond affichent <rendu>

  Examples:
    | mapping                                            | semaine          | rendu                                                  |
    | index pair → Parent A, index impair non affecté    | ISO 27 (impaire) | gris neutre, sans nom, et aucune entrée de légende     |
    | index pair → Parent A, index impair non affecté    | ISO 28 (paire)   | « Parent A » en bleu (contrôle positif)                |
    | aucun index affecté (cycle vide)                   | ISO 27 (impaire) | gris neutre, sans nom, et aucune entrée de légende     |
```

### Scenario 5 — Cycle d'une seule semaine : aucune alternance, même responsable partout `@limite`

```gherkin
Scenario: Cycle d'une seule semaine : aucune alternance, même responsable partout
  Given un cycle de fond d'une seule semaine est défini : index 0 → Parent A bleu
  And aucune période explicite n'est saisie
  And la date de référence est le lundi 29 juin 2026 (semaine ISO 27)
  When la grille est rendue sur la fenêtre de 5 semaines à partir de la semaine ISO 27
  Then toutes les semaines affichées (ISO 27 à 31) affichent « Parent A » en bleu en fond, sans aucune alternance
  And la légende ne comporte que « Parent A » en bleu
```

### Scenario 6 — Deux parents éditent le cycle en même temps : dernière écriture gagne `@limite`

```gherkin
Scenario: Deux parents éditent le cycle en même temps : dernière écriture gagne
  Given le foyer compte Parent A en bleu, Parent B en orange et Parent C en vert
  And un cycle de fond de 2 semaines est défini : index pair → Parent A, index impair → Parent B
  When un premier parent règle l'index pair sur Parent A depuis un écran
  And un second parent règle l'index pair sur Parent C juste après depuis un autre écran
  Then les deux grilles affichent « Parent C » en vert sur les semaines d'index pair (ISO 28, 6-12 juillet 2026)
  And aucun message de rejet n'apparaît
  And la convergence se fait sans rechargement
```

### Scenario 7 — Définir un cycle de zéro semaine est refusé `@erreur`

```gherkin
Scenario: Définir un cycle de zéro semaine est refusé
  Given le foyer affiche ses acteurs et un cycle de fond de 2 semaines déjà défini :
    index pair → Parent A, index impair → Parent B
  When un parent tente d'enregistrer un cycle de zéro semaine
  Then l'édition est refusée avec le message « le cycle doit compter au moins une semaine »
  And le cycle de 2 semaines précédent reste inchangé
```

### Scenario 8 — Édition du cycle impossible si le service de configuration est injoignable `@erreur`

```gherkin
Scenario: Édition du cycle impossible si le service de configuration est injoignable
  Given un parent a saisi un cycle de 2 semaines (index pair → Parent A bleu, index impair → Parent B orange)
    dans l'écran de configuration
  When il valide l'édition du cycle alors que le service de configuration est injoignable
  Then un message d'échec clair s'affiche
  And la saisie du cycle reste à l'écran à resoumettre
  And aucun cycle n'est enregistré
```

## Risques

- **Borne anti-cliquet (règle 30).** Le cycle de fond reste **en mémoire** (adaptateur InMemory) ;
  sa durabilité est portée par le palier 9 (« reste de la config »). Ne **pas** tirer Mongo en avant
  au prétexte que la config acteurs est déjà durable.
- **Discontinuité de parité ISO à la jonction d'année (assumée).** L'ancrage ISO pur peut enchaîner
  deux semaines de même parité au changement d'année (une année à 53 semaines : ISO 53 impaire → ISO 1
  impaire = même responsable deux semaines de suite). Conséquence **assumée** de l'option ISO 8601 ;
  documentée, non bloquante pour ce palier.
- **Non-régression des périodes explicites.** Ajouter la couche fond dans `GrilleAgendaQuery` /
  `ResponsabiliteQuery` ne doit pas altérer le rendu des périodes déjà saisies (priorité surcharge >
  fond) : Sc.2 est le garde-fou.
- **Parcimonie / vélocité.** 3 drivers réels (Sc.1, Sc.2, Sc.7) ; lot de 5 caractérisations
  probablement early-green (Sc.3 ; Sc.4 + Sc.5 consécutives ; Sc.6 ; Sc.8) regroupables côté
  `tdd-auto` pour ne pas diluer la vélocité.
- **Coût de saisie du cycle.** Maîtrisé par le grain hebdo (1 responsable/semaine, mapping court) ;
  le grain journalier a été écarté (débordement > 2h).
- **Tranche de secours.** Si débordement ~2h réel au `/3` : replier sur un **cycle d'une seule
  semaine** (responsable de fond unique, sans alternance multi-semaines) en livrant Sc.5 / Sc.1
  dégénéré. À n'activer que sur débordement avéré, pas par précaution.
