# Scenario 5 — La page de saisie dédiée « Définir un transfert » n'existe plus `@limite 🖥️ IHM`

[← Retour au suivi](00-sprint12-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**.
> **Driver réel (vrai cycle RED→GREEN)** — le retrait du dernier écran de saisie dédié referme
> l'épic É12. Le nettoyage n'est exécuté **qu'APRÈS** que l'acceptation runtime du Sc.1 prouve
> la **couverture intégrale** de l'écran supprimé (borne du Risque P1). Symptôme runtime : tant
> que la route/page/lien subsistent, le test « la route n'existe plus » est rouge.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Ne_proposer_aucun_lien_ni_route_de_saisie_dediee_de_transfert_et_ne_laisser_que_la_dialog_depuis_une_case_When_le_planning_est_affiche_pour_un_parent** — ✅ GREEN (RED→GREEN) (`FrontWasmTransfertAucunEcranDedieTests`)

Sur l'app **réellement câblée**, le planning est affiché pour un Parent. **Observable runtime** :
(1) **aucun lien** « Définir un transfert » vers un écran de saisie dédié n'est présent dans la
barre du planning / le NavMenu ; (2) ouvrir directement la route `/planning/definir-transfert`
**n'aboutit plus** (route inexistante) ; (3) le **seul chemin** pour définir un transfert est la
**dialog ouverte depuis une case** (couverture prouvée par l'acceptation Sc.1). *Anti
vert-qui-ment* : exécuter ce retrait **après** Sc.1 vert ; vérifier que la suite complète reste
verte (les tests des écrans supprimés retirés, comportement couvert par la dialog + acceptation
runtime).

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Ne_pas_afficher_de_lien_vers_un_ecran_dedie_de_transfert_When_le_planning_est_affiche | (unconditional) absence de lien | Tant que le lien-barre / NavMenu pointe vers `/planning/definir-transfert`, l'assertion d'absence échoue | ✅ couvert par l'acceptation runtime (assertions 1 & 2 : absence de lien barre + NavMenu) |
| 2 | Should_Ne_pas_resoudre_la_route_planning_definir_transfert_When_elle_est_ouverte_directement | (unconditional) route retirée | Tant que `@page "/planning/definir-transfert"` existe, la route résout encore → contredit « la route n'existe plus » | ✅ couvert par l'acceptation runtime (assertion 3 : aucun `RouteAttribute` `/planning/definir-transfert` dans l'assembly Web) |

## Fichiers à créer / modifier (par `ihm-builder`)

- **Suppression** de `DefinirTransfert.razor` (+ code-behind) et de sa directive
  `@page "/planning/definir-transfert"`.
- **`PlanningPartage.razor` / NavMenu** — retrait du **lien** « Définir un transfert ».
- **`Web.Tests`** — bUnit (absence de lien + route non résolue) ; retrait des tests devenus
  caducs des écrans supprimés ; acceptation runtime. Suite complète à revérifier verte.

## Design notes

- **Ordre impératif** : ce scénario s'exécute **après** l'acceptation runtime du Sc.1 (borne
  Risque P1 — ne pas retirer l'écran avant que la dialog en prouve la couverture intégrale).
- **Referme É12** : à la livraison, **plus aucun écran de saisie dédié ne subsiste** (Poser un
  slot et Affecter une période déjà retirés s11) → l'épic « écriture en contexte » est clos.
- **Repli horloge conservé** : la suppression concerne la **page/route/lien**, pas le port
  `IDateTimeProvider` (garde-fou règle 17, cf. Sc.2).
