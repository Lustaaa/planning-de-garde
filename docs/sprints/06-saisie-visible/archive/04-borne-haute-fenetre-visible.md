# Scénario 4 — Saisie à la borne haute de la fenêtre reste visible

`@limite`

[← Retour au suivi](00-sprint06-suivi.md)

> **Axe : backend (caractérisation).** Routé vers `tdd-auto`. Le comportement vit dans la
> projection `GrilleAgendaQuery` : un slot au **dernier jour de la fenêtre de 35 jours** est
> rendu, un slot **au-delà** est exclu. Cet invariant est **déjà vert** (sprint 03 :
> `Scenario_GrilleStructure5Semaines` pour les 35 jours datés depuis le lundi ;
> `Scenario_SlotHorsFenetreExclu` pour l'exclusion hors fenêtre par date complète). Ce scénario
> **n'introduit aucun nouveau driver** : il **caractérise la borne haute** précise (26/07/2026,
> 35ᵉ jour depuis le lundi 22/06) comme filet de non-régression du palier saisie visible.
>
> **Niveau d'acceptation : test unitaire** (projection sans Blazor, date de référence injectée,
> fakes peuplés via l'agrégat `SlotDeLocalisation`).

## Acceptation (BDD)

`Should_Faire_apparaitre_le_slot_domicile_A_dans_la_case_du_26_07_2026_et_exclure_un_slot_du_27_07_2026_When_la_grille_est_projetee_a_la_semaine_du_lundi_22_06_2026` — ✅ GREEN

- **Given** la date de référence est le **26 juin 2026** ; la grille affiche les **35 jours
  datés** depuis le **lundi 22/06/2026** ; un slot « domicile A » est posé au **26/07/2026**
  (dernier jour de la fenêtre) et un autre slot **au 27/07/2026** (premier jour hors fenêtre) ;
- **When** la grille est projetée à la date de référence ;
- **Then** la **case du 26/07/2026** porte le slot « domicile A » ; **et** le slot du 27/07/2026
  ne figure dans **aucune** case de la fenêtre.

## Tests

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Faire_apparaitre_le_slot_domicile_A_dans_la_case_du_26_07_2026_When_il_est_pose_au_dernier_jour_de_la_fenetre_de_35_jours` | tableau peuplé → case du 35ᵉ jour | ⚠️ probablement early green — couvert par `Scenario_GrilleStructure5Semaines` + `Scenario_SlotDansCaseDuJour` (caractérisation, pas driver) : la fenêtre fait **déjà** 35 jours (`Enumerable.Range(0,35)`) et le rattachement se fait **déjà** par `DateOnly.FromDateTime(slot.Debut)` ; la case du 26/07 existe et accueille le slot sans nouveau code. | ✅ GREEN (caractérisation) |
| 2 | `Should_Exclure_le_slot_du_27_07_2026_de_toutes_les_cases_When_il_est_pose_au_premier_jour_hors_fenetre` | présence + absence couplées | ⚠️ probablement early green — couvert par `Scenario_SlotHorsFenetreExclu` (caractérisation, pas driver) : aucune case n'est datée au-delà du 26/07 (fenêtre bornée à 35), donc le slot du 27/07 n'a **aucune case d'accueil**. Couple présence (26/07) + absence (27/07) pour qu'une grille vide ne passe pas. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- **`tests/PlanningDeGarde.Tests/Scenario_SlotBorneHauteFenetre.cs`** (ou nom équivalent) —
  tests xUnit de projection. Réutilise les fakes existants (`FakeSlotRepository`,
  `FakePeriodeRepository`, `FakePaletteCouleurs`) et l'agrégat `SlotDeLocalisation`.

## Design notes

- **Caractérisation, pas driver** : `tdd-auto` doit s'attendre à **GREEN dès le 1er passage**
  (`✅ GREEN (caractérisation)`) — l'invariant fenêtre 35j + exclusion hors fenêtre est déjà
  codé. Ce scénario est un **filet de non-régression** de la borne haute, pas une règle neuve.
- **Anti early-green trompeur** : coupler présence (26/07 rendu) **et** absence (27/07 exclu)
  dans la même grille évite qu'une grille vide ou une fenêtre élastique passe.
- **Date de référence injectée** : projeter à `26/06/2026` (lundi de la semaine = 22/06), jamais
  `DateTime.Now`. Borne haute = `22/06 + 34 jours = 26/07/2026`.
- **Pas d'IHM ici** : aucun `.razor` ni câblage — projection pure (l'aspect runtime « saisie
  visible à aujourd'hui » est porté par Sc.1).
