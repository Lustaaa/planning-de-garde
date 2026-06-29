# Sc.1 — Incarner un acteur déclaré : bandeau + vue selon son rôle `@nominal` 🖥️ scénario IHM · driver

← [Retour au suivi](00-sprint14-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app réellement câblée, DI
> réelle, **référentiel d'acteurs réel** avec **type seed surfacé read-only** — D3). **PAS** un test
> backend bUnit-à-doublures. Le symptôme PO est un **fait d'usage runtime** : le bandeau « Vous incarnez
> X » s'affiche et le **menu d'écriture au clic sur une case** est visible (Parent/Admin incarné) ou
> masqué (Autre incarné). La table ci-dessous est l'**inner-loop `SessionPlanning`** (POCO de session,
> logique pure) que `ihm-builder` peut dérouler en boucle rapide — l'**acceptation reste runtime**.

## Acceptation (BDD)

Test **runtime** sur `/planning` (front WASM câblé à l'API distante réelle, référentiel réel, type
surfacé read-only depuis le seed) — `Should_AfficherLeBandeauEtAdapterLeMenuClicCase_When_LeConfigurateurIncarneUnActeurDéclaré` :
le foyer déclare **Bruno** (Parent), **Nina la nounou** (Autre), **Carla** (Admin) ; depuis le
**sélecteur d'incarnation**, le configurateur incarne **`<acteur>`** →
- un **bandeau « Vous incarnez `<acteur>` »** s'affiche ;
- le **menu d'actions au clic sur une case** est **`<menu>`** : `Bruno` → visible · `Nina la nounou` →
  masqué · `Carla` → visible.

Prouvé sur l'app réellement câblée (render mode interactif, DI réelle, référentiel réel) — pas par
bUnit forçant l'interactivité ni par un type d'acteur doublé.

## Tests unitaires (ordonnés) — inner-loop `SessionPlanning` (acceptation = runtime)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_PrésenterLeConfigurateurSousSonIdentitéRéelleParent_When_AucuneIncarnationNEstActive` | `{} → nil` / état constant | ⚠️ état initial (pas de contradiction réelle) — hors incarnation, l'identité effective EST l'identité réelle (Parent, actions autorisées) ; caractérisation de départ | ✅ Green |
| 2 | `Should_AfficherLeBandeauEtRefléterLActeurIncarné_When_OnIncarneUnActeurDéclaréDeTypeParent` | constant → variable | driver — l'état initial ne porte aucune incarnation ; incarner Bruno force l'identité effective ≠ réelle + le libellé du bandeau « Vous incarnez Bruno » | ✅ Green |
| 3 | `Should_MasquerLesActionsDÉcriture_When_OnIncarneUnActeurDeTypeAutre` | inconditionnel → conditionnel | driver (règle 8) — après #2 un `Incarner` naïf « garde toujours le droit d'écrire » est vrai ; incarner Nina (Autre) le contredit → force `EstParent` à dériver du **type effectif** | ✅ Green |
| 4 | `Should_ConserverLesActionsDÉcriture_When_OnIncarneUnActeurDeTypeAdmin` | (même branche) | ⚠️ probablement early green — couvert par #2 (type ∈ {Parent, Admin} → autorisé) : caractérisation, pas driver | ✅ Green |

> **Réalisé.** Inner-loop dans `tests/PlanningDeGarde.Web.Tests/SessionPlanningIncarnationTests.cs`
> (7 faits : les 4 ci-dessus + refus silencieux, retour identité réelle, composition rôle démo Invité —
> socles Sc.2/Sc.3). **Acceptation runtime ✅ GREEN** :
> `tests/PlanningDeGarde.Web.Tests/FrontWasmIncarnerRefleteRoleTempsReelTests.cs` (Theory Bruno/Nina la
> nounou/parent-c Admin sur l'app réellement câblée). RED d'abord constaté (sélecteur/bandeau absents),
> puis vert. NB : l'exemple Admin « Carla » du Gherkin est porté par l'acteur Admin déjà seedé `parent-c`
> (Marie-Hélène) — « Carla » reste réservé aux tests d'AJOUT (nom frais), non seedé ; comportement Admin
> (menu visible) identique.

## Fichiers à créer

- `src/PlanningDeGarde.Web/State/SessionPlanning.cs` (extension : identité réelle vs effective,
  `Incarner` / `RevenirIdentiteReelle`, `EstParent` dérivé de l'effective, libellé du bandeau)
- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) : **bandeau
  d'incarnation** + **sélecteur d'incarnation** alimenté par le référentiel d'acteurs (id stable + type)
- Extension **read-only** du contrat de lecture pour surfacer le **type** d'acteur (D3) :
  `src/PlanningDeGarde.Application/Interfaces/IEnumerationActeursFoyer.cs` (ou DTO de projection) +
  `src/PlanningDeGarde.Api/Controllers/CanalLecture.cs` (`ActeurFoyerVue` + type) +
  réalisation seed côté `AdapterDroite` (type depuis la déclaration seed ; ajout en session → « Parent »)
- `tests/PlanningDeGarde.Web.Tests/FrontWasmIncarnerRefleteRoleTempsReelTests.cs` (acceptation runtime)

## Design notes

- **D3 — source du type read-only.** Le type (Admin/Parent/Autre) est **lu**, jamais saisi ni persisté :
  il vient de la **déclaration seed** du foyer, surfacé via l'**énumération de lecture** ; un acteur
  **ajouté en session** est typé **« Parent »** par défaut. Aucune écriture, aucune persistance neuve.
  La résolution se fait sur l'**identifiant stable** (`acteur-…`), jamais le libellé (règles 5/19).
- **`EstParent` dérive de l'identité EFFECTIVE** : vrai si le type effectif ∈ {Parent, Admin}, faux si
  Autre. Hors incarnation, l'effective = la réelle (Parent configurateur).
- **⚠️ Cascade early-green (câblage existant).** Le gating de la grille (`@if Session.EstParent`, garde
  `OuvrirMenu`, classe `grille-jour-cliquable`) **lit déjà `EstParent`** : une fois ce dernier dérivé de
  l'effective, le **menu masqué pour un Autre incarné est early-green** côté grille. Le **neuf** ici est
  le **bandeau** + le **sélecteur d'incarnation** (et la dérivation `EstParent` sur le type). `ihm-builder`
  doit traiter le gating grille comme caractérisation, pas comme un rouge propre.
- **Choix d'ergonomie du sélecteur d'incarnation** (remplace / complète le dropdown rôle démo
  Parent/Invité actuel ; emplacement, libellé, regroupement avec le bandeau) **non tranché par la spec**
  → **remonter au CP si ambigu**.
