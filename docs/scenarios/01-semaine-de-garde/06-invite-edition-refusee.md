# Scénario 6 — Un Invité tente d'éditer un slot `@erreur`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_le_deplacement_du_slot_car_l_Invite_est_en_consultation_seule_et_laisser_le_slot_inchange_When_un_Invite_tente_de_deplacer_un_slot_existant` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_modification_au_motif_de_consultation_seule_When_l_auteur_de_l_action_est_un_Invite | unconditional → conditional (4) | Introduit le contrôle de droit à l'entrée de l'Application : refuser l'écriture d'un Invité (refus inconditionnel d'abord sur rôle Invité) | ⏳ Pending |
| 2 | Should_autoriser_la_modification_When_l_auteur_de_l_action_est_un_Parent | unconditional → conditional (4) | Contredit le toujours-refuser : un Parent doit pouvoir modifier → force la garde conditionnelle sur le rôle | ⏳ Pending |
| 3 | Should_laisser_le_slot_inchange_dans_le_planning_partage_When_la_modification_d_un_Invite_est_refusee | conditional (4) | Contredit un effet de bord du refus : le slot existant reste à ses bornes d'origine (snapshot inchangé) | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Application/DeplacerSlotHandler.cs`, `tests/PlanningDeGarde.Tests/Scenario6_InviteEditionRefusee.cs`
**Design notes** :
- Droit Parent/Invité gardé **à l'entrée de l'Application** (cf. analyse technique), pas dans l'agrégat — le domaine ne connaît pas les rôles d'accès.
- `@erreur` ⇒ vérifier le slot inchangé via snapshot du repository (pas d'écriture périmée).
