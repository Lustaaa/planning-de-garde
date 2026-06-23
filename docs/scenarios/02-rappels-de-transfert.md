# Rappels de transfert — Analyse & scénarios

## Analyse technique

- **Composants impactés** — agrégat `Transfert` { déposeParId, récupèreParId, lieuId, heure, date } de la phase 1, enrichi d'un `délaiRappel` (15/30/60 min) — la modification du délai passe par l'agrégat (Domain) ; use case Application « programmer / recaler / annuler le rappel » réagissant au cycle de vie du transfert (créé / heure modifiée / supprimé) ; planificateur temporel + hub notifications in-app (Infrastructure/SignalR).
- **Couches & dépendances** — le domaine ne porte aucun `using` de framework ni de timer ; le planificateur et le canal de notification vivent en Infrastructure, remplaçables sans toucher au domaine ; le use case orchestre mais n'invente aucun invariant.
- **Contrats de données** — entrée : `Transfert` avec `heure`, `date`, `déposeParId`, `récupèreParId`, `délaiRappel ∈ {15, 30, 60}` min ; sortie observable : une notification in-app par acteur ciblé (déposant + récupérant) nommant lieu + heure, émise à `heure − délaiRappel`.
- **Write vs read (CQRS)** — `délaiRappel` sert un invariant du transfert (timing d'émission), il vit donc sur l'agrégat `Transfert` ; l'émission elle-même est un effet déclenché par le franchissement temporel, pas une projection de lecture.
- **Invariants** — un rappel est toujours dérivé de l'**état courant** du transfert (pas d'émission pour un transfert supprimé, ni à une heure qu'il ne porte plus) ; **pas d'émission rétroactive** (si `heure − délai` est déjà passé à la programmation, rien n'est émis) ; **exactement une émission** par acteur et par transfert.
- **Points d'attention TDD** — ne doubler que les ports (l'horloge donnant l'instant T, le port de notification), jamais le domaine ni le planificateur réel ; tester le recalage en avançant l'horloge (ancienne heure → rien, nouvelle heure → une émission) ; vérifier l'idempotence en re-déclenchant la programmation sur le même transfert (compter une seule notif par acteur).

## Scénarios

Feature: Rappels de transfert — à l'approche d'un transfert, l'app prévient
automatiquement et à l'avance le déposant et le récupérant (notification in-app,
délai paramétrable 15/30/60 min), pour qu'aucune dépose ni récupération ne soit
oubliée. Le rappel suit toujours l'état courant du transfert : il se recale si
l'heure change, s'annule si le transfert disparaît.

### Scenario 1 — Rappel émis aux deux acteurs à l'approche du transfert `@nominal`

```gherkin
Scenario: Rappel émis aux deux acteurs à l'approche du transfert
  Given un transfert « Parent A dépose Léa à l'école à 8h30 le 21/07, Parent B récupère » existe dans le planning
  And ce transfert a un délai de rappel de 30 min
  When l'heure atteint 8h00 le 21/07
  Then « Parent A » et « Parent B » reçoivent chacun une notification « Transfert de Léa à l'école à 8h30 »
  And aucun autre membre du foyer n'est notifié
```

### Scenario 2 — Délai de 60 min décale l'émission `@limite`

```gherkin
Scenario: Délai de 60 min décale l'émission
  Given un transfert « Parent A dépose Léa chez la nounou à 17h00 le 21/07, Parent B récupère » existe dans le planning
  And ce transfert a un délai de rappel de 60 min
  When l'heure atteint 16h00 le 21/07
  Then « Parent A » et « Parent B » reçoivent chacun une notification « Transfert de Léa chez la nounou à 17h00 »
  And à 16h30 le 21/07 aucune notification supplémentaire n'a été émise
```

### Scenario 3 — Heure du transfert modifiée, le rappel se recale `@limite`

```gherkin
Scenario: Heure du transfert modifiée, le rappel se recale
  Given un transfert « Parent A dépose Léa à l'école à 8h30 le 21/07, Parent B récupère » avec un délai de rappel de 30 min existe dans le planning
  And un Parent a décalé l'heure de ce transfert à 9h30 le 21/07
  When l'heure atteint 8h00 le 21/07 puis 9h00 le 21/07
  Then aucune notification de rappel n'est émise à 8h00
  And à 9h00 « Parent A » et « Parent B » reçoivent chacun une notification « Transfert de Léa à l'école à 9h30 »
```

### Scenario 4 — Transfert supprimé avant l'heure, rappel annulé `@erreur`

```gherkin
Scenario: Transfert supprimé avant l'heure, rappel annulé
  Given un transfert « Parent A dépose Léa à l'école à 8h30 le 21/07, Parent B récupère » avec un délai de rappel de 30 min existe dans le planning
  And un Parent a supprimé ce transfert à 7h00 le 21/07
  When l'heure atteint 8h00 le 21/07
  Then aucune notification de rappel n'est émise à « Parent A » ni à « Parent B »
```

### Scenario 5 — Rappel dont l'heure est déjà passée, pas d'émission rétroactive `@erreur`

```gherkin
Scenario: Rappel dont l'heure est déjà passée, pas d'émission rétroactive
  Given il est 8h15 le 21/07
  And un Parent crée un transfert « dépose Léa à l'école à 8h30 le 21/07, Parent B récupère » avec un délai de rappel de 30 min
  When le transfert est enregistré, alors que son heure de rappel 8h00 est déjà passée
  Then aucun rappel n'est émis à « Parent A » ni à « Parent B » pour ce transfert
```

### Scenario 6 — Pas de double rappel pour le même transfert `@limite`

```gherkin
Scenario: Pas de double rappel pour le même transfert
  Given un transfert « Parent A dépose Léa à l'école à 8h30 le 21/07, Parent B récupère » avec un délai de rappel de 30 min existe dans le planning
  And la notification de rappel a déjà été reçue à 8h00 le 21/07 par « Parent A » et « Parent B »
  When le planning est rouvert et l'état de ce transfert est ré-évalué à 8h05 le 21/07
  Then aucune nouvelle notification de rappel n'est émise
  And « Parent A » et « Parent B » ont reçu exactement une notification pour ce transfert
```

## Risques & questions ouvertes

- **Idempotence après redémarrage** — garantir « exactement une émission par acteur » exige de mémoriser l'émission ; le mécanisme de marquage (état persisté du rappel vs reconstruction au démarrage) reste à fixer.
- **Source de l'instant T** — le planificateur dépend d'une horloge ; le fuseau horaire et l'heure de référence (serveur vs membre) restent à trancher.
- **Délai figé** — l'ensemble {15, 30, 60} min est figé en phase 2 ; un délai libre ou une valeur par défaut au niveau du foyer est hors périmètre.
- **Acteur indisponible** — le comportement si un acteur du transfert n'est plus membre du foyer au moment du rappel (déposant retiré) reste à traiter.
