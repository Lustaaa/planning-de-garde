# Scénario 3 — Slot de nuit franchissant minuit `@limite`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_faire_apparaitre_le_slot_de_nuit_dans_le_planning_partage_When_un_Parent_place_un_enfant_de_22h_un_jour_a_7h_le_lendemain` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_la_pose_du_slot_et_exposer_ses_bornes_de_22h_a_7h_le_lendemain_When_le_slot_franchit_minuit | conditional (4) | Contredit une garde de durée naïve fondée sur l'heure seule : un slot qui franchit minuit (fin calendaire > début) reste de durée positive et doit réussir | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario3_SlotFranchissantMinuit.cs` (Domain/Application réutilisés)
**Design notes** :
- Distinguer durée nulle (Sc.2) d'un franchissement de minuit : la comparaison `fin > début` porte sur l'instant calendaire complet (date + heure), pas sur l'heure du jour — c'est ce qui sépare ce `@limite` du `@erreur` précédent.
