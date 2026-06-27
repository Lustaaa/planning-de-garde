# Sc.6 — Deux parents éditent le cycle en même temps : dernière écriture gagne

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : tranche **backend = CARACTÉRISATION** (`tdd-auto`, ⚠️ early green **attendu** : le store
cycle singleton **écrase** par affectation, sans version ni rejet → dernière écriture gagne **par
construction** — patron **s08 Sc.7** — **pas** un driver) **+** acceptation **runtime IHM driver**
(`ihm-builder` : les **deux** grilles convergent **sans rechargement** via SignalR — le vrai symptôme PO).

> **Données** : `parent-a` Alice bleu, `parent-b` Bruno orange, `parent-c` Parent C **vert** (acteur du
> foyer ; un id résolu vert — `tdd-auto` réutilise un acteur du référentiel persisté coloré vert, ou
> seede `parent-c`/vert). Cycle N=2 défini pair → A, impair → B. Écran 1 règle l'index pair sur Parent A ;
> écran 2, juste après, règle l'index pair sur Parent C. Attendu : les deux grilles affichent Parent C
> vert sur les semaines d'index pair (ISO 28), aucun rejet, convergence sans rechargement.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app réellement câblée (deux clients, store cycle singleton partagé, hub SignalR réel), deux
> parents règlent successivement l'index pair depuis deux écrans ; la **dernière** écriture (Parent C
> vert) l'emporte ; les **deux** grilles convergent vers Parent C vert sur l'index pair (ISO 28),
> **aucun message de rejet** n'apparaît, et la convergence se fait **sans rechargement**. **Pas** un test
> bUnit à doublure (qui ne prouve ni le store partagé serveur, ni le hub réel, ni le re-render des deux
> clients).

`Should_Faire_converger_les_deux_grilles_vers_Parent_C_vert_sur_l_index_pair_sans_rejet_ni_rechargement_When_deux_parents_reglent_le_meme_index_l_un_apres_l_autre_depuis_deux_ecrans` — ✅ GREEN *(runtime, `ihm-builder`)* — réalisé par `FrontWasmGrilleDeuxEcransCycleDerniereEcritureGagneTempsReelTests` (app câblée : front WASM + API distante réelle + store cycle singleton partagé + hub SignalR réel). Deux `TestContext` distincts (deux navigateurs/DI), MÊME API. Écran 1 définit le cycle N=2 pair→parent-a/impair→parent-b → les deux grilles affichent « Alice » sur ISO 28 (baseline anti faux-vert) ; écran 2, juste après, règle l'index pair sur parent-c via `POST /api/canal/definir-cycle` → **dernière écriture gagne**, les **deux** grilles convergent **sans rechargement** vers « Marie-Hélène Grand-Dubois » (parent-c résolu sur le référentiel réel ; le scénario nommait « Parent C vert » côté doublure backend, le runtime prouve la convergence par le **nom** résolu sur l'identifiant stable), en case comme en légende, par diffusion SignalR ; **aucun** rejet (confirmation sur chaque écran, `motif-echec-cycle` absent). **Early-green de câblage** (compose Sc.1 + Sc.3 + store partagé s08 Sc.7, aucun code de prod neuf) ; **non-vacuité prouvée** par le baseline « Alice » **ET** par neutralisation temporaire de la re-projection du hub (`On(MiseAJour)` sans `ChargerAsync`) → rouge sur la convergence, puis revert.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Resoudre_le_dernier_mapping_ecrit_sans_rejeter_aucune_edition_When_deux_ecrans_reglent_le_meme_index_du_cycle_l_un_apres_l_autre_dans_le_store_partage` | — | ⚠️ **probablement early green — couvert par le store cycle singleton (affectation, sans version) + Sc.1 (caractérisation, patron s08 Sc.7 — pas driver)**. Deux `DefinirCycle` successifs sur le même store écrasent le mapping ; les deux réussissent (aucune garde de conflit) ; `GrilleAgendaQuery` relit → Parent C/vert sur ISO 28. Filet documentant l'**absence** de version/rejet (YAGNI, cohérent règle 26). `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **Aucun fichier neuf** — réutilise l'écrasement du store cycle singleton (dernière écriture gagne) +
  la résolution du fond (Sc.1).
- **Doublures tests** — store cycle singleton partagé par deux `DefinirCycleHandler` (deux écrans),
  réglés successivement ; `GrilleAgendaQuery` relisant le store partagé. Acteur `parent-c` résolu vert.
- **Volet runtime IHM (routé `ihm-builder`)** — deux clients, hub SignalR réel, convergence des deux
  grilles sans rechargement (patron s08 Sc.7).

## Design notes

- **Dernière écriture gagne par construction** (décision CP, patron s08 Sc.7) : le store cycle est une
  unité de cohérence partagée éditée par affectation, **sans jeton optimiste** ni message « rechargez »
  (réservé aux périodes calendaires, agrégat distinct s01 Sc.10). Aucun faux rouge à inventer.
- **Convergence = effet de la diffusion** (jamais d'écriture par le canal de diffusion) — prouvée au
  runtime sur deux clients réels.
- **Caractérisation conservée** : @limite documentant l'absence de version/rejet ; le driver est runtime
  (convergence sans rechargement).
