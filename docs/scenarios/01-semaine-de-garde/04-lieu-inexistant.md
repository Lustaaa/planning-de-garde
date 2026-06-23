# Scénario 4 — Lieu inexistant `@erreur`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_la_pose_car_le_lieu_n_existe_pas_et_n_inscrire_aucun_slot_When_un_Parent_place_un_enfant_dans_un_lieu_absent_des_lieux_du_foyer` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_pose_au_motif_de_lieu_inexistant_When_le_lieu_vise_n_est_pas_dans_les_lieux_du_foyer | unconditional → conditional (4) | Introduit la consultation des lieux du foyer : refuser quand le lieu est absent (refus d'abord inconditionnel sur lieu absent) | ⏳ Pending |
| 2 | Should_poser_le_slot_au_lieu_designe_When_le_lieu_vise_existe_dans_les_lieux_du_foyer | unconditional → conditional (4) | Contredit le toujours-refuser : un lieu existant doit réussir → force la vraie garde conditionnelle de référence du lieu | ⏳ Pending |
| 3 | Should_n_inscrire_aucun_slot_dans_le_planning_partage_When_la_pose_est_refusee_pour_lieu_inexistant | conditional (4) | Contredit un éventuel effet de bord partiel : refus ⇒ AllSnapshots vide | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario4_LieuInexistant.cs` (réutilise `ILieuRepository` / `FakeLieuRepository`)
**Design notes** :
- Vérification d'existence du lieu = règle d'entrée de la pose ; le port `ILieuRepository` répond une **capacité** (« ce lieu existe-t-il dans le foyer ? »), pas un mécanisme.
- Couple refus-inconditionnel (#1) puis succès-contredisant (#2) pour faire émerger la garde sans voler le rouge du nominal.
