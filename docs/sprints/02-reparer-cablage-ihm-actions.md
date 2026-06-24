# Réparer le câblage IHM des actions d'écriture — Analyse & scénarios

## Analyse technique

- **Composants impactés**
  - Blazor — `PoserSlot.razor`, `AffecterPeriode.razor`, `DefinirTransfert.razor`,
    `PlanningPartage.razor` (bloc d'édition de période inline) : sélecteurs
    `InputSelect` peuplés depuis `Infrastructure.Foyer` (`Lieux`, `Responsables`),
    binding `@bind-Value`, construction de la commande, appel du handler.
  - Application (handlers **existants, inchangés**) — `PoserSlotHandler`,
    `AffecterPeriodeHandler`, `DefinirTransfertHandler`, `ModifierPeriodeHandler` ;
    les commandes associées sont les contrats de transmission.
  - Infrastructure — `Foyer` (référentiel lieux/responsables),
    `FoyerLieuRepository` / `FoyerResponsableRepository` : portent l'invariant de
    cohérence (toute valeur sélectionnable est acceptée par le handler).

- **Couches & dépendances** — l'IHM (Infrastructure–Blazor) dépend vers l'intérieur
  des handlers Application ; le domaine (gardes métier) reste sans `using` de
  framework. Litmus : le câblage est testable en composant bUnit sans toucher au
  domaine ; les repositories sont remplaçables sans modifier les handlers.

- **Contrats de données**
  - Le sélecteur de lieu transmet un `LieuId` appartenant à `Foyer.Lieux`
    (ex : « école ») → le handler ne renvoie pas « Le lieu visé n'existe pas ».
  - Le sélecteur de responsable transmet un `ResponsableId` appartenant à
    `Foyer.Responsables` (ex : « Parent A ») → pas de « Un responsable est requis ».
  - `definir-transfert` transmet la récupération (ex : « Parent B ») **et** l'heure
    (`input type=time` → `TimeOnly` 08:30 → `TimeSpan`) → pas de « Transfert
    incomplet ».
  - `Modifier` une période transmet `ModifierPeriodeCommand(base observée,
    modification)` à `ModifierPeriodeHandler`.

- **Write vs read (CQRS)** — chaque action est une **modification** passant par son
  handler/agrégat (invariants protégés). L'affichage du planning (slots, périodes,
  transferts) reste une lecture par projection ; ce sprint ne touche pas la lecture,
  seulement la transmission de l'écriture.

- **Invariants**
  - Toute valeur proposée par un sélecteur appartient au référentiel validé par le
    repository correspondant — aucune sélection valide ne peut produire une erreur
    « n'existe pas / requis ».
  - Les gardes métier (lieu inexistant, responsable requis, transfert incomplet,
    écriture périmée) restent dans les handlers/domaine et **ne sont pas réécrites**
    par ce sprint.

- **Points d'attention TDD**
  - Périmètre = câblage seul : les cas **nominaux pilotent 100 % du code produit**
    (peuplement du sélecteur + transmission). **Pas** de scénario d'erreur sur les
    gardes métier (lieu/responsable/transfert) : déjà verts au sprint 1 (scénarios
    4/8/12 archivés) → réécrits ici, ils seraient des *early green* (caractérisation,
    pas driver).
  - Tests d'acceptation au niveau **composant bUnit** : rendre la page, sélectionner
    une option valide du référentiel, soumettre, constater l'absence de
    `[data-testid=motif-echec]`, la navigation vers `/planning` et l'état métier
    enregistré (slot / période / transfert avec valeurs concrètes).
  - `Modifier` période : tester le câblage `@onclick` (ouverture inline) puis
    `Enregistrer` → `ModifierPeriodeHandler` ; **ne pas** réécrire le rejet
    d'écriture périmée (déjà couvert au sprint 1).

## Scénarios

Feature: Câbler les actions d'écriture du hub `/planning` à leur handler — chaque
dialog peuple ses sélecteurs depuis le référentiel du foyer (lieux, responsables) et
transmet une valeur valide, pour que poser un slot, affecter une période, définir un
transfert et modifier une période **réussissent depuis l'IHM** au lieu d'échouer
invariablement.

### Scenario 1 — Poser un slot avec un lieu du foyer `@nominal` `@vert` <!-- vert — f9ca58d -->

```gherkin
Scenario: Poser un slot depuis l'IHM avec un lieu du foyer
  Given un Parent sur la dialog « Poser un slot » du planning de Léa
  And le sélecteur de lieu propose les lieux du foyer : école, domicile A, domicile B, nounou
  When il choisit le lieu « école », saisit le 15/07/2025 de 08 h 30 à 16 h 30 et valide
  Then le slot de Léa à « école » le 15/07 de 08 h 30 à 16 h 30 apparaît dans la section Localisation du planning
  And aucun message d'échec n'est affiché
```

### Scenario 2 — Affecter une période avec un responsable du foyer `@nominal` `@vert` <!-- vert — fdb3686 -->

```gherkin
Scenario: Affecter une période depuis l'IHM avec un responsable du foyer
  Given un Parent sur la dialog « Affecter une période »
  And le sélecteur de responsable propose les responsables du foyer : Parent A, Parent B
  When il choisit « Parent A », saisit du 14/07/2025 au 21/07/2025 et valide
  Then la période « Parent A responsable du 14/07 au 21/07 » apparaît dans la section Responsabilité du planning
  And aucun message d'échec n'est affiché
```

### Scenario 3 — Définir un transfert avec récupération et heure transmises `@nominal` `@vert`

```gherkin
Scenario: Définir un transfert depuis l'IHM avec récupération et heure transmises
  Given un Parent sur la dialog « Définir un transfert »
  And les sélecteurs proposent les responsables (Parent A, Parent B) et les lieux du foyer
  When il choisit dépose « Parent A », récupère « Parent B », lieu « école », le 21/07/2025 à 08 h 30 et valide
  Then le transfert « dépose Parent A → récupère Parent B, école, 08 h le 21/07 » apparaît dans la section Transferts du planning
  And aucun message d'échec n'est affiché
```

### Scenario 4 — Modifier une période depuis le bouton Modifier `@nominal`

```gherkin
Scenario: Modifier une période depuis le bouton Modifier du planning
  Given un Parent sur le planning où figure la période « Parent A responsable du 14/07 au 21/07 »
  When il clique « Modifier », le formulaire inline pré-rempli s'ouvre, il choisit « Parent B » et enregistre
  Then la période affichée devient « Parent B responsable du 14/07 au 21/07 »
  And aucun message d'échec n'est affiché
```
