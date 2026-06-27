# Sc.2 — Recolorier un acteur : la case et la légende changent de couleur

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : tranche **backend** (`tdd-auto`, 2 drivers : store `recolorier`, handler route
la couleur ; +1 caractérisation indépendance nom/couleur) **+** acceptation **runtime IHM**
(`ihm-builder` : la case **et** la légende changent de couleur sans rechargement).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime : depuis l'écran de config je recolorie et j'enregistre ; **sans
> recharger**, la case **et** l'entrée de légende changent de teinte sur la grille réellement
> câblée (front WASM + API distante + store réel + SignalR). **Pas** un test bUnit à doublure.

`Should_Rendre_la_case_du_15_07_2026_en_violet_et_passer_l_entree_de_legende_au_violet_sans_recharger_en_conservant_le_libelle_Bruno_et_l_identifiant_parent_b_When_l_acteur_parent_b_est_recolorie_de_orange_en_violet`

- **Niveau** : E2E/runtime sur l'app câblée, store **réel** seedé (`parent-b → orange`).
  Anti « vert qui ment » : si la couleur n'est pas re-résolue côté grille, la case reste
  orange → rouge.
- **Observable** : la case du 15/07/2026 devient violet en conservant le libellé « Bruno » ;
  l'entrée de légende « Bruno » passe au violet ; l'identifiant « parent-b » est inchangé.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Resoudre_la_nouvelle_couleur_pour_l_identifiant_stable_When_un_acteur_deja_seme_est_recolorie_dans_le_store_de_configuration` | constante (seed couleur figé) → valeur mutable | **Driver** — la couleur est lue d'un dictionnaire `static readonly` (`Foyer.CouleursParActeur`) : **aucun moyen de recolorier**. Distinct du renommage (Sc.1) : autre surface du store. Force `recolorier(id, couleur)` dont `CouleurDe(id)` reflète la dernière écriture. | ⏳ Pending |
| 2 | `Should_Appliquer_la_nouvelle_couleur_et_confirmer_l_effet_When_la_commande_recolorie_un_acteur_connu_vers_une_teinte_du_set` | valeur mutable → commande orchestrée | **Driver** — le handler `EditerActeur` (Sc.1) ne route que le **nom** : le champ `couleur?` n'est pas appliqué. Force le routage de la couleur vers le store couleur + confirmation. | ⏳ Pending |
| 3 | `Should_Conserver_le_nom_de_l_acteur_When_seule_sa_couleur_est_modifiee` | invariant d'indépendance | ⚠️ **probablement early green — couvert par Sc.1 #1 + Sc.2 #1 (caractérisation, pas driver)** : nom et couleur vivent dans deux surfaces séparées du store ; recolorier ne touche pas le nom **par construction**. Documente l'indépendance nom↔couleur (filet anti-régression). | ⏳ Pending |

> **Caractérisation anticipée (non listée)** : la **case + légende changent de teinte** est
> une re-projection de `GrilleAgendaQuery` inchangé qui **surface** la couleur déjà résolue
> (palier 2) — prouvée au runtime, pas de rouge backend.

## Fichiers à créer / modifier (backend uniquement ici)

- **Store mutable singleton (Infrastructure)** — étendu de `recolorier` ; réalise aussi
  `IPaletteCouleurs.CouleurDe` (contrat inchangé). Remplace `FoyerPaletteCouleurs` (lecture
  du dictionnaire statique) par la lecture du store.
- **Port d'écriture (Application)** — étendu de `recolorier` ; doublure `Fake` côté tests.
- **`EditerActeurHandler` (Application)** — applique `couleur?` quand fournie (en plus de
  `nom?`).
- *(Rendu couleur case + légende, suivi SignalR : hors backend — routé `ihm-builder`.)*

## Design notes

- **Couleur = valeur surfacée, pas recalculée.** La légende/case ne recalculent rien :
  elles relisent `CouleurDe`. Recolorier ne fait que muter la valeur résolue.
- **Nom et couleur indépendants** (cf. s07 Sc.5) : deux surfaces distinctes du store ;
  recolorier vers une teinte du set est le **seul** moyen de sortir du neutre (préparé pour
  Sc.4 collision et Sc.5 hors-set).
- **Libellés couleur Gherkin illustratifs** : backend injecte le set librement, runtime
  asserte le set réel.
