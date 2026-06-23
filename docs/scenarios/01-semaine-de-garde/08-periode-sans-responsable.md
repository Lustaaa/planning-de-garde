# Scénario 8 — Période sans responsable refusée `@erreur`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_la_creation_car_un_responsable_est_requis_et_n_inscrire_aucune_periode_When_un_Parent_cree_une_periode_sans_designer_de_responsable` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_creation_au_motif_de_responsable_requis_When_aucun_responsable_n_est_designe_sur_la_periode | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.7 : l'absence de responsable force la garde « exactement un responsable » | ⏳ Pending |
| 2 | Should_n_inscrire_aucune_periode_dans_le_planning_partage_When_la_creation_est_refusee_faute_de_responsable | conditional (4) | Contredit l'écriture systématique : refus ⇒ aucune période persistée (AllSnapshots vide) | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario8_PeriodeSansResponsable.cs` (Domain/Application réutilisés)
**Design notes** :
- Invariant « exactement un responsable » porté par l'agrégat `PeriodeDeGarde`.
- `@erreur` ⇒ verdict d'échec + absence d'effet de bord.
