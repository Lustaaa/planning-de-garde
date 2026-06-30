# Scénario 4 — Lieu inexistant `@erreur`

> Suivi : [00-suivi.md](00-suivi.md) · Source : `docs/sprints/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_la_pose_car_le_lieu_n_existe_pas_et_n_inscrire_aucun_slot_When_un_Parent_place_un_enfant_dans_un_lieu_absent_des_lieux_du_foyer` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_pose_au_motif_de_lieu_inexistant_When_le_lieu_vise_n_est_pas_dans_les_lieux_du_foyer | unconditional → conditional (4) | Introduit la consultation des lieux du foyer : refuser quand le lieu est absent (refus d'abord inconditionnel sur lieu absent) | ✅ GREEN |
| 2 | Should_poser_le_slot_au_lieu_designe_When_le_lieu_vise_existe_dans_les_lieux_du_foyer | unconditional → conditional (4) | Contredit le toujours-refuser : un lieu existant doit réussir → force la vraie garde conditionnelle de référence du lieu | ⚠️ EARLY GREEN |
| 3 | Should_n_inscrire_aucun_slot_dans_le_planning_partage_When_la_pose_est_refusee_pour_lieu_inexistant | conditional (4) | Contredit un éventuel effet de bord partiel : refus ⇒ AllSnapshots vide | ⚠️ EARLY GREEN |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario4_LieuInexistant.cs` (réutilise `ILieuRepository` / `FakeLieuRepository`)
**Design notes** :
- Vérification d'existence du lieu = règle d'entrée de la pose ; le port `ILieuRepository` répond une **capacité** (« ce lieu existe-t-il dans le foyer ? »), pas un mécanisme.
- Couple refus-inconditionnel (#1) puis succès-contredisant (#2) pour faire émerger la garde sans voler le rouge du nominal.
- En pratique, le refus inconditionnel « toujours échouer » aurait régressé la suite nominale (Sc.1) ; la garde conditionnelle `_lieux.Existe(...)` a donc été posée dès le GREEN de #1 (seule implémentation gardant la suite verte sous non-régression). Conséquence : #2 (succès lieu existant) et #3 (absence d'effet de bord, l'early-return précède la persistance) sont ⚠️ EARLY GREEN — conservés comme filets de la branche succès et de l'invariant @erreur.
