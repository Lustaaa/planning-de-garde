# Scénario 2 — Slot de durée nulle refusé `@erreur`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_la_pose_car_la_duree_est_nulle_et_n_inscrire_aucun_slot_When_un_Parent_place_un_enfant_avec_une_fin_egale_au_debut` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_pose_au_motif_de_duree_nulle_When_la_fin_du_slot_est_egale_au_debut | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.1 : une fin égale au début force la garde `fin > début` (refus inconditionnel d'abord, puis conditionnel) | ✅ GREEN |
| 2 | Should_n_inscrire_aucun_slot_dans_le_planning_partage_When_la_pose_est_refusee_pour_duree_nulle | conditional (4) | Contredit l'écriture systématique : un refus ne doit produire aucun effet de bord (AllSnapshots vide) | ⚠️ EARLY GREEN |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario2_SlotDureeNulle.cs` (Domain/Application réutilisés)
**Design notes** :
- Garde `fin > début` posée dans l'agrégat `SlotDeLocalisation` (Tell, Don't Ask), pas dans le handler.
- `@erreur` ⇒ double assertion : verdict d'échec **et** absence d'effet de bord — convention `Result<T>` tenue.
- Test #2 (⚠️ EARLY GREEN) : l'absence d'effet de bord était déjà garantie par l'early-return du handler implémenté au GREEN du test #1. Le test n'a pas piloté de rouge mais verrouille l'invariant `@erreur` ; conservé comme filet de régression.
