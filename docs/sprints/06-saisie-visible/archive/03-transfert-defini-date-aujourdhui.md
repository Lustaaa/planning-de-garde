# Scénario 3 — Transfert défini sans toucher à la date prend aujourd'hui

`@nominal` `@vert` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : IHM / runtime.** Le défaut vit dans le formulaire `DefinirTransfert` : sa date est
> **figée en 2025** (`new(2025,7,21)`), **et** sa logique est dans le **template** (`@code` —
> dette : pas de code-behind). Aucun transfert n'étant projeté dans la grille à ce palier (trou
> par construction), l'observable n'est **pas** une couleur mais la **date portée par la commande
> émise au canal**. Le symptôme PO reste un fait d'usage runtime (« je définis un transfert sans
> toucher la date, il devrait être horodaté à aujourd'hui »). Correction = passage en
> **code-behind** + câblage sur **`IDateTimeProvider.Today`**. **JAMAIS** un bUnit à doublures
> comme preuve d'acceptation (port non injecté / date figée mentiraient au vert).
>
> **Niveau d'acceptation : E2E / runtime** sur l'app réellement câblée (front WASM réel émettant
> vers l'API distante, `IDateTimeProvider` injecté = `2026-06-26`). Comme **aucun observable
> couleur** n'existe pour le transfert, l'acceptation observe la **commande réellement reçue par
> le canal de l'hôte d'API** (date = 26/06/2026) **et** le succès. RED→GREEN piloté par
> `ihm-builder`.

## Acceptation (BDD)

`Should_Horodater_le_transfert_au_26_06_2026_dans_la_commande_recue_par_le_canal_When_un_parent_definit_un_transfert_via_le_front_WASM_sans_modifier_la_date_pre_remplie` — ✅ Passing
(`tests/PlanningDeGarde.Web.Tests/FrontWasmTransfertDateAujourdhuiTests.cs`)

**Test de NIVEAU RUNTIME** sur l'app réellement câblée (front WASM réel émettant vers l'API
distante, `IDateTimeProvider` injecté = **26 juin 2026**) :
- **Given** la date de référence injectée est le **26 juin 2026** ; un parent ouvre le formulaire
  « définir un transfert » du front WASM ; le formulaire (passé en code-behind) **pré-remplit sa
  date depuis le port** ;
- **When** le parent renseigne dépose « Parent A », récupère « Parent B », lieu « école », heure
  16h30, **sans modifier la date pré-remplie**, et valide ;
- **Then** la **commande de transfert reçue par le canal de l'API distante** porte la **date du
  26/06/2026** ; **et** la saisie est **acceptée sans erreur** (réponse de succès, aucun motif
  d'échec affiché).

> Discriminance du rouge : date figée 2025 → la commande reçue porte le 21/07/2025 → rouge.
> Logique restée dans le template (dette non levée) → pas de point d'injection du port.

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (passage `DefinirTransfert` en **code-behind**,
> injection `IDateTimeProvider`, suppression de `new(2025,7,21)`, sélecteurs dépose/récupère
> bindant l'**id stable** par cohérence — cf. cadrage (B), **sans observable couleur**). Boucle
> externe = acceptation runtime ci-dessus. **Aucune table de tests unitaires backend** : la règle
> du transfert (`DefinirTransfertHandler`) est **déjà verte** (sprints 04/05).

## Fichiers à créer / modifier

- **`src/PlanningDeGarde.Web/Components/Pages/DefinirTransfert.razor`** — extraction de la
  logique vers un **`DefinirTransfert.razor.cs`** (lever la dette template) ; injection
  d'`IDateTimeProvider`, pré-remplissage `_form.Date` depuis `Today`, **suppression** de
  `new(2025,7,21)`.
- Sélecteurs dépose/récupère : binder l'**identifiant stable** (cohérence (B), sans observable
  couleur à ce palier).
- Port `IDateTimeProvider` + implémentation + DI (partagés avec Sc.1/Sc.2).

## Design notes

- **Pas d'observable couleur** : aucun transfert n'est projeté dans `GrilleAgendaQuery` à ce
  palier (palier « immédiat & événements / cloche » ultérieur). L'acceptation observe donc la
  **commande reçue par le canal**, pas une case colorée.
- **Dette code-behind levée** : `DefinirTransfert` doit passer en code-behind (convention du
  projet) pour offrir un point d'injection propre du port — signalé en analyse technique.
- **Anti « vert qui ment »** : app réellement câblée ; échoue comme l'utilisateur la voit si la
  date reste figée 2025. Pas de bUnit à doublures comme preuve.
- **Déterminisme** : `IDateTimeProvider` doublé en test ; jamais `DateTime.Today` en dur.
