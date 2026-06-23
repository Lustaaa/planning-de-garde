# Scénario 7 — Un Parent affecte la responsabilité d'une période de garde `@nominal`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_indiquer_le_responsable_de_la_periode_dans_le_planning_partage_independamment_du_lieu_de_l_enfant_When_un_Parent_affecte_un_responsable_sur_un_intervalle` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_l_affectation_de_la_periode_When_un_Parent_rend_un_responsable_responsable_sur_un_intervalle_valide | nil → constant (2) | Baseline du nouvel agrégat période : pose un succès toujours-réussir d'affectation | ⏳ Pending |
| 2 | Should_exposer_le_responsable_et_l_intervalle_de_la_periode_affectee_When_un_Parent_a_affecte_la_responsabilite | constant → scalar (3) | Contredit le succès vide : la période reflète responsable / début / fin fournis (snapshot) | ⏳ Pending |
| 3 | Should_indiquer_la_periode_dans_le_planning_partage_du_foyer_When_la_periode_a_ete_affectee | constant → scalar (3) | Contredit l'agrégat isolé : la période persistée apparaît dans le planning (fake repository) | ⏳ Pending |
| 4 | Should_conserver_le_responsable_de_la_periode_When_l_enfant_change_de_lieu_pendant_l_intervalle | conditional (4) | Contredit un couplage responsabilité↔localisation : la responsabilité reste stable quels que soient les slots de l'enfant (orthogonalité des deux axes) | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Domain/PeriodeDeGarde.cs`, `src/PlanningDeGarde.Application/AffecterPeriodeHandler.cs`, `src/PlanningDeGarde.Application/IPeriodeRepository.cs`, `src/PlanningDeGarde.Application/IResponsableRepository.cs`, `tests/PlanningDeGarde.Tests/Scenario7_AffecterPeriode.cs`, `tests/PlanningDeGarde.Tests/Fakes/FakePeriodeRepository.cs`, `tests/PlanningDeGarde.Tests/Builders/PeriodeBuilder.cs`
**Design notes** :
- `PeriodeDeGarde { responsableId, début, fin }` : invariants `exactement un responsable`, `fin > début` ; bornes paramétrables (intervalle libre).
- Référence au responsable par **`Id`** (pas par objet) ; orthogonalité période↔slot vérifiée par #4 (la responsabilité ne lit jamais les slots).
