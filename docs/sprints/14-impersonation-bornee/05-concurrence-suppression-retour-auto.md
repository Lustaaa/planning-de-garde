# Sc.5 — Concurrence : l'acteur incarné est supprimé → retour auto à l'identité réelle `@limite` 🖥️ scénario IHM · driver · runtime / G3

← [Retour au suivi](00-sprint14-suivi.md)

> **Routé vers `ihm-builder`** — **acceptation runtime / G3 uniquement** (D2) : ce scénario touche la
> **diffusion temps réel SignalR** (propagation de la suppression d'acteur vers l'écran qui incarne),
> **validé sur l'app réellement câblée**, **pas** en filet de régression automatisé instable (flakes
> SignalR P2). Table = inner-loop `SessionPlanning` ; **preuve = runtime**.

## Acceptation (BDD)

Test **runtime / G3** sur l'app câblée (front WASM + API distante + SignalR + Mongo réel) —
`Should_RevenirAutomatiquementÀLIdentitéRéelle_When_LActeurIncarnéEstSuppriméEnTempsRéel` : le
configurateur incarne **Nina la nounou** (bandeau « Vous incarnez Nina la nounou » affiché) ; **un autre
écran supprime** Nina du foyer et la suppression **se propage en temps réel** →
- le configurateur **revient automatiquement** à son identité réelle ;
- le **bandeau « Vous incarnez Nina la nounou » n'est plus affiché** ;
- **aucun nom fantôme** de « Nina la nounou » ne subsiste dans la vue.

Prouvé sur l'app réellement câblée avec diffusion SignalR réelle (convention anti-flake `*TempsReel*`,
stable ≥ 3×). **Pas** bUnit seul.

## Tests unitaires (ordonnés) — inner-loop `SessionPlanning` (acceptation = runtime / G3)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_RevenirAutomatiquementÀLIdentitéRéelle_When_LActeurIncarnéDisparaîtDuRéférentiel` | conditionnel (détection d'orphelin) | driver — après une suppression concurrente, l'identité effective pointe un acteur **absent du référentiel** (acteur fantôme) ; rien ne le détecte aujourd'hui → force le repli « effective absente du référentiel relu → retour identité réelle » (projection des règles 6/18/19, D2) | ⏳ Pending |

## Fichiers à créer

- `src/PlanningDeGarde.Web/State/SessionPlanning.cs` (repli automatique : si l'identité effective n'est
  plus dans le référentiel, retour à l'identité réelle)
- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor.cs` (sur réception de l'événement
  SignalR de mise à jour, ré-évaluer l'existence de l'acteur incarné et déclencher le repli)
- `tests/PlanningDeGarde.Web.Tests/FrontWasmIncarnationSuppressionConcurrenteTempsReelTests.cs`
  (acceptation runtime / G3, store réel)

## Design notes

- **D2 — extension cohérente de la neutralisation par repli (règle 6).** Le « neutre » de l'incarnation,
  c'est l'**identité réelle** du configurateur (état par défaut hors incarnation) : la référence orpheline
  cesse de primer → repli. Aucune règle neuve.
- **Invariant « sans nom fantôme » (règles 18/19)** : le bandeau pointant l'acteur supprimé disparaît
  aussitôt, aucun nom fantôme ne subsiste.
- **Acceptation runtime / G3, pas de filet flaky** (D2) : la propagation SignalR se prouve sur l'app
  câblée ; convention `*TempsReel*` + garde déterministe `WaitForState` (standard des tests frères).
- **Déclencheur du repli** (re-vérifier sur l'événement SignalR de mise à jour vs polling) **non tranché
  par la spec** → **remonter au CP si ambigu**.
