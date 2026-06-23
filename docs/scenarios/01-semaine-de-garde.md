# Semaine de garde — Analyse & scénarios

## Analyse technique

- **Composants impactés** — agrégats `PériodeDeGarde`, `Transfert` et `SlotDeLocalisation` (Domain) ; use cases d'affectation de période, de définition de transfert et de pose de slot (Application) ; persistance + hub temps réel + notifications in-app (Infrastructure/SignalR) ; vue planning Blazor.
- **Couches & dépendances** — le domaine ne porte aucun `using` de framework ; SignalR et persistance vivent en Infrastructure, remplaçables sans toucher au domaine ; les droits Parent/Invité sont gardés à l'entrée de l'Application.
- **Contrats de données** — `PériodeDeGarde { responsableId, début, fin }` (bornes paramétrables) ; `Transfert { déposeParId, récupèreParId, lieuId, heure, date }` (point de bascule A↔B) ; `SlotDeLocalisation { enfantId, lieuId, début→fin }` (**sans responsable**).
- **Write vs read (CQRS)** — responsabilité (période) et localisation (slot) sont **deux axes orthogonaux** : le slot ne porte pas de responsable. Toute modification passe par l'agrégat concerné ; l'avertissement de chevauchement est une **projection de lecture** sur la journée d'un enfant, pas un invariant.
- **Invariants** — `PériodeDeGarde` : exactement un responsable, fin > début. `SlotDeLocalisation` : fin > début (durée nulle interdite, franchissement de minuit autorisé), lieu référencé existant. `Transfert` : dépose + récupère + lieu + heure tous renseignés.
- **Points d'attention TDD** — tester le rejet de l'écriture périmée sur la période en ne doublant que le port de persistance ; distinguer durée nulle d'un slot franchissant minuit ; le transfert borne les périodes et bascule la responsabilité ; ne doubler que les ports, jamais le domaine.

## Scénarios

Feature: Semaine de garde — le foyer partage une source de vérité à deux axes :
**qui est responsable** (période de garde paramétrable, bascule à un transfert) et
**où est l'enfant** (slots de localisation à l'horaire fin). Le planning reflète
l'état et notifie les autres membres en temps réel.

### Scenario 1 — Un Parent pose un slot de localisation `@nominal` `@vert` <!-- vert — 1d635ea -->

```gherkin
Scenario: Un Parent pose un slot de localisation
  Given un Parent connecté au planning du foyer
  And l'enfant « Léa » et le lieu « école » existent
  When le Parent place Léa à l'école de 8h30 à 16h30 le mardi 15/07
  Then le slot « Léa à l'école 8h30–16h30 le 15/07 » apparaît dans le planning partagé
  And l'Invité reçoit une notification de mise à jour du planning
```

### Scenario 2 — Slot de durée nulle refusé `@erreur` `@vert` <!-- vert — 398fd73 -->

```gherkin
Scenario: Slot de durée nulle refusé
  Given un Parent connecté au planning du foyer
  And l'enfant « Léa » et le lieu « école » existent
  When le Parent place Léa à l'école de 16h30 à 16h30 le mardi 15/07
  Then la création est refusée car la durée est nulle
  And aucun slot « Léa à l'école le 15/07 » n'apparaît dans le planning partagé
```

### Scenario 3 — Slot de nuit franchissant minuit `@limite` `@vert` <!-- vert — 14822ad -->

```gherkin
Scenario: Slot de nuit franchissant minuit
  Given un Parent connecté au planning du foyer
  And l'enfant « Léa » et le lieu « domicile A » existent
  When le Parent place Léa au domicile A de 22h le 15/07 à 7h le 16/07
  Then le slot « Léa au domicile A 22h–7h du 15/07 au 16/07 » apparaît dans le planning partagé
```

### Scenario 4 — Lieu inexistant `@erreur` `@vert` <!-- vert — 9f6982b -->

```gherkin
Scenario: Lieu inexistant
  Given un Parent connecté au planning du foyer
  And l'enfant « Léa » existe
  And le lieu « ancienne crèche » n'existe pas dans la liste des lieux du foyer
  When le Parent place Léa au lieu « ancienne crèche » de 8h30 à 16h30 le mardi 15/07
  Then la création est refusée car le lieu n'existe pas
  And aucun slot « Léa le 15/07 » n'apparaît dans le planning partagé
```

### Scenario 5 — Chevauchement de localisation pour le même enfant `@limite` `@vert`

```gherkin
Scenario: Chevauchement de localisation — créé mais signalé
  Given un slot « Léa à l'école 8h30–16h30 le 15/07 » existe dans le planning
  And un Parent connecté au planning du foyer
  When le Parent place Léa chez la nounou de 16h à 18h le mardi 15/07
  Then le slot « Léa chez la nounou 16h–18h le 15/07 » est créé et apparaît dans le planning partagé
  And le planning affiche un avertissement de chevauchement entre les slots de Léa le 15/07
```

### Scenario 6 — Un Invité tente d'éditer un slot `@erreur`

```gherkin
Scenario: Un Invité tente d'éditer un slot
  Given un slot « Léa à l'école 8h30–16h30 le 15/07 » existe dans le planning
  And un Invité connecté au planning du foyer, en consultation seule
  When l'Invité tente de déplacer ce slot chez la nounou
  Then l'action est refusée car l'Invité est en consultation seule
  And le slot reste « Léa à l'école 8h30–16h30 le 15/07 »
```

### Scenario 7 — Un Parent affecte la responsabilité d'une période de garde `@nominal`

```gherkin
Scenario: Affecter la responsabilité d'une période de garde
  Given un Parent connecté au planning du foyer
  And les responsables « Parent A » et « Parent B » existent
  When le Parent rend « Parent A » responsable de Léa du lundi 14/07 au lundi 21/07
  Then le planning partagé indique « Parent A responsable du 14/07 au 21/07 »
  And « Parent A » reste responsable quel que soit le lieu où se trouve Léa pendant la période
```

### Scenario 8 — Période sans responsable refusée `@erreur`

```gherkin
Scenario: Période sans responsable refusée
  Given un Parent connecté au planning du foyer
  When le Parent crée une période de garde du lundi 14/07 au lundi 21/07 sans désigner de responsable
  Then la création est refusée car un responsable est requis
  And aucune période « du 14/07 au 21/07 » n'apparaît dans le planning partagé
```

### Scenario 9 — Bornes de période paramétrables `@limite`

```gherkin
Scenario: Bornes de période paramétrables
  Given un Parent connecté au planning du foyer
  And le responsable « Parent B » existe
  When le Parent rend « Parent B » responsable de Léa du mercredi 16/07 au mercredi 23/07
  Then le planning partagé indique « Parent B responsable du 16/07 au 23/07 »
```

### Scenario 10 — Édition concurrente d'une période `@erreur`

```gherkin
Scenario: Édition concurrente — la modification fondée sur un état périmé est rejetée
  Given une période « Parent A responsable du 14/07 au 21/07 » existe dans le planning
  And le Parent X et le Parent Y affichent tous deux cette même période
  And le Parent X a enregistré le remplacement du responsable par « Parent B »
  When le Parent Y enregistre, depuis son affichage périmé, le décalage de la fin au 22/07
  Then la modification du Parent Y est rejetée car elle se fonde sur un état périmé
  And le Parent Y est invité à recharger l'état à jour de la période
```

### Scenario 11 — Définir le transfert de bascule entre deux parents `@nominal`

```gherkin
Scenario: Définir le transfert de bascule
  Given une période « Parent A responsable jusqu'au lundi 21/07 » existe dans le planning
  And une période « Parent B responsable à partir du lundi 21/07 » existe dans le planning
  When le Parent définit le transfert du lundi 21/07 : « Parent A » dépose Léa à l'école à 8h30, « Parent B » la récupère
  Then le planning partagé affiche le transfert « dépose Parent A → récupère Parent B, école, 8h30 le 21/07 »
  And la responsabilité bascule de « Parent A » à « Parent B » à ce transfert
```

### Scenario 12 — Transfert incomplet refusé `@erreur`

```gherkin
Scenario: Transfert incomplet refusé
  Given une période « Parent A responsable jusqu'au lundi 21/07 » existe dans le planning
  When le Parent définit le transfert du lundi 21/07 avec dépose par « Parent A » à l'école, sans préciser qui récupère ni à quelle heure
  Then la définition du transfert est refusée car la récupération et l'heure sont requises
  And aucun transfert « du 21/07 » n'apparaît dans le planning partagé
```

## Risques & questions ouvertes

- **Récurrence du cycle** — la répétition automatique multi-semaines (pair/impair) est hors socle ; à introduire en phase 3 une fois ce modèle adopté.
- **Définition de « simultané »** — la granularité de version qui détermine quand deux éditions d'une période sont en conflit reste à fixer.
- **Cohérence période ↔ transfert** — un transfert doit border deux périodes contiguës ; le comportement si les périodes se chevauchent ou laissent un trou reste à traiter.
- **Lieu supprimé encore référencé** — l'intégrité d'un lieu supprimé alors qu'un slot existant le référence est à traiter.
