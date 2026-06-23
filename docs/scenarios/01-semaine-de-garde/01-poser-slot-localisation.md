# Scénario 1 — Un Parent pose un slot de localisation `@nominal`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_faire_apparaitre_le_slot_dans_le_planning_partage_et_notifier_l_Invite_When_un_Parent_place_un_enfant_dans_un_lieu_existant_sur_un_creneau_valide` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_la_pose_du_slot_When_un_Parent_place_un_enfant_dans_un_lieu_existant_sur_un_creneau_valide | nil → constant (2) | Baseline — pose un verdict de succès toujours-réussir pour la pose d'un slot valide | ✅ GREEN |
| 2 | Should_exposer_l_enfant_le_lieu_et_les_bornes_du_slot_pose_When_un_Parent_place_un_enfant_dans_un_lieu_existant_sur_un_creneau_valide | constant → scalar (3) | Contredit le succès vide : le slot posé doit refléter enfant/lieu/début/fin fournis (snapshot) | ✅ GREEN |
| 3 | Should_inscrire_le_slot_dans_le_planning_partage_du_foyer_When_un_Parent_a_pose_le_slot | constant → scalar (3) | Contredit l'agrégat isolé : le slot persisté apparaît dans le planning (fake repository, AllSnapshots) | ✅ GREEN |

**Fichiers à créer** : `src/PlanningDeGarde.Domain/SlotDeLocalisation.cs`, `src/PlanningDeGarde.Domain/Result.cs`, `src/PlanningDeGarde.Application/PoserSlotHandler.cs`, `src/PlanningDeGarde.Application/ISlotRepository.cs`, `src/PlanningDeGarde.Application/ILieuRepository.cs`, `tests/PlanningDeGarde.Tests/Scenario1_PoserSlot.cs`, `tests/PlanningDeGarde.Tests/Fakes/FakeSlotRepository.cs`, `tests/PlanningDeGarde.Tests/Fakes/FakeLieuRepository.cs`, `tests/PlanningDeGarde.Tests/Builders/SlotBuilder.cs`
**Design notes** :
- `SlotDeLocalisation` **sans responsable** (axe localisation seul, orthogonal à la période) — invariant porté : `fin > début`, lieu référencé.
- `Result<T>` fermé partagé Domain (succès porteur de valeur / échec porteur de motif métier) — base de la convention de refus de tous les `@erreur`.
- Fakes copy-on-read (`FromSnapshot(ToSnapshot())`), `AllSnapshots()` pour assertions ; aucun framework de mock.
- La notification temps réel à l'Invité (`And l'Invité reçoit une notification`) relève de l'**intégration SignalR** — hors liste unitaire ; couverte par le test d'acceptation BDD (second client) que `tdd-auto` portera, non décomposée ici en test unitaire.
