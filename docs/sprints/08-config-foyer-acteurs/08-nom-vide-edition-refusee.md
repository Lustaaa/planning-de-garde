# Sc.8 — Renommer avec un nom vide : édition refusée, ancien nom conservé

`@erreur` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : tranche **backend** (`tdd-auto`, 2 drivers de garde + 1 caractérisation
absence-de-diffusion) **+** acceptation **runtime IHM** (`ihm-builder` : **message clair** à
l'écran, case + légende inchangées).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime : tenter d'enregistrer un nom vide affiche un **message clair** à l'écran
> (« le nom ne peut pas être vide ») ; la case + la légende **conservent** l'ancien nom. bUnit
> seul ne prouve pas le rendu réel du message ni la non-application via le canal HTTP.

`Should_Afficher_un_message_clair_le_nom_ne_peut_pas_etre_vide_et_conserver_Bruno_dans_la_case_du_15_07_2026_et_en_legende_When_on_tente_d_enregistrer_parent_b_avec_un_nom_vide`

- **Niveau** : E2E/runtime sur l'app câblée. Store réel : `parent-b` (Bruno).
- **Observable** : message d'erreur clair à l'écran ; case du 15/07 et légende restent
  « Bruno » (inchangé).

## Tests unitaires backend (boucle interne, `tdd-auto`)

> **Garde conditionnelle dès le 1er test `@erreur`** : le nominal « renommage réussi » (Sc.1)
> est **déjà vert** ; un refus inconditionnel régresserait Sc.1. La garde « nom non vide » est
> donc **conditionnelle** (ne refuse que le cas vide), sans casser le chemin de succès.

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Refuser_l_edition_avec_un_motif_clair_et_conserver_l_ancien_nom_When_le_nom_demande_est_une_chaine_vide` | succès inconditionnel → conditionnel (garde sur la donnée) | **Driver** — le handler issu de Sc.1 applique **tout** nom fourni : un nom vide écraserait l'ancien par une chaîne vide. Force la garde « nom non vide » qui refuse (`Result.Echec` avec motif) **et laisse le store inchangé** (ancien nom conservé). | ⏳ Pending |
| 2 | `Should_Refuser_l_edition_et_conserver_l_ancien_nom_When_le_nom_demande_ne_contient_que_des_espaces` | conditionnel naïf → conditionnel robuste | **Driver** — une garde naïve « chaîne non vide » (longueur > 0) **laisserait passer** un nom tout-espaces : il écraserait « Bruno » par des blancs. Force la garde sur le nom **utile** (trim/espaces), contredisant l'implémentation minimale du #1. | ⏳ Pending |
| 3 | `Should_Ne_declencher_aucune_diffusion_temps_reel_When_une_edition_est_refusee` | invariant d'effet de bord | ⚠️ **probablement early green — couvert par Sc.1 #3 (caractérisation, pas driver)** : la notification est déclenchée **après** mutation réussie ; un refus retourne avant → aucune diffusion **par construction**. Filet (Spy) documentant « pas de diffusion sur échec ». | ⏳ Pending |

## Fichiers à créer / modifier (backend uniquement ici)

- **`EditerActeurHandler` (Application)** — garde « nom non vide » (vide / tout-espaces) →
  `Result.Echec("le nom ne peut pas être vide")`, store **non muté**, **pas** de notification.
- **Doublures tests** — `Fake` du port d'écriture (assert non-mutation) + `FakeNotificateurPlanning`
  (assert 0 notification).
- *(Affichage du message à l'écran : hors backend — routé `ihm-builder`.)*

## Design notes

- **Garde conditionnelle, pas inconditionnelle** : le refus ne s'applique qu'au nom vide ; le
  chemin de succès (Sc.1) reste vert. Le motif est **métier** (« le nom ne peut pas être
  vide »), pas technique.
- **Ancien nom conservé** = store non muté sur refus (invariant « tout acteur conserve un nom
  non vide »). Vérifié backend par la doublure ; rendu à l'écran prouvé runtime.
- **Couleur facultative** : une édition qui ne fournit qu'une couleur (sans nom) **n'est pas**
  visée par cette garde — la garde porte sur `nom` **quand il est fourni**.
