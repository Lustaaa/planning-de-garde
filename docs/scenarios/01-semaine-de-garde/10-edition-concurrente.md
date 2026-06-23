# Scénario 10 — Édition concurrente d'une période `@erreur`

> Suivi : [suivi.md](suivi.md) · Source : `docs/scenarios/01-semaine-de-garde.md`

**Acceptation (BDD)** : `Should_rejeter_la_modification_fondee_sur_un_etat_perime_et_inviter_a_recharger_la_periode_When_un_Parent_enregistre_depuis_un_affichage_devance_par_une_modification_anterieure` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_enregistrer_la_modification_When_elle_se_fonde_sur_la_version_courante_de_la_periode | constant → scalar (3) | Baseline du contrôle de version : une modification sur l'état à jour réussit (toujours-accepter d'abord) | ⏳ Pending |
| 2 | Should_rejeter_la_modification_au_motif_d_etat_perime_When_la_periode_a_ete_modifiee_depuis_l_affichage_de_l_auteur | unconditional → conditional (4) | Contredit le toujours-accepter : une modification fondée sur une version dépassée est rejetée (écriture périmée) | ⏳ Pending |
| 3 | Should_conserver_la_modification_anterieure_de_la_periode_When_une_modification_perimee_est_rejetee | conditional (4) | Contredit un écrasement : l'état reste celui de la modification antérieure (snapshot inchangé, pas d'effet de bord) | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Application/ModifierPeriodeHandler.cs`, `tests/PlanningDeGarde.Tests/Scenario10_EditionConcurrente.cs`
**Design notes** :
- Tester le rejet de l'écriture périmée en ne doublant **que** le port de persistance (`IPeriodeRepository`) ; la version/jeton optimiste fait partie du contrat de sauvegarde, pas un getter ajouté à l'agrégat.
- « Invité à recharger l'état à jour » = motif métier porté par le verdict d'échec `Result<T>`, observable sans détail technique.
