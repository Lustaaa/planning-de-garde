# Scénario 6 — Une période à cheval sur la borne de fin n'est colorée que sur ses jours internes `@limite`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Intersection partielle d'une période avec la
> fenêtre (coloration des seuls jours internes) par `GrilleAgendaQuery`.

> **Acceptation (BDD)** —
> `Should_Colorer_du_20_07_au_26_07_2026_la_periode_de_Parent_B_sans_aucune_case_au_dela_du_dimanche_26_07_et_colorer_le_22_06_de_Parent_A_When_une_periode_deborde_la_borne_de_fin_de_la_grille`
> Test unitaire de projection : set Parent A = bleu, Parent B = orange ; période
> Parent B du 20/07 au 02/08/2026 (déborde la fenêtre) + période Parent A le
> 22/06/2026 ; date de réf 24/06/2026 → cases du 20/07 et du 26/07 portent orange
> (Parent B), case du 22/06 porte bleu (Parent A), et **la dernière case reste le
> dimanche 26/07** (aucune case au-delà). Couple coloration interne + borne stricte.

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Colorer_de_Parent_B_les_cases_du_20_07_au_26_07_2026_sans_deborder_au_dela_de_la_borne_de_fin_When_une_periode_de_Parent_B_court_du_20_07_au_02_08_2026` | intersection partielle (clamp aux jours internes) | Driver : une implémentation qui projette la période sur **tous** ses jours (y compris hors fenêtre) lèverait une erreur d'indexation ou ne trouverait pas de case ; une qui s'arrête mal échoue ; force l'intersection `[période] ∩ [fenêtre]`. Couplé : la case du 26/07 (dernière, interne, débordée par la période) **est** colorée, et il n'existe **aucune** case au-delà. | ⏳ Pending |
| 2 | `Should_Maintenir_la_couleur_de_Parent_A_sur_le_22_06_2026_When_une_seconde_periode_couvre_ce_jour_en_plus_de_la_periode_de_Parent_B` | coexistence de deux périodes (chacune sur ses jours) | Driver : deux périodes distinctes colorent des plages disjointes ; une implémentation qui n'en garderait qu'une (ou colorerait toute la grille de la dernière) échoue ; force le mapping par jour de la période **couvrant** ce jour. | ⏳ Pending |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — intersection période ∩
  fenêtre datée (ne colorer que les jours **présents** dans la fenêtre).
- `tests/PlanningDeGarde.Tests/Scenario_PeriodeACheval.cs`.

## Design notes

- **Anticipation early-green sur la borne** : la **borne de fin** « aucune case au-delà
  du 26/07 » est déjà garantie par le calcul de fenêtre du **Sc.1** (la grille a
  toujours exactement 35 cases). Donc l'assertion « dernière case = 26/07 » est une
  **caractérisation** (filet de non-régression), pas le driver de ce scénario. Le
  **driver réel** est l'**intersection partielle** : ne colorer que les jours
  internes d'une période débordante sans planter (test #1). Marquer l'assertion de
  borne comme caractérisation si elle passe d'emblée.
- **Anti early-green imposé** : le couplage présence (cases 20/07–26/07 orange + 22/06
  bleu) + borne stricte fait échouer une grille vide / mal bornée.
- La période Parent B **commence** dans la fenêtre (20/07 interne) et **finit** au-delà
  (02/08 hors) : c'est le débordement par la **borne de fin**. Le test #1 vérifie que
  la projection clampe sans exception ni case fantôme.
- Doubler uniquement les ports. Pas de Blazor.
