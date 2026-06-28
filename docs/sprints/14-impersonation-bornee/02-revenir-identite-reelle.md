# Sc.2 — Revenir à l'identité réelle : bandeau retiré, état restauré `@nominal` 🖥️ scénario IHM · driver

← [Retour au suivi](00-sprint14-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app câblée, DI réelle). **PAS**
> un test backend bUnit-à-doublures. Symptôme PO runtime : après incarnation, le retour à l'identité
> réelle **retire le bandeau** et **restaure** le menu d'écriture de l'utilisateur principal. Table =
> inner-loop `SessionPlanning` ; acceptation runtime.

## Acceptation (BDD)

Test **runtime** sur `/planning` —
`Should_RetirerLeBandeauEtRestaurerLeMenuRéel_When_LeConfigurateurRevientÀSonIdentitéRéelle` : le foyer
déclare **Bruno** (Parent) ; le configurateur incarne Bruno (bandeau « Vous incarnez Bruno » affiché,
menu visible) puis **revient à son identité réelle** →
- le **bandeau « Vous incarnez Bruno » n'est plus affiché** ;
- le **menu d'actions au clic sur une case** est de nouveau celui de l'identité réelle (**visible**).

Prouvé sur l'app réellement câblée (render mode interactif, DI réelle).

## Tests unitaires (ordonnés) — inner-loop `SessionPlanning` (acceptation = runtime)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_RestaurerLIdentitéRéelleEtRetirerLeBandeau_When_OnRevientAprèsAvoirIncarnéUnActeur` | variable → constant (geste inverse) | driver — après Sc.1 (incarné), aucun chemin ne restaure l'identité réelle ; `RevenirIdentiteReelle` est neuf (Sc.1 ne l'implémente pas) → ramène l'effective à la réelle, retire le bandeau, ré-autorise le menu (assertions cohésives du même geste) | ⏳ Pending |

## Fichiers à créer

- `src/PlanningDeGarde.Web/State/SessionPlanning.cs` (`RevenirIdentiteReelle`)
- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) : commande de retour
  à l'identité réelle (bouton/affordance du bandeau)
- `tests/PlanningDeGarde.Web.Tests/FrontWasmRevenirIdentiteReelleTempsReelTests.cs` (acceptation runtime)

## Design notes

- **Geste inverse de Sc.1**, méthode distincte (`RevenirIdentiteReelle`) → **driver réel**, pas
  early-green : Sc.1 pose `Incarner`, pas la restauration.
- **Restauration = effective remise à la réelle** : `EstParent` redérive automatiquement (Parent
  configurateur), le bandeau se vide. Aucun recalcul métier.
- **Affordance du retour** (bouton dans le bandeau, libellé) **non tranchée par la spec** → **remonter au
  CP si ambigu**.
