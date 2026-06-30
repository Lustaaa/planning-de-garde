# Scénario 9 — Bornes de période paramétrables `@limite`

> Suivi : [00-suivi.md](00-suivi.md) · Source : `docs/sprints/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_indiquer_la_periode_aux_bornes_demandees_dans_le_planning_partage_When_un_Parent_affecte_un_responsable_sur_un_intervalle_decale` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_exposer_la_periode_aux_bornes_demandees_When_l_intervalle_affecte_ne_commence_pas_un_lundi | constant → scalar (3) | Contredit toute borne figée (semaine lundi→lundi) : un intervalle arbitraire mercredi→mercredi doit être conservé tel quel | ⚠️ EARLY GREEN |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario9_BornesParametrables.cs` (Domain/Application réutilisés)
**Design notes** :
- Vérifie l'absence d'hypothèse de calendrier fixe : les bornes sont des données, pas une convention codée en dur — contredit un éventuel raccourci pris au Sc.7.
- Test #1 (⚠️ EARLY GREEN, anticipé) : l'agrégat `PeriodeDeGarde` stocke `Debut`/`Fin` comme `DateTime` libres, sans aucune normalisation calendaire (Sc.7 n'avait pris aucun raccourci de semaine figée). Un intervalle mercredi→mercredi est donc conservé tel quel d'office. Aucun changement de code de prod ; test conservé comme caractérisation `@limite` (filet anti-régression contre une future borne codée en dur).
