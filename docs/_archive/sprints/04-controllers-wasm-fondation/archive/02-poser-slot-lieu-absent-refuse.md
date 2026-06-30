# Scénario 2 — Poser un slot sur un lieu absent du foyer est refusé et ne touche pas la grille

`@erreur`

[← Retour au suivi](00-sprint04-suivi.md)

> **Axe : backend.** Canal d'écriture (endpoint HTTP) → `PoserSlotHandler` **inchangé** →
> store réel → projection réelle. Routé vers `tdd-auto`. **Niveau d'acceptation :
> intégration** (`WebApplicationFactory`).
>
> **Garde conditionnelle dès le 1er test `@erreur`** : le nominal Sc.1 (pose réussie via le
> canal) est vert avant ce scénario ; un refus inconditionnel régresserait Sc.1. La garde
> « lieu inexistant » est donc **conditionnelle** dès le départ (déjà portée par
> `PoserSlotHandler`, motif « Le lieu visé n'existe pas dans les lieux du foyer »).

## Acceptation (BDD)

`Should_Refuser_la_pose_au_motif_que_le_lieu_vise_n_existe_pas_et_laisser_la_case_du_mercredi_24_06_2026_sans_slot_When_la_commande_de_pose_au_lieu_piscine_absent_du_foyer_est_emise_via_le_canal` — ✅ GREEN

Test d'**intégration de bout en bout** (`WebApplicationFactory<Program>`, store réel) :
- **Given** le foyer connaît « école »/« domicile A » mais **pas** « piscine » (référentiel
  réel) ; aucun slot pour le 24/06/2026 ;
- **When** la commande de pose (Léa, « piscine », 24/06/2026 08:30→16:30) est émise via le
  canal requête/réponse ;
- **Then** le canal renvoie une **réponse d'échec** « le lieu visé n'existe pas dans les
  lieux du foyer » ; **et** la projection réelle ne porte **aucun** slot « piscine » et la
  case du mercredi 24/06 **reste sans slot**.

## Tests d'intégration (ordonnés)

| # | Test (FLFI) | TPP | Contradiction | Status |
|---|-------------|-----|---------------|--------|
| 1 | `Should_Renvoyer_une_reponse_d_echec_au_motif_que_le_lieu_vise_n_existe_pas_When_la_commande_de_pose_au_lieu_piscine_absent_du_foyer_est_emise_via_le_canal` | endpoint succès → endpoint qui propage le refus | **Driver du canal d'erreur** : un canal qui acquitterait toujours en succès (Sc.1) passe le nominal mais échoue ici. Force le canal à **propager le `Result.Echec`** du handler (statut/réponse d'échec + motif métier). ⚠️ Le refus handler « lieu inexistant » est déjà vert en unit (`Scenario4_LieuInexistant`) — le driver est la **propagation par le canal**, pas la règle. | ✅ GREEN |
| 2 | `Should_Laisser_la_case_du_mercredi_24_06_2026_sans_aucun_slot_piscine_dans_la_projection_reelle_When_la_pose_au_lieu_absent_a_ete_refusee_via_le_canal` | refus propagé → absence d'effet de bord observée en bout de chaîne | **Anti early-green / vert qui ment** : un canal qui répondrait « échec » mais aurait quand même persisté (ou un store doublé) passe le #1 mais échoue ici. Force l'observation sur le **store réel** via la projection : aucune case ne porte « piscine », case du 24/06 vide. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- Test d'intégration `tests/PlanningDeGarde.Web.Tests/` (cas d'erreur du canal de pose).
- (Endpoint d'écriture « pose de slot » déjà introduit au Sc.1 — réutilisé ; pas de nouveau
  fichier de production attendu.)

## Design notes

- **Réponse d'échec** : la convention de signalement reste `Result<T>` côté Application ; le
  canal la traduit en réponse d'échec porteuse du **motif métier** (langage métier, pas de
  code HTTP dans l'étiquette). Le détail du statut est une décision d'implémentation de
  `tdd-auto`.
- **Absence d'effet de bord** : observer la **projection réelle** après le canal (case 24/06
  vide, aucune case « piscine »), pas un accusé du canal — ferme le piège « vert qui ment ».
- **Aucune notification** : un refus ne doit pas déclencher la diffusion temps réel
  (vérifiable par Spy sur `INotificateurPlanning` si `tdd-auto` le juge utile).
