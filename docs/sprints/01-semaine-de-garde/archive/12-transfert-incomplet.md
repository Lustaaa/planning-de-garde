# Scénario 12 — Transfert incomplet refusé `@erreur`

> Suivi : [00-suivi.md](00-suivi.md) · Source : `docs/sprints/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_refuser_la_definition_car_la_recuperation_et_l_heure_sont_requises_et_n_inscrire_aucun_transfert_When_un_Parent_definit_un_transfert_sans_recuperant_ni_heure` — ✅ GREEN

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_definition_au_motif_de_recuperation_et_heure_requises_When_le_recuperant_et_l_heure_du_transfert_sont_absents | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.11 : l'absence de récupérant/heure force la garde de complétude du transfert | ✅ GREEN |
| 2 | Should_n_inscrire_aucun_transfert_dans_le_planning_partage_When_la_definition_est_refusee_pour_transfert_incomplet | conditional (4) | Contredit l'écriture systématique : refus ⇒ aucun transfert persisté (AllSnapshots vide) | ⚠️ EARLY GREEN |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario12_TransfertIncomplet.cs` (Domain/Application réutilisés)
**Design notes** :
- Invariant de complétude porté par l'agrégat `Transfert` ; un seul `@erreur` couvre récupérant + heure manquants (même comportement de refus, données groupées).
- `@erreur` ⇒ verdict d'échec + absence d'effet de bord.
- Invariant de complétude posé dans `Transfert.Definir` (Tell-Don't-Ask) : retourne désormais `Result<Transfert>`, échec si dépose/récupère/lieu vide ou heure non renseignée (`TimeSpan.Zero`) ; le handler unwrap et n'inscrit qu'en succès. Un seul `@erreur` couvre récupérant + heure manquants (données groupées, même comportement de refus). Sc.11 nominal restant vert, la garde est conditionnelle d'emblée. #1 vrai rouge ; #2 ⚠️ EARLY GREEN (l'early-return précède la persistance), conservé comme filet @erreur.
