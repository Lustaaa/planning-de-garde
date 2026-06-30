# Scénario 5 — Affecter une période sur une plage de 2 cases contiguës

`@nominal` · **🖥️ scénario IHM** — **Routé vers `ihm-builder`** · **acceptation RUNTIME**. Le **write
réutilise `AffecterPeriode` existant** (backend early green) ; le neuf est **front** (sélection de plage +
dialog pré-remplie sur l'intervalle).

[← Retour au suivi](00-sprint15-suivi.md)

Connecté en Parent, sélectionner les cases du mardi 09/06 au mercredi 10/06 (fond Alice/bleu) et affecter
« Bruno » : **une seule** période 09→10/06 / responsable Bruno est enregistrée ; les 2 cases réapparaissent
nommées « Bruno » et colorées **orange** (surcharge Bruno/orange primant le fond Alice/bleu — couleurs
distinctes, ce qui prouve visiblement que la surcharge prime) ; aucune autre case modifiée (11/06 reste Alice/bleu).

## Acceptation (BDD) — niveau RUNTIME — ✅ GREEN

`Should_Enregistrer_une_seule_periode_du_09_au_10_06_2026_responsable_Bruno_et_faire_reapparaitre_les_deux_cases_nommees_Bruno_en_orange_sans_modifier_les_autres_When_un_Parent_selectionne_la_plage_des_deux_cases_contigues_et_affecte_Bruno_sur_l_app_reellement_cablee`
(`tests/PlanningDeGarde.Web.Tests/FrontWasmAffecterPeriodePlageContigueTempsReelTests.cs`)
— sur l'app réellement câblée : la sélection de 2 cases contiguës ouvre l'affectation pré-remplie sur
l'intervalle `[09/06, 10/06]` ; l'écriture passe par le **canal requête/réponse** (`AffecterPeriode`) ; la
grille **relue** (jamais mutée localement) nomme/colore les 2 cases en Bruno/bleu ; les cases hors plage
restent inchangées.

## Inner-loop (boucle rapide `ihm-builder`)

| # | Test inner-loop (sélection de plage → intervalle) | Contradiction | Status |
|---|---------------------------------------------------|---------------|--------|
| 1 | `Should_Deriver_l_intervalle_du_09_au_10_06_2026_When_deux_cases_contigues_sont_selectionnees` | Aucune sélection de plage n'existe ; force un état de sélection (début/fin) dérivant l'intervalle `[min, max]` des 2 dates. **Driver front.** — couvert par l'acceptation runtime (sélection clic-début + clic-fin → intervalle `[09/06, 10/06]`). | ✅ GREEN |
| 2 | `Should_Emettre_une_seule_commande_AffecterPeriode_couvrant_l_intervalle_selectionne_When_l_affectation_est_validee_sur_la_plage` | ⚠️ early green côté **handler** — `AffecterPeriode` couvre déjà un intervalle `[début, fin]` (inclusif aux 2 bornes, vérifié) ; le neuf est le **câblage de la dialog de plage** (param `DateFinContexte`). Caractérisation du write (1 seul snapshot store), driver du câblage. | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) — **sélection de plage**
  de cases contiguës (état début/fin), ouverture de l'affectation pré-remplie sur l'intervalle, **relecture**
  après succès.
- `src/PlanningDeGarde.Web/Components/AffecterPeriodeDialog.razor` (+ `.razor.cs`) — réutilisée, pré-remplie
  sur `[début, fin]` de la plage (au lieu de la seule date de case).
- *(réutilisé, AUCUN changement)* `AffecterPeriodeHandler` / `AffecterPeriodeCommand` / `IPeriodeRepository` —
  le handler gère déjà l'intervalle.

## Design notes

- **AUCUN handler ni port neuf** (analyse technique, bloc B borné) : la plage émet **une** commande
  `AffecterPeriode(Bruno, 09/06, 10/06)`. La réapparition se fait par **relecture** (canal de lecture),
  diffusion SignalR inchangée — la grille reste lecture seule.
- **Surcharge prime le fond** : déjà garanti par `CaseJourAu` (`surcharge ?? fond`) — caractérisation, pas
  un driver. « Aucune autre case modifiée » découle d'une période bornée à l'intervalle (relecture).
- **Gating règle 9 mutualisé** : le déclencheur de plage est gardé `Session.EstParent` (gate présent sur
  `PlanningPartage` via `OuvrirMenu`) — c'est ce gating partagé que Sc.7 caractérise. → remonter au CP si
  le **geste** de sélection (drag vs clic début + clic fin) doit être tranché ; variantes de plage (vide,
  chevauchement, à cheval sur vue/mois, drag riche) **reportées tranche 2** (cf. cadrage CP, à consigner au
  backlog en `/4-retours`).

## Réalisation (`ihm-builder` — RED→GREEN runtime)

- **Geste tranché = clic début + clic fin** (le `drag riche` étant explicitement reporté tranche 2, le
  geste tranche 1 se réduit à un geste clic-based — pas d'escalade CP nécessaire). Un bouton **toggle
  « Sélectionner une plage »** (data-testid `mode-plage`, **gardé `Session.EstParent`** → invisible pour
  l'Invité, ce qui rend Sc.7 inerte par construction) entre en **mode plage** ; le 1ᵉʳ clic-case fixe le
  début, le 2ᵉ borne l'intervalle `[min, max]` et **ouvre l'affectation pré-remplie** sur la plage. Hors
  mode plage, le clic-case ouvre le menu single-jour (palier 7 inchangé → Sc.2 préservé).
- **Réutilisation pure du write** : `AffecterPeriodeDialog` gagne un paramètre **optionnel `DateFinContexte`**
  (borne de fin) ; `null` = comportement single-jour antérieur (Début = Fin). **Aucun handler ni port neuf** ;
  **une seule** commande `AffecterPeriode(parent-b, 09/06, 10/06)` émise, vérifiée à **1 snapshot** sur le
  store réel. Réapparition par **relecture** ; surcharge Bruno/orange prime le fond Alice/bleu ; 11/06 (hors
  plage) inchangé.
- **Pour Sc.7** : la sélection de plage est **réutilisable telle quelle** — le gating `EstParent` du bouton
  `mode-plage` + le garde de `OuvrirMenu`/`BasculerModePlage` rendent toute tentative de plage **inerte** en
  consultation (aucun déclencheur, aucune dialog). Sc.7 sera une **caractérisation** de ce gate, **non tiré
  en avant** ici.
