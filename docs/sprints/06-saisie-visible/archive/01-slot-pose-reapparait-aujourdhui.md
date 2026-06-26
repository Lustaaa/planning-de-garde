# Scénario 1 — Slot posé sans toucher aux dates réapparaît à aujourd'hui

`@nominal` `@vert` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : IHM / runtime.** Le défaut vit dans le **formulaire** `PoserSlot` : son
> `_form.Debut/_form.Fin` est **figé en 2025** (`new(2025,7,15,…)`), si bien qu'une pose
> validée **sans corriger la date** tombe hors de la fenêtre affichée et **semble disparaître**.
> Le symptôme PO est un **fait d'usage runtime** (« je pose un slot, je ne touche à rien, il
> n'apparaît pas »). La correction est l'injection d'un port **`IDateTimeProvider`** dans le
> `.razor.cs`, pré-remplissant la date sur **`Today`**. **JAMAIS** planifié comme un bUnit
> composant à doublures : un bUnit force l'interactivité et stub le transport — il **mentirait
> au vert** si le port n'était pas injecté, ou s'il restait `DateTime.Today` en dur (non
> déterministe).
>
> **Niveau d'acceptation : E2E / runtime** sur l'**app réellement câblée** (front WASM réel +
> hôte d'API détaché démarré, DI réelle dont `IDateTimeProvider` fixé à `2026-06-26`). Le détail
> RED→GREEN (`.razor.cs`, port, enregistrement DI) est piloté par `ihm-builder`.

## Acceptation (BDD)

`Should_Faire_reapparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_26_06_2026_When_un_parent_pose_un_slot_via_le_front_WASM_sans_modifier_les_dates_pre_remplies` — ✅ Passing
(`tests/PlanningDeGarde.Web.Tests/FrontWasmSlotDateAujourdhuiTests.cs`)

**Test de NIVEAU RUNTIME** sur l'app réellement câblée (front WASM réel émettant vers l'API
distante, `IDateTimeProvider` doublé/injecté = **26 juin 2026**) :
- **Given** la date de référence injectée (`IDateTimeProvider.Today`) est le **26 juin 2026** ;
  un parent ouvre le formulaire « poser un slot » du front WASM ; le formulaire **pré-remplit
  ses dates depuis le port** (et non une date figée 2025) ; aucun slot pour le 26/06/2026 ;
- **When** le parent choisit le lieu « école » de 08h30 à 16h30 et valide **sans modifier
  aucune date pré-remplie**, puis revient au planning ;
- **Then** dans la grille projetée à la semaine du lundi 22/06/2026, la **case du 26/06/2026**
  porte le slot « école » de **08h30 à 16h30** — l'écriture ayant réellement transité par le
  canal jusqu'au store relu par `GrilleAgendaQuery` (pas une grille statique, pas un accusé).

> Discriminance du rouge : si le formulaire porte encore la date figée 2025, la pose tombe hors
> fenêtre → la case du 26/06 reste vide → rouge. Un bUnit à doublures (date stub) ne verrait pas
> ce câblage.

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (introduction du port `IDateTimeProvider`, câblage
> `PoserSlot.razor.cs`, enregistrement DI, suppression de la date figée). L'acceptation runtime
> ci-dessus est la **boucle externe**. **Aucune table de tests unitaires backend** : la règle de
> pose et la projection sont **déjà vertes** (sprints 01/03) ; ce scénario prouve le **câblage
> runtime « date par défaut = aujourd'hui »** du formulaire.

## Fichiers à créer / modifier

- **`src/PlanningDeGarde.Application/IDateTimeProvider.cs`** — port exposant la date du jour
  (`Today` / `Aujourdhui`), doublé en test.
- **Implémentation système** en `src/PlanningDeGarde.Infrastructure/` + **enregistrement DI**
  côté front WASM (`Program.cs`).
- **`src/PlanningDeGarde.Web/Components/Pages/PoserSlot.razor.cs`** — injection du port,
  pré-remplissage `_form.Debut/_form.Fin` depuis `Today`, **suppression** de `new(2025,7,15,…)`.

## Design notes

- **Anti « vert qui ment »** : l'acceptation doit échouer **comme l'utilisateur la voit** si le
  port n'est pas injecté ou si la date reste figée 2025. App réellement câblée (DI réelle),
  **pas** bUnit composant à doublures.
- **Déterminisme** : `IDateTimeProvider` est **doublé** en test (date fixée `2026-06-26`),
  symétrie avec `Projeter(dateReference)` côté lecture — **jamais** `DateTime.Today` en dur.
- **Heure conservée** : seule la **date** est par défaut « aujourd'hui » ; l'heure saisie
  (08h30→16h30) reste celle du formulaire. La pose porte donc le 26/06/2026 08h30→16h30.
- **Aucun changement de règle** : pose (`PoserSlotHandler`) et projection (`GrilleAgendaQuery`)
  inchangées ; seul le **pré-remplissage de la vue** change.
