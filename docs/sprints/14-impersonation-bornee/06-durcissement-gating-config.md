# Sc.6 — Durcissement gating config : un « Autre » incarné masque toutes les écritures `@erreur` 🖥️ scénario IHM · driver · runtime / G3 · **CUTTABLE**

← [Retour au suivi](00-sprint14-suivi.md)

> **Routé vers `ihm-builder`** — **acceptation runtime / G3** (gating effectif d'un écran réel, DI réelle).
> **PAS** un test backend bUnit-à-doublures. Symptôme PO runtime : en incarnant un acteur **« Autre »**,
> l'écran de configuration ne propose **aucune** écriture. **CUTTABLE** (D1) : embarqué SI ≤ ~2h une fois
> l'identité effective posée (Sc.1) ; sinon **coupé et re-séquencé sans toucher au cœur** Sc.1→Sc.5.

## Acceptation (BDD)

Test **runtime / G3** sur `/configuration` (app câblée, contexte rôle réel) —
`Should_MasquerToutesLesÉcrituresDeLaConfiguration_When_OnIncarneUnActeurDeTypeAutre` : le foyer déclare
**Nina la nounou** (Autre) ; le configurateur incarne Nina et ouvre l'écran de configuration →
- l'**ajout d'un acteur** n'est pas proposé ;
- l'**édition d'un acteur** n'est pas proposée ;
- l'**édition du cycle de fond** n'est pas proposée ;
- le **bouton de suppression d'un acteur** n'est pas proposé.

Contrôle positif : sous l'identité réelle (Parent), toutes ces écritures restent proposées (anti faux
vert). Prouvé sur l'app réellement câblée (gating règle 9 sur l'identité **effective**) — pas bUnit seul.

## Tests unitaires (ordonnés) — inner-loop (acceptation = runtime / G3)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_MasquerToutesLesÉcrituresDeLaConfiguration_When_OnIncarneUnActeurDeTypeAutre` | inconditionnel → conditionnel (étendre le garde) | driver — aujourd'hui **seul le bouton supprimer** est gaté `@if EstParent` (s13) ; **ajout / édition d'acteur / édition du cycle** restent ouverts → un « Autre » incarné les verrait encore. Le bouton supprimer, lui, **suit l'effective** automatiquement (⚠️ early-green partiel). Le rouge propre force l'extension du garde à **toutes** les écritures config | ⏳ Pending |

## Fichiers à créer

- `src/PlanningDeGarde.Web/Components/Pages/ConfigurationFoyer.razor` : envelopper sous `@if Session.EstParent`
  (lu sur l'identité **effective**) les formulaires d'**ajout**, d'**édition d'acteur**, d'**édition du
  cycle de fond** — le bouton supprimer l'est déjà (s13)
- `tests/PlanningDeGarde.Web.Tests/FrontWasmConfigGatingAutreIncarneTempsReelTests.cs` (acceptation
  runtime / G3, contexte rôle réel)

## Design notes

- **Angle mort Sc.7 s13 (D1).** Le gating règle 9 n'était posé que sur le bouton supprimer et la grille ;
  ajout / édition / cycle de config restaient ouverts à un non-Parent. L'impersonation rend cette
  incohérence **testable** (incarner un Autre tout en laissant l'écran config ouvert en écriture).
- **⚠️ Cascade early-green partielle.** Une fois `EstParent` dérivé de l'effective (Sc.1), le **bouton
  supprimer** (déjà gaté) suit l'incarnation **sans code neuf** ; le **driver réel** est l'extension du
  garde aux **autres** écritures config. `ihb-builder` doit batcher le bouton supprimer comme
  caractérisation et concentrer le rouge sur ajout/édition/cycle.
- **CUTTABLE (D1)** : si l'extension déborde ~2h une fois l'identité effective posée, **couper** et
  re-séquencer la révision de règle 9 — **sans toucher au cœur** Sc.1→Sc.5. **Remonter au CP** avant de
  couper.
- **Périmètre exact des écritures à gater** (ajout, édition, cycle, supprimer — la liste du Gherkin) est
  tranché ; tout écran d'écriture config **non listé** qui apparaîtrait → **remonter au CP si ambigu**.
