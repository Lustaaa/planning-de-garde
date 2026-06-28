# Sc.3 — Incarner un identifiant inconnu : refus, identité réelle conservée `@erreur` 🖥️ scénario IHM · driver

← [Retour au suivi](00-sprint14-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app câblée, **référentiel réel**).
> **PAS** un test backend bUnit-à-doublures. Symptôme PO runtime : tenter d'incarner un identifiant
> **absent du référentiel** ne change **rien** (aucun bandeau, identité réelle conservée, menu inchangé).
> Table = inner-loop `SessionPlanning` ; acceptation runtime.

## Acceptation (BDD)

Test **runtime** sur `/planning` (référentiel réel) —
`Should_ResterSousLIdentitéRéelleSansBandeau_When_OnTenteDIncarnerUnIdentifiantAbsentDuRéférentiel` : le
référentiel du foyer ne contient **aucun** acteur d'identifiant `acteur-inexistant` ; le configurateur
tente d'incarner `acteur-inexistant` →
- **aucun bandeau « Vous incarnez »** n'est affiché ;
- il **reste sous son identité réelle** ;
- le **menu d'actions au clic sur une case** est **inchangé** (visible).

Prouvé sur l'app réellement câblée avec un référentiel réel (pas d'acteur fantôme doublé).

## Tests unitaires (ordonnés) — inner-loop `SessionPlanning` (acceptation = runtime)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_ResterSousLIdentitéRéelleSansBandeau_When_OnTenteDIncarnerUnIdentifiantAbsentDuRéférentiel` | garde **conditionnelle** (existence) | driver — Sc.1 « pose réussie » est déjà vert : un `Incarner` **inconditionnel** poserait l'identité effective (et un bandeau pointant un acteur inexistant) → contradiction. La garde est **conditionnelle dès ce 1er `@erreur`** (un refus inconditionnel régresserait Sc.1) : vérifier l'existence au référentiel (READ) **avant** de poser l'identité effective | ⏳ Pending |

## Fichiers à créer

- `src/PlanningDeGarde.Web/State/SessionPlanning.cs` (`Incarner` : refus silencieux si l'id est absent du
  référentiel — l'identité effective reste la réelle)
- `tests/PlanningDeGarde.Web.Tests/FrontWasmIncarnerInconnuRefusTempsReelTests.cs` (acceptation runtime)

## Design notes

- **Refus = no-op silencieux** : pas d'exception, pas de bandeau, identité réelle conservée. La résolution
  s'appuie sur l'**énumération de lecture** du référentiel (READ, identifiant stable — règle 19).
- **Garde conditionnelle dès le 1er `@erreur`** car Sc.1 nominal est déjà vert (le refus inconditionnel
  régresserait la pose réussie).
- **Message éventuel à l'utilisateur** (silencieux vs « acteur introuvable ») **non tranché par la spec**
  (le Gherkin n'exige que « aucun bandeau / identité réelle conservée ») → **remonter au CP si ambigu**.
