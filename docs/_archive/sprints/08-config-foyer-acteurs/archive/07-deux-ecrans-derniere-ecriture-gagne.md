# Sc.7 — Deux écrans renomment le même acteur : dernière écriture gagne, les grilles convergent

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **caractérisation backend** (`tdd-auto`, ⚠️ early green attendu sur le store
partagé) **+** **driver runtime IHM** (`ihm-builder` : les **deux grilles convergent** par
diffusion temps réel — le vrai cœur du scénario est runtime/SignalR). Décision CP : store
partagé serveur, **dernière écriture gagne**, sans version ni rejet.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime **SignalR** : deux écrans partageant la grille du foyer renomment le même
> acteur l'un après l'autre ; **les deux grilles convergent** vers la dernière valeur, propagé
> par la diffusion temps réel, **sans rechargement** et **sans rejet**. bUnit seul ne prouve
> **jamais** ce câblage (hub réel, deux clients, re-render) — acceptation sur l'app câblée.

`Should_Faire_converger_les_deux_grilles_vers_Bruno_Martin_dans_la_case_du_15_07_2026_et_en_legende_sans_rechargement_ni_rejet_When_un_premier_ecran_renomme_parent_b_en_Bruno_M_puis_un_second_le_renomme_en_Bruno_Martin`

- **Niveau** : E2E/runtime sur l'app câblée + **hub SignalR réel existant** (palier 1) —
  asserté, pas reconstruit. Store **partagé** côté serveur (singleton).
- **Observable** : après les deux enregistrements, les **deux** grilles affichent
  « Bruno Martin » dans la case du 15/07 et en légende ; aucune édition rejetée.
- **Statut** : ✅ GREEN (runtime, `@vert`) — `FrontWasmConfigDeuxEcransConvergenceTempsReelTests`
  (deux grilles dans deux `TestContext` distincts, même API distante / store singleton partagé ;
  baseline « Bruno » asserté sur les deux avant ; convergence « Bruno Martin » via diffusion réelle ;
  aucun code de production neuf — GREEN minimal).

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Resoudre_le_dernier_nom_ecrit_sans_rejeter_aucune_edition_When_le_meme_acteur_est_renomme_deux_fois_successivement_dans_le_store_partage` | — (caractérisation du store, dernière-écriture-gagne) | ⚠️ **probablement early green — couvert par Sc.1 #1 (caractérisation, pas driver)** : le store **écrase** la valeur (affectation, pas de version ni de garde de conflit) → la dernière écriture gagne **par construction**. Aucun rouge — filet documentant l'absence de rejet/version (décision CP). | ✅ GREEN (caractérisation) |

> **Le driver réel de ce scénario est RUNTIME** (convergence des deux grilles via SignalR),
> routé `ihm-builder` — pas un test backend. La diffusion (palier 1) n'est **pas** une infra
> de ce sprint : on **assert** dessus.

## Fichiers à créer / modifier

- *(Aucun fichier backend nouveau.)* Caractérisation : deux `renommer` successifs → dernière
  valeur, sans rejet (store / handler).
- **Câblage IHM** (routé `ihm-builder`) — le **store singleton partagé** garantit la mémoire
  commune ; le suivi live réutilise le canal de diffusion **existant** (s05) ; `ihm-builder`
  **assert** la convergence des deux grilles, ne reconstruit pas le hub.

## Design notes

- **Mémoire partagée du foyer** (décision CP) : store singleton serveur → toute édition
  aboutie est visible de toutes les grilles connectées. Conflit = **convergence vers la
  dernière valeur** (YAGNI : pas de version, cohérent règle 25).
- **Diffusion lecture seule** : la convergence passe par le canal de diffusion **existant**,
  jamais par une écriture via la diffusion.
- **Scénario à driver runtime, pas backend** : le test backend n'est qu'un filet
  (early green attendu) ; ne pas en inventer un faux rouge.
