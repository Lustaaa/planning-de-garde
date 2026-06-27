# Sc.4 — Collision de couleur entre deux acteurs : distingués par le nom

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **caractérisation backend** (`tdd-auto`, ⚠️ early green attendu) **+** acceptation
**runtime IHM** (`ihm-builder`). La collision de couleur est **assumée** (règle 17 : la
lisibilité repose sur le **nom + légende**, pas la couleur seule) — **pas un défaut**.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime : après recoloriage de `parent-b` vers la couleur de `parent-a`, les deux
> cases sont de la même teinte mais **restent distinguables par leur nom**, et la légende
> liste **deux** entrées de même couleur, nommées distinctement. bUnit seul ne prouve pas le
> rendu réel.

`Should_Rendre_les_cases_du_14_07_et_du_15_07_2026_toutes_deux_bleues_distinguables_par_Alice_et_Bruno_avec_deux_entrees_de_legende_bleues_nommees_distinctement_When_parent_b_est_recolorie_vers_la_couleur_de_parent_a`

- **Niveau** : E2E/runtime sur l'app câblée. Store réel : `parent-a` (Alice, bleu) +
  recoloriage `parent-b` (Bruno) → bleu.
- **Observable** : cases du 14/07 et 15/07 toutes deux bleues, l'une « Alice » l'autre
  « Bruno » ; légende = **deux** entrées bleues distinctes (« Alice », « Bruno »).

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Lister_deux_entrees_de_legende_de_meme_couleur_distinguees_par_leur_identifiant_et_leur_nom_When_deux_acteurs_presents_partagent_la_meme_couleur` | — (caractérisation d'invariant déjà acquis) | ⚠️ **probablement early green — couvert par s07 Sc.2 + Sc.2 #1 (caractérisation, pas driver)** : la légende est **dédoublonnée par identifiant stable** (s07), **jamais par couleur** ; deux ids distincts donnent deux entrées même de teinte identique. Le recoloriage (Sc.2) ne fait que muter la couleur. Aucun rouge — filet documentant la collision **assumée**. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun fichier backend nouveau.)* Test de caractérisation sur `GrilleAgendaQuery`
  (légende dédoublonnée par id) avec un set de couleurs en collision.
- *(Rendu cases + légende : hors backend — routé `ihm-builder`.)*

## Design notes

- **Collision assumée, pas un défaut** (règle 17) : la dédup par **id** (non par couleur)
  est l'invariant qui garantit deux entrées distinctes ; la lisibilité repose sur le nom.
- **Scénario sans driver réel** : tous ses observables découlent de s07 Sc.2 (dédup id) +
  Sc.2 (recolorier). S'il ressort vert au 1er passage chez `tdd-auto`, c'est **attendu**
  (`✅ GREEN (caractérisation)`), pas un défaut à investiguer.
