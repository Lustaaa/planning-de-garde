# Scénario 5 — Chevauchement de localisation pour le même enfant `@limite`

> Suivi : [00-suivi.md](00-suivi.md) · Source : `docs/sprints/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_creer_le_second_slot_et_signaler_un_chevauchement_entre_les_slots_de_l_enfant_le_meme_jour_When_un_Parent_pose_un_slot_qui_recouvre_un_slot_existant_du_meme_enfant` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_creer_le_second_slot_et_le_faire_apparaitre_dans_le_planning_partage_When_il_recouvre_un_slot_existant_du_meme_enfant | constant → scalar (3) | Pose la règle clé du `@limite` : le recouvrement **n'est pas** un invariant d'écriture → la création réussit malgré le chevauchement | ⚠️ EARLY GREEN |
| 2 | Should_ne_signaler_aucun_chevauchement_When_les_slots_d_un_enfant_un_meme_jour_ne_se_recouvrent_pas | constant → scalar (3) | Baseline de la projection de lecture : sans recouvrement, aucun avertissement (refus inconditionnel d'avertir d'abord) | ✅ GREEN (baseline) |
| 3 | Should_signaler_un_chevauchement_entre_les_slots_de_l_enfant_le_meme_jour_When_deux_slots_du_meme_enfant_se_recouvrent | unconditional → conditional (4) | Contredit le « jamais d'avertissement » : deux slots recouvrants d'un même enfant le même jour déclenchent l'avertissement | ✅ GREEN |

**Fichiers à créer** : `src/PlanningDeGarde.Application/JourneeEnfantQuery.cs` (read model), `src/PlanningDeGarde.Application/AvertissementChevauchement.cs`, `tests/PlanningDeGarde.Tests/Scenario5_Chevauchement.cs`
**Design notes** :
- L'avertissement de chevauchement est une **projection de lecture** (CQRS) sur la journée d'un enfant, **pas** un invariant d'agrégat — ne touche jamais `SlotDeLocalisation`. Séparer écriture (#1 réussit toujours) et lecture (#2/#3 calculent l'avertissement).
- Chevauchement borné au **même enfant** et au **même jour** (cf. `Then` : « entre les slots de Léa le 15/07 »).
- Implémenté en `JourneeEnfantQuery` (read model CQRS) + `AvertissementChevauchement` (record de lecture), tous deux en Application ; recouvrement détecté par `a.Debut < b.Fin && b.Debut < a.Fin`. L'agrégat `SlotDeLocalisation` et le handler sont restés intacts (aucun invariant d'écriture ajouté).
- Tests #1 (⚠️ EARLY GREEN : la création du second slot ne fut jamais bloquée — pas d'invariant d'écriture) et #2 (✅ GREEN baseline : stub constant renvoyant aucun avertissement, TPP constant-first) ; seul #3 a piloté un vrai rouge faisant émerger le calcul conditionnel de chevauchement.
