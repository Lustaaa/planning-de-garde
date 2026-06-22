<!-- Généré via le skill make-gherkin (test sous pression autonome). Exemple, non lié au produit planning-de-garde. -->

# Trombonomicon — Analyse & scénarios

## Analyse technique

### Composants ciblés (.NET / Blazor / SignalR)

- **`PaperclipEngine`** (C# pur, sans framework) — moteur de règles : production manuelle, machines, marché, approvisionnement, condition de victoire. Toutes les règles de gestion numérotées de la spec vivent ici. Testable en pur unitaire xUnit sans UI.
- **`OfflineProductionCalculator`** (C# pur) — calcul déterministe de la production accumulée entre deux timestamps, borné par le fil disponible et un plafond de 8 h. Entrée : `GameState` + `DateTimeOffset elapsed`. Sortie : `OfflineSummary` (trombones crédités, fil consommé, durée prise en compte).
- **`GameStateStore`** (Blazor, `IJSRuntime`) — persistance locale via `localStorage`. Sérialise / désérialise `GameState` (JSON). Seul point d'I/O côté client.
- **`GameLoop`** (Blazor `ComponentBase`) — timer `System.Threading.Timer` à 1 s ; invoque `PaperclipEngine.Tick()` et déclenche le re-render. Pas de SignalR en phase 1 (client-only) ; SignalR envisageable pour la phase multijoueur ou tableau des scores.
- **`GameHubClient`** (SignalR, optionnel) — si tableau des scores global ou diffusion d'événements de victoire ; hors périmètre des scénarios présents.

### Contrats clés

- `PaperclipEngine.ClickProduce(state) : ProduceResult` — renvoie `{ Produced, WireConsumed, Blocked: bool }`.
- `PaperclipEngine.Tick(state, elapsed) : TickResult` — avance les machines, calcule les ventes selon la demande courante, renvoie le delta de chaque ressource.
- `PaperclipEngine.BuyMachine(state) : BuyResult` — vérifie le budget, augmente le compteur de machines, recalcule le coût suivant.
- `PaperclipEngine.CheckVictory(state) : bool` — renvoie vrai si `state.TotalProduced >= VictoryThreshold`.
- `OfflineProductionCalculator.Compute(state, elapsed) : OfflineSummary`.

### Points TDD ciblés

- **Règle 3 (clic à vide)** : `ClickProduce` avec fil = 0 → `Blocked = true`, état inchangé.
- **Règle 12 (arrêt machine sur pénurie)** : `Tick` avec fil = 0 et machines > 0 → delta trombones = 0.
- **Règle 11 (coût croissant)** : après N achats de machine, le coût de la machine N+1 est strictement supérieur au coût de la machine N.
- **Règle 15 (plafonnement hors-ligne)** : `Compute` avec elapsed > 8 h → `Summary.DurationConsidered = 8 h`.
- **Règle 18 (demande inverse)** : à prix bas la demande est supérieure à la demande à prix haut.
- **Règle 29 (victoire)** : `CheckVictory` passe à `true` exactement quand `TotalProduced` atteint le seuil ; le `Tick` suivant ne modifie plus l'état.

---

## Scénarios

```gherkin
Feature: Trombonomicon — boucle clicker / idle

  # ──────────────────────────────────────────────
  # Boucle manuelle
  # ──────────────────────────────────────────────

  @nominal
  Scenario 1: Clic producteur avec fil disponible
    Given une partie initialisée avec 100 m de fil de fer, 0 trombone en stock et 0 €
    And le prix de vente est fixé à 0,10 €
    When le joueur clique sur "Produire"
    Then le compteur "Trombones produits" augmente de 1
    And le stock de fil de fer diminue de 1 unité
    And l'argent augmente de 0,10 € (vente automatique)
    And le stock de trombones reste à 0 (vendu immédiatement)

  @limite
  Scenario 2: Clic à vide sans fil de fer
    Given une partie avec 0 m de fil de fer et 5 trombones en stock
    When le joueur clique sur "Produire"
    Then aucun trombone n'est produit
    And le stock de fil de fer reste à 0
    And un message "Fil de fer insuffisant" est visible
    And l'argent reste inchangé

  @nominal
  Scenario 3: Achat d'une amélioration de clic
    Given une partie avec 10 € disponibles
    And le bouton "Améliorer le clic (×2) — 8 €" est débloqué
    When le joueur achète l'amélioration
    Then l'argent disponible diminue de 8 €
    And le prochain clic produit 2 trombones au lieu de 1

  @erreur
  Scenario 4: Tentative d'achat avec budget insuffisant
    Given une partie avec 3 € disponibles
    And le coût de la prochaine machine est 10 €
    When le joueur tente d'acheter une machine
    Then l'achat est refusé
    And l'argent reste à 3 €
    And le nombre de machines reste inchangé
    And un message "Fonds insuffisants" est visible

  # ──────────────────────────────────────────────
  # Machines auto-productrices
  # ──────────────────────────────────────────────

  @nominal
  Scenario 5: Machine produit des trombones à chaque tick
    Given une partie avec 1 machine achetée, 50 m de fil disponible et 0 trombone
    When 1 seconde de jeu s'écoule (1 tick)
    Then le stock de trombones augmente du débit de la machine
    And le fil de fer diminue du même montant de consommation
    And l'argent augmente en proportion des trombones vendus

  @limite
  Scenario 6: Machine s'arrête lorsque le fil est épuisé
    Given une partie avec 2 machines actives et 0 m de fil de fer
    When 1 tick s'écoule
    Then aucun trombone n'est produit par les machines
    And le stock de fil de fer reste à 0
    And aucune valeur négative n'apparaît dans les compteurs

  @nominal
  Scenario 7: Coût croissant de la deuxième machine
    Given une partie avec 1 machine déjà achetée au coût de 100 €
    When le joueur consulte le coût de la prochaine machine
    Then le coût affiché est strictement supérieur à 100 €

  # ──────────────────────────────────────────────
  # Marché — prix & demande
  # ──────────────────────────────────────────────

  @nominal
  Scenario 8: Demande plus élevée à prix bas qu'à prix haut
    Given une partie sur le marché avec une demande calculée à prix 0,05 € et à prix 0,20 €
    When on compare les deux valeurs de demande
    Then la demande à 0,05 € est supérieure à la demande à 0,20 €
    And le stock non vendu à prix haut s'accumule en stock de trombones

  @limite
  Scenario 9: Prix nul — trombones non vendus
    Given une partie avec un prix de vente fixé à 0,00 €
    And 10 trombones produits
    When 1 tick s'écoule
    Then les trombones vendus ne rapportent aucun argent
    And le moteur ne produit pas de valeur négative d'argent

  # ──────────────────────────────────────────────
  # Persistance & production hors-ligne
  # ──────────────────────────────────────────────

  @nominal
  Scenario 10: Production hors-ligne créditée à la réouverture
    Given une partie sauvegardée avec 5 machines actives et 1 000 m de fil disponible
    And le joueur ferme l'application pendant 2 heures
    When le joueur rouvre l'application
    Then un récapitulatif "Pendant votre absence : +X trombones, +Y €" est affiché
    And les compteurs reflètent la production des 2 heures écoulées
    And le fil de fer restant est cohérent avec la consommation calculée

  @limite
  Scenario 11: Production hors-ligne plafonnée à 8 heures
    Given une partie sauvegardée avec des machines actives et du fil suffisant
    And le joueur ferme l'application pendant 24 heures
    When le joueur rouvre l'application
    Then la production créditée correspond au maximum à 8 heures de production
    And le récapitulatif indique "Production créditée limitée à 8 h"

  # ──────────────────────────────────────────────
  # Fin de partie
  # ──────────────────────────────────────────────

  @nominal
  Scenario 12: Condition de victoire atteinte
    Given une partie où le seuil de victoire est 1 000 000 trombones cumulés
    And le compteur "Trombones produits" est à 999 999
    When le joueur clique sur "Produire" et produit 1 trombone supplémentaire
    Then l'écran de victoire s'affiche
    And le message "Objectif atteint : 1 000 000 trombones !" est visible
    And la partie est figée (bouton "Produire" désactivé)

  @erreur
  Scenario 13: Clics et machines sans effet après la victoire
    Given une partie dont l'écran de victoire est affiché
    When le joueur tente de cliquer sur "Produire"
    And un tick de machine s'écoule
    Then aucun compteur ne change
    And l'état de la partie reste figé
```

---

**Hypothèses documentées (decisions autonomes)**

- Demande : modèle linéaire inverse `D = D_max − k × prix`, D ≥ 0. Paramètres calibrables par constantes.
- Vente automatique active dès le départ (règle 6) ; pas de gestion manuelle du stock dans ces scénarios.
- Tick = 1 seconde ; contrôlable via injection de `IClock` pour les tests.
- Plafond hors-ligne = 8 heures (tranché en hypothèse Q4).
- Seuil de victoire = 1 000 000 trombones cumulés (exemple illustratif, paramétrable).
- Prix nul est un état limite légal (Scenario 9) : aucune erreur, mais revenu nul.
- SignalR hors périmètre de ces scénarios ; architecture client-only (Blazor WASM + localStorage).
