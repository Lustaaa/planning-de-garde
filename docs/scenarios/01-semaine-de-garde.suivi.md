# Suivi TDD — Semaine de garde

> Source : `docs/scenarios/01-semaine-de-garde.md` · produit par tdd-analyse, suivi par tdd-auto.

> **Cadrage scaffolding** — Solution `PlanningDeGarde.sln` : projets `PlanningDeGarde.Domain`,
> `PlanningDeGarde.Application`, `PlanningDeGarde.Infrastructure`, `PlanningDeGarde.Web` (Blazor),
> tests `PlanningDeGarde.Tests` (xUnit). Refus via type `Result<T>` fermé (les `@erreur` assertent
> le verdict + l'absence d'effet de bord). Domaine sans framework, ports en Application.
> SignalR/persistance en Infrastructure ; droits Parent/Invité gardés à l'entrée de l'Application.

## Scénario 1 — Un Parent pose un slot de localisation `@nominal`

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

## Scénario 2 — Slot de durée nulle refusé `@erreur`

**Acceptation (BDD)** : `Should_refuser_la_pose_car_la_duree_est_nulle_et_n_inscrire_aucun_slot_When_un_Parent_place_un_enfant_avec_une_fin_egale_au_debut` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_pose_au_motif_de_duree_nulle_When_la_fin_du_slot_est_egale_au_debut | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.1 : une fin égale au début force la garde `fin > début` (refus inconditionnel d'abord, puis conditionnel) | ⏳ Pending |
| 2 | Should_n_inscrire_aucun_slot_dans_le_planning_partage_When_la_pose_est_refusee_pour_duree_nulle | conditional (4) | Contredit l'écriture systématique : un refus ne doit produire aucun effet de bord (AllSnapshots vide) | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario2_SlotDureeNulle.cs` (Domain/Application réutilisés)
**Design notes** :
- Garde `fin > début` posée dans l'agrégat `SlotDeLocalisation` (Tell, Don't Ask), pas dans le handler.
- `@erreur` ⇒ double assertion : verdict d'échec **et** absence d'effet de bord — convention `Result<T>` tenue.

## Scénario 3 — Slot de nuit franchissant minuit `@limite`

**Acceptation (BDD)** : `Should_faire_apparaitre_le_slot_de_nuit_dans_le_planning_partage_When_un_Parent_place_un_enfant_de_22h_un_jour_a_7h_le_lendemain` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_la_pose_du_slot_et_exposer_ses_bornes_de_22h_a_7h_le_lendemain_When_le_slot_franchit_minuit | conditional (4) | Contredit une garde de durée naïve fondée sur l'heure seule : un slot qui franchit minuit (fin calendaire > début) reste de durée positive et doit réussir | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario3_SlotFranchissantMinuit.cs` (Domain/Application réutilisés)
**Design notes** :
- Distinguer durée nulle (Sc.2) d'un franchissement de minuit : la comparaison `fin > début` porte sur l'instant calendaire complet (date + heure), pas sur l'heure du jour — c'est ce qui sépare ce `@limite` du `@erreur` précédent.

## Scénario 4 — Lieu inexistant `@erreur`

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

## Scénario 5 — Chevauchement de localisation pour le même enfant `@limite`

**Acceptation (BDD)** : `Should_creer_le_second_slot_et_signaler_un_chevauchement_entre_les_slots_de_l_enfant_le_meme_jour_When_un_Parent_pose_un_slot_qui_recouvre_un_slot_existant_du_meme_enfant` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_creer_le_second_slot_et_le_faire_apparaitre_dans_le_planning_partage_When_il_recouvre_un_slot_existant_du_meme_enfant | constant → scalar (3) | Pose la règle clé du `@limite` : le recouvrement **n'est pas** un invariant d'écriture → la création réussit malgré le chevauchement | ⏳ Pending |
| 2 | Should_ne_signaler_aucun_chevauchement_When_les_slots_d_un_enfant_un_meme_jour_ne_se_recouvrent_pas | constant → scalar (3) | Baseline de la projection de lecture : sans recouvrement, aucun avertissement (refus inconditionnel d'avertir d'abord) | ⏳ Pending |
| 3 | Should_signaler_un_chevauchement_entre_les_slots_de_l_enfant_le_meme_jour_When_deux_slots_du_meme_enfant_se_recouvrent | unconditional → conditional (4) | Contredit le « jamais d'avertissement » : deux slots recouvrants d'un même enfant le même jour déclenchent l'avertissement | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Application/JourneeEnfantQuery.cs` (read model), `src/PlanningDeGarde.Application/AvertissementChevauchement.cs`, `tests/PlanningDeGarde.Tests/Scenario5_Chevauchement.cs`
**Design notes** :
- L'avertissement de chevauchement est une **projection de lecture** (CQRS) sur la journée d'un enfant, **pas** un invariant d'agrégat — ne touche jamais `SlotDeLocalisation`. Séparer écriture (#1 réussit toujours) et lecture (#2/#3 calculent l'avertissement).
- Chevauchement borné au **même enfant** et au **même jour** (cf. `Then` : « entre les slots de Léa le 15/07 »).

## Scénario 6 — Un Invité tente d'éditer un slot `@erreur`

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

## Scénario 7 — Un Parent affecte la responsabilité d'une période de garde `@nominal`

**Acceptation (BDD)** : `Should_indiquer_le_responsable_de_la_periode_dans_le_planning_partage_independamment_du_lieu_de_l_enfant_When_un_Parent_affecte_un_responsable_sur_un_intervalle` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_l_affectation_de_la_periode_When_un_Parent_rend_un_responsable_responsable_sur_un_intervalle_valide | nil → constant (2) | Baseline du nouvel agrégat période : pose un succès toujours-réussir d'affectation | ⏳ Pending |
| 2 | Should_exposer_le_responsable_et_l_intervalle_de_la_periode_affectee_When_un_Parent_a_affecte_la_responsabilite | constant → scalar (3) | Contredit le succès vide : la période reflète responsable / début / fin fournis (snapshot) | ⏳ Pending |
| 3 | Should_indiquer_la_periode_dans_le_planning_partage_du_foyer_When_la_periode_a_ete_affectee | constant → scalar (3) | Contredit l'agrégat isolé : la période persistée apparaît dans le planning (fake repository) | ⏳ Pending |
| 4 | Should_conserver_le_responsable_de_la_periode_When_l_enfant_change_de_lieu_pendant_l_intervalle | conditional (4) | Contredit un couplage responsabilité↔localisation : la responsabilité reste stable quels que soient les slots de l'enfant (orthogonalité des deux axes) | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Domain/PeriodeDeGarde.cs`, `src/PlanningDeGarde.Application/AffecterPeriodeHandler.cs`, `src/PlanningDeGarde.Application/IPeriodeRepository.cs`, `src/PlanningDeGarde.Application/IResponsableRepository.cs`, `tests/PlanningDeGarde.Tests/Scenario7_AffecterPeriode.cs`, `tests/PlanningDeGarde.Tests/Fakes/FakePeriodeRepository.cs`, `tests/PlanningDeGarde.Tests/Builders/PeriodeBuilder.cs`
**Design notes** :
- `PeriodeDeGarde { responsableId, début, fin }` : invariants `exactement un responsable`, `fin > début` ; bornes paramétrables (intervalle libre).
- Référence au responsable par **`Id`** (pas par objet) ; orthogonalité période↔slot vérifiée par #4 (la responsabilité ne lit jamais les slots).

## Scénario 8 — Période sans responsable refusée `@erreur`

**Acceptation (BDD)** : `Should_refuser_la_creation_car_un_responsable_est_requis_et_n_inscrire_aucune_periode_When_un_Parent_cree_une_periode_sans_designer_de_responsable` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_creation_au_motif_de_responsable_requis_When_aucun_responsable_n_est_designe_sur_la_periode | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.7 : l'absence de responsable force la garde « exactement un responsable » | ⏳ Pending |
| 2 | Should_n_inscrire_aucune_periode_dans_le_planning_partage_When_la_creation_est_refusee_faute_de_responsable | conditional (4) | Contredit l'écriture systématique : refus ⇒ aucune période persistée (AllSnapshots vide) | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario8_PeriodeSansResponsable.cs` (Domain/Application réutilisés)
**Design notes** :
- Invariant « exactement un responsable » porté par l'agrégat `PeriodeDeGarde`.
- `@erreur` ⇒ verdict d'échec + absence d'effet de bord.

## Scénario 9 — Bornes de période paramétrables `@limite`

**Acceptation (BDD)** : `Should_indiquer_la_periode_aux_bornes_demandees_dans_le_planning_partage_When_un_Parent_affecte_un_responsable_sur_un_intervalle_decale` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_exposer_la_periode_aux_bornes_demandees_When_l_intervalle_affecte_ne_commence_pas_un_lundi | constant → scalar (3) | Contredit toute borne figée (semaine lundi→lundi) : un intervalle arbitraire mercredi→mercredi doit être conservé tel quel | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario9_BornesParametrables.cs` (Domain/Application réutilisés)
**Design notes** :
- Vérifie l'absence d'hypothèse de calendrier fixe : les bornes sont des données, pas une convention codée en dur — contredit un éventuel raccourci pris au Sc.7.

## Scénario 10 — Édition concurrente d'une période `@erreur`

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

## Scénario 11 — Définir le transfert de bascule entre deux parents `@nominal`

**Acceptation (BDD)** : `Should_afficher_le_transfert_dans_le_planning_partage_et_faire_basculer_la_responsabilite_au_point_de_transfert_When_un_Parent_definit_un_transfert_complet_entre_deux_periodes_contigues` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_confirmer_la_definition_du_transfert_When_un_Parent_definit_un_transfert_complet | nil → constant (2) | Baseline du nouvel agrégat transfert : succès toujours-réussir d'une définition complète | ⏳ Pending |
| 2 | Should_exposer_le_deposant_le_recuperant_le_lieu_l_heure_et_la_date_du_transfert_When_le_transfert_a_ete_defini | constant → scalar (3) | Contredit le succès vide : le transfert reflète dépose/récupère/lieu/heure/date fournis (snapshot) | ⏳ Pending |
| 3 | Should_afficher_le_transfert_dans_le_planning_partage_du_foyer_When_le_transfert_a_ete_defini | constant → scalar (3) | Contredit l'agrégat isolé : le transfert persisté apparaît dans le planning | ⏳ Pending |
| 4 | Should_faire_basculer_la_responsabilite_du_deposant_au_recuperant_au_point_de_transfert_When_le_transfert_borne_deux_periodes_contigues | conditional (4) | Contredit une responsabilité statique : au point de transfert la responsabilité passe de A à B (bascule observable) | ⏳ Pending |

**Fichiers à créer** : `src/PlanningDeGarde.Domain/Transfert.cs`, `src/PlanningDeGarde.Application/DefinirTransfertHandler.cs`, `src/PlanningDeGarde.Application/ITransfertRepository.cs`, `tests/PlanningDeGarde.Tests/Scenario11_DefinirTransfert.cs`, `tests/PlanningDeGarde.Tests/Fakes/FakeTransfertRepository.cs`, `tests/PlanningDeGarde.Tests/Builders/TransfertBuilder.cs`
**Design notes** :
- `Transfert { déposeParId, récupèreParId, lieuId, heure, date }` = point de bascule A↔B ; invariant : dépose + récupère + lieu + heure tous renseignés.
- Bascule de responsabilité (#4) observable au point de transfert : le transfert borne deux périodes contiguës (la cohérence trou/chevauchement entre périodes reste hors socle — cf. questions ouvertes).

## Scénario 12 — Transfert incomplet refusé `@erreur`

**Acceptation (BDD)** : `Should_refuser_la_definition_car_la_recuperation_et_l_heure_sont_requises_et_n_inscrire_aucun_transfert_When_un_Parent_definit_un_transfert_sans_recuperant_ni_heure` — ⏳ Pending

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|---|---|---|---|
| 1 | Should_refuser_la_definition_au_motif_de_recuperation_et_heure_requises_When_le_recuperant_et_l_heure_du_transfert_sont_absents | unconditional → conditional (4) | Contredit le toujours-réussir du Sc.11 : l'absence de récupérant/heure force la garde de complétude du transfert | ⏳ Pending |
| 2 | Should_n_inscrire_aucun_transfert_dans_le_planning_partage_When_la_definition_est_refusee_pour_transfert_incomplet | conditional (4) | Contredit l'écriture systématique : refus ⇒ aucun transfert persisté (AllSnapshots vide) | ⏳ Pending |

**Fichiers à créer** : `tests/PlanningDeGarde.Tests/Scenario12_TransfertIncomplet.cs` (Domain/Application réutilisés)
**Design notes** :
- Invariant de complétude porté par l'agrégat `Transfert` ; un seul `@erreur` couvre récupérant + heure manquants (même comportement de refus, données groupées).
- `@erreur` ⇒ verdict d'échec + absence d'effet de bord.
