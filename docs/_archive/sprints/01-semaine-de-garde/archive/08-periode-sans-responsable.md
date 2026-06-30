# Scénario 8 — Période sans responsable refusée `@erreur`

> Suivi : [00-suivi.md](00-suivi.md) · Source : `docs/sprints/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_la_creation_car_un_responsable_est_requis_et_n_inscrire_aucune_periode_When_un_Parent_cree_une_periode_sans_designer_de_responsable` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_creation_au_motif_de_responsable_requis_When_aucun_responsable_n_est_designe_sur_la_periode | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.7 : l'absence de responsable force la garde « exactement un responsable » | ✅ GREEN |
| 2 | Should_n_inscrire_aucune_periode_dans_le_planning_partage_When_la_creation_est_refusee_faute_de_responsable | conditional (4) | Contredit l'écriture systématique : refus ⇒ aucune période persistée (AllSnapshots vide) | ⚠️ EARLY GREEN |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario8_PeriodeSansResponsable.cs` (Domain/Application réutilisés)
**Design notes** :
- Invariant « exactement un responsable » porté par l'agrégat `PeriodeDeGarde`.
- `@erreur` ⇒ verdict d'échec + absence d'effet de bord.
- Invariant posé dans `PeriodeDeGarde.Affecter` (Tell-Don't-Ask) : retourne désormais `Result<PeriodeDeGarde>`, échec si `responsableId` vide/blanc ; le handler unwrap et n'inscrit qu'en cas de succès. Sc.7 nominal restant vert, la garde est conditionnelle d'emblée (un refus inconditionnel l'aurait régressé). #1 vrai rouge ; #2 ⚠️ EARLY GREEN (l'early-return précède la persistance), conservé comme filet @erreur.
