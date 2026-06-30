# Scénario 2 — Période affectée sans toucher aux dates tombe dans la fenêtre

`@nominal` `@vert` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : IHM / runtime.** Le défaut vit dans le formulaire `AffecterPeriode` : ses dates sont
> **figées en 2025** (`new(2025,7,14)` / `new(2025,7,21)`), si bien qu'une affectation validée
> **sans corriger les dates** tombe hors de la fenêtre affichée. Le symptôme PO est un **fait
> d'usage runtime** (« j'affecte une période, je ne touche pas aux dates, la grille ne se colore
> pas »). La correction est le câblage du `.razor.cs` sur **`IDateTimeProvider.Today`**. **JAMAIS**
> planifié comme un bUnit composant à doublures (il mentirait au vert sur le port non injecté ou
> une date figée).
>
> **Niveau d'acceptation : E2E / runtime** sur l'app réellement câblée (front WASM réel + API
> distante, `IDateTimeProvider` fixé à `2026-06-26`). RED→GREEN piloté par `ihm-builder`.

## Acceptation (BDD)

`Should_Colorer_la_case_du_26_06_2026_pour_la_periode_affectee_When_un_parent_affecte_une_periode_via_le_front_WASM_sans_modifier_les_dates_pre_remplies` — ✅ Passing
(`tests/PlanningDeGarde.Web.Tests/FrontWasmPeriodeDateAujourdhuiTests.cs`)

**Test de NIVEAU RUNTIME** sur l'app réellement câblée (front WASM réel émettant vers l'API
distante, `IDateTimeProvider` injecté = **26 juin 2026**) :
- **Given** la date de référence injectée est le **26 juin 2026** ; un parent ouvre le formulaire
  « affecter une période » du front WASM ; le formulaire **pré-remplit ses dates depuis le port** ;
  aucune période sur la fenêtre ;
- **When** le parent choisit le responsable « Parent A » et valide **sans modifier aucune date
  pré-remplie**, puis revient au planning ;
- **Then** dans la grille projetée à la semaine du lundi 22/06/2026, la **case du 26/06/2026 est
  colorée** pour la période affectée (couleur du responsable, **non** neutre) — l'affectation
  ayant réellement transité par le canal jusqu'au store relu par `GrilleAgendaQuery`.

> Discriminance du rouge : dates figées 2025 → la période tombe hors fenêtre → la case du 26/06
> reste neutre → rouge. (La **couleur du parent** par identifiant stable est couverte en propre
> par le Sc.6 ; ici l'observable est que la case **est colorée pour la période**, c.-à-d. tombe
> bien dans la fenêtre.)

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (câblage `AffecterPeriode.razor.cs` sur
> `IDateTimeProvider`, suppression des dates figées 2025). Boucle externe = acceptation runtime
> ci-dessus. **Aucune table de tests unitaires backend** : affectation et projection couleur sont
> **déjà vertes** (sprints 01/03) ; ce scénario prouve le **câblage « dates par défaut =
> aujourd'hui »**.

## Fichiers à créer / modifier

- **`src/PlanningDeGarde.Web/Components/Pages/AffecterPeriode.razor.cs`** — injection
  d'`IDateTimeProvider`, pré-remplissage `_form.Debut/_form.Fin` depuis `Today`, **suppression**
  de `new(2025,7,14)` / `new(2025,7,21)`.
- Port `IDateTimeProvider` + implémentation + DI (partagés avec Sc.1/Sc.3).

## Design notes

- **Anti « vert qui ment »** : app réellement câblée ; échoue si le port n'est pas injecté ou si
  la date reste figée 2025. Pas de bUnit à doublures comme preuve d'acceptation.
- **Largeur de période** : pré-remplir un **début et une fin cohérents** autour d'aujourd'hui
  (au minimum un intervalle couvrant le 26/06) pour que la case du jour tombe dans l'affectation.
  Choix d'intervalle laissé à `ihm-builder`, contraint par l'observable (case du 26/06 colorée).
- **Couleur = Sc.6** : l'identifiant stable (`parent-a`→bleu) est traité au Sc.6 ; ici on
  observe seulement que la case n'est **plus neutre** (la période tombe dans la fenêtre).
- **Déterminisme** : `IDateTimeProvider` doublé en test ; jamais `DateTime.Today` en dur.
