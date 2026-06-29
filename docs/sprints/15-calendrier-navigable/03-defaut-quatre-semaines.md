# Scénario 3 — Fenêtre par défaut à l'ouverture = 4 semaines glissantes

`@limite` · **backend** (défaut de fenêtre, `GrilleAgendaQuery`).

[← Retour au suivi](00-sprint15-suivi.md)

À l'ouverture, **sans navigation**, le planning montre **4 semaines glissantes** (28 j / 4 lignes) depuis
la semaine en cours — le **nouveau défaut** (aligne le 5 → 4 semaines). Un seul **driver** (le défaut) ;
le reste caractérise Sc.2.

**Ancrage** : aujourd'hui = mercredi 10/06/2026 → semaine en cours lundi 08/06/2026 ; fenêtre 08/06→05/07,
dernière ligne au lundi 29/06 ; fond première semaine = Alice (ISO 24 paire → index 0).

## Acceptation (BDD) — ✅ GREEN

`Should_Ouvrir_sur_4_lignes_de_28_jours_du_lundi_08_06_au_dimanche_05_07_2026_derniere_ligne_au_lundi_29_06_fond_premiere_semaine_Alice_When_le_hub_planning_est_ouvert_sans_navigation_le_10_06_2026`
— sur `GrilleAgendaQuery.Projeter(dateReference)` **sans vue explicite** : 4 lignes / 28 jours, du 08/06 au
05/07, dernière ligne au 29/06, fond de la 1ʳᵉ semaine = Alice.

## Tests unitaires (boucle interne, TDD)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Projeter_28_jours_en_4_lignes_depuis_le_lundi_de_la_semaine_en_cours_When_le_planning_est_projete_sans_vue_explicite` | constant → calcul (défaut) | L'ancien défaut produit **35 j / 5 lignes** ; force le défaut **4 semaines** sur `Projeter` sans vue. **Driver.** | ✅ GREEN |
| 2 | `Should_Demarrer_la_derniere_ligne_au_lundi_29_06_2026_When_la_fenetre_par_defaut_de_4_semaines_est_projetee_le_10_06_2026` | (aucune) | ⚠️ probablement early green — couvert par #1 (le span de 28 j place mécaniquement la 4ᵉ ligne au 29/06) ; caractérisation `@limite`, pas driver. | ✅ GREEN (caractérisation) |
| 3 | `Should_Afficher_Alice_en_fond_de_la_premiere_semaine_affichee_When_la_fenetre_par_defaut_demarre_au_lundi_08_06_2026` | (aucune) | ⚠️ probablement early green — couvert par la résolution `ResponsableDeFond(date)` par date déjà acquise (ISO 24 paire → Alice) ; caractérisation, pas driver. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/Classes/GrilleAgendaQuery.cs` — défaut de `Projeter` (sans vue) = `VuePlanning.QuatreSemaines` (porté conjointement avec Sc.2).
- `tests/PlanningDeGarde.Tests/Scenario_DefautQuatreSemaines.cs` — projection vide doublée, date de référence injectée.

## Design notes

- **Scénario à 1 seul driver** (#1, le défaut 5 → 4). #2 et #3 sont des **caractérisations** (`@limite` /
  fond) qui ne forcent aucun rouge une fois #1 et le span 4-semaines de Sc.2 verts. Les conserver comme
  filet de non-régression et documentation du défaut, sans gonfler la liste de drivers.
- **Migration des tests structurels existants** : le passage du défaut 35 → 28 jours impacte tout test
  asserant 5 semaines / dernière ligne au jour 34 (`Scenario_GrilleStructure5Semaines`,
  `Scenario_SlotBorneHauteFenetre`). Re-pointage mécanique, **pas** une régression — à signaler à
  `tdd-auto` pour qu'il ne traite pas ces rouges comme des défauts.
