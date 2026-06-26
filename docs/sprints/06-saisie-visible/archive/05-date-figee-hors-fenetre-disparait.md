# Scénario 5 — Date figée hors fenêtre fait disparaître la saisie

`@erreur`

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : backend (caractérisation / diagnostic).** Routé vers `tdd-auto`. Ce scénario
> **documente la cause** du faux bug « les saisies n'apparaissent pas » : une date par défaut
> **figée en 2025** fait tomber la saisie **hors de la fenêtre** affichée (qui démarre au lundi
> de la semaine du 26/06/2026), donc invisible **alors qu'elle est bien enregistrée**. C'est la
> **justification métier du défaut (A)** corrigé côté IHM par les Sc.1/Sc.2/Sc.3. L'invariant
> d'exclusion hors fenêtre est **déjà vert** (`Scenario_SlotHorsFenetreExclu`) : aucun nouveau
> driver — ce test est un **diagnostic** caractérisant le symptôme.
>
> **Niveau d'acceptation : test unitaire** (projection sans Blazor, date de référence injectée).

## Acceptation (BDD)

`Should_N_inclure_dans_aucune_case_de_la_fenetre_le_slot_pose_au_15_07_2025_While_il_reste_enregistre_When_la_grille_du_26_06_2026_est_projetee` — ✅ GREEN

- **Given** la date de référence est le **26 juin 2026** ; un slot est posé au **15 juillet 2025**
  (date par défaut figée, non corrigée) ;
- **When** la grille du 26/06/2026 est projetée ;
- **Then** **aucune case** de la fenêtre ne porte ce slot ; **et** la saisie **semble avoir
  disparu** alors qu'elle est **enregistrée hors fenêtre** (le store contient bien le slot).

## Tests

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_N_afficher_le_slot_du_15_07_2025_dans_aucune_case_While_il_demeure_dans_le_store_When_la_grille_du_26_06_2026_est_projetee` | présence dans le store + absence dans la grille | ⚠️ probablement early green — couvert par `Scenario_SlotHorsFenetreExclu` (caractérisation, pas driver) : la fenêtre démarre au lundi 22/06/2026 et fait 35 jours ; un slot au 15/07/2025 n'a **aucune case d'accueil**. La pointe **discriminante** ici : asserter que le slot est **bien présent dans le store** (le repository le rend) mais **absent de toute case** — montre que c'est un défaut de **date figée**, pas une non-persistance. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- **`tests/PlanningDeGarde.Tests/Scenario_DateFigeeHorsFenetre.cs`** (ou nom équivalent) — test
  xUnit de projection. Réutilise `FakeSlotRepository`, `FakePeriodeRepository`,
  `FakePaletteCouleurs`, `SlotDeLocalisation`.

## Design notes

- **Caractérisation / diagnostic** : `tdd-auto` doit s'attendre à **GREEN dès le 1er passage**
  (`✅ GREEN (caractérisation)`). Ce scénario **ne corrige rien** : il **prouve la cause** du
  symptôme PO. La **correction** vit dans les formulaires (Sc.1/Sc.2/Sc.3, `IDateTimeProvider`).
- **Discriminance « enregistré ≠ affiché »** : asserter explicitement que le slot **existe dans
  le store** (via `_slots.AllSnapshots()` ou équivalent du fake) tout en étant **hors de toute
  case** — c'est le cœur du « semble disparu mais enregistré ».
- **Date de référence injectée** : `26/06/2026` (fenêtre lundi 22/06 → 26/07/2026) ; le 15/07/2025
  est nettement en amont. Jamais `DateTime.Now`.
- **Pas d'IHM ici** : projection pure ; le câblage runtime correctif est Sc.1.
